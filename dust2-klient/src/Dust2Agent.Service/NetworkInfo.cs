using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Dust2Agent.Service;

/// <summary>
/// Kompyuterning lokal IP va MAC manzili — juftlashda adminga yuboriladi.
///
/// Diqqat: Wake-on-LAN aynan shu MAC bo'yicha ishlaydi, shuning uchun manzil
/// haqiqiy tarmoq kartasiniki bo'lishi shart. Kompyuterda VirtualBox, VMware,
/// Hyper-V, WSL yoki VPN bo'lsa, "birinchi ishlayotgan karta" ko'pincha
/// virtual karta bo'lib chiqadi va uning MAC'iga yuborilgan paket hech qachon
/// kompyuterni uyg'otmaydi. Shu sababli avval adminga boradigan yo'ldagi karta
/// aniqlanadi (UDP soketning lokal manzili orqali — hech narsa yuborilmaydi).
/// </summary>
public static class NetworkInfo
{
    /// <summary>Admin shu manzilda bo'lsa, o'sha tomonga ketadigan karta tanlanadi.</summary>
    public static string? AdminHost { get; set; }

    public static string LocalIp() => Primary().Ip;

    public static string MacAddress() => Primary().Mac;

    public readonly record struct Nic(string Ip, string Mac, string Name);

    /// <summary>Adminga ulanish uchun ishlatiladigan tarmoq kartasi.</summary>
    public static Nic Primary()
    {
        try
        {
            var local = RouteAddress(AdminHost);
            var nics = Usable().ToList();

            // 1) Adminga boradigan kartaning o'zi
            foreach (var n in nics)
            {
                if (n.Ip == local && n.Mac.Length > 0) return n;
            }

            // 2) MAC'i bor birinchi haqiqiy karta (IP ham o'shaniki bo'lsin)
            foreach (var n in nics)
            {
                if (n.Mac.Length > 0) return n;
            }

            // 3) Hech bo'lmasa IP manzil
            return new Nic(local ?? "", "", "");
        }
        catch (Exception)
        {
            // Tarmoq ma'lumotini o'qib bo'lmadi — ulanishga xalaqit bermaymiz
            return new Nic("", "", "");
        }
    }

    /// <summary>
    /// Admin tomonga qaysi manzildan chiqilishini aniqlaydi. UDP soket "ulanishi"
    /// hech qanday paket yubormaydi — Windows shunchaki marshrutni tanlaydi.
    /// </summary>
    private static string? RouteAddress(string? host)
    {
        if (string.IsNullOrWhiteSpace(host)) return null;
        try
        {
            var ip = IPAddress.TryParse(host, out var parsed)
                ? parsed
                : Dns.GetHostAddresses(host).FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork);
            if (ip is null) return null;

            using var s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            s.Connect(ip, 9);
            return (s.LocalEndPoint as IPEndPoint)?.Address.ToString();
        }
        catch (Exception ex) when (ex is SocketException or ArgumentException)
        {
            return null;
        }
    }

    /// <summary>Haqiqiy (virtual bo'lmagan) IPv4 kartalar — ehtimoli kattadan kichigiga.</summary>
    private static IEnumerable<Nic> Usable()
    {
        var list = new List<(Nic Nic, int Rank)>();
        foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ni.OperationalStatus != OperationalStatus.Up) continue;
            if (ni.NetworkInterfaceType is NetworkInterfaceType.Loopback or NetworkInterfaceType.Tunnel) continue;
            if (IsVirtual(ni.Description) || IsVirtual(ni.Name)) continue;

            var mac = ni.GetPhysicalAddress().GetAddressBytes();
            var macText = mac.Length == 6 ? string.Join(":", mac.Select(b => b.ToString("X2"))) : "";

            foreach (var a in ni.GetIPProperties().UnicastAddresses)
            {
                if (a.Address.AddressFamily != AddressFamily.InterNetwork) continue;
                if (IPAddress.IsLoopback(a.Address)) continue;
                // Simli karta birinchi: Wake-on-LAN odatda faqat unda ishlaydi
                var rank = ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet ? 0 : 1;
                list.Add((new Nic(a.Address.ToString(), macText, ni.Name), rank));
            }
        }
        return list.OrderBy(x => x.Rank).Select(x => x.Nic);
    }

    private static readonly string[] VirtualMarks =
    {
        "virtual", "vmware", "vbox", "virtualbox", "hyper-v", "loopback", "tap-", "tun",
        "wsl", "docker", "bluetooth", "vpn", "wintun", "wireguard", "zerotier", "hamachi", "radmin"
    };

    private static bool IsVirtual(string? text)
    {
        if (string.IsNullOrEmpty(text)) return false;
        var lower = text.ToLowerInvariant();
        return VirtualMarks.Any(m => lower.Contains(m, StringComparison.Ordinal));
    }
}
