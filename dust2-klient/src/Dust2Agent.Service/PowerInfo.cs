using System.Runtime.Versioning;
using Microsoft.Win32;

namespace Dust2Agent.Service;

/// <summary>
/// Quvvat sozlamalari. Hozircha bitta narsa muhim: "tez ishga tushirish"
/// (fast startup / hybrid boot).
///
/// U yoqilgan bo'lsa Windows "o'chirish" buyrug'ida kompyuterni to'liq
/// o'chirmaydi — gibrid uyquga qo'yadi. Ko'p tarmoq kartalari bunday holatda
/// sehrli paketni qabul qilmaydi va masofadan yoqish (Wake-on-LAN) ishlamaydi.
/// </summary>
[SupportedOSPlatform("windows")]
public static class PowerInfo
{
    private const string KeyPath = @"SYSTEM\CurrentControlSet\Control\Session Manager\Power";
    private const string ValueName = "HiberbootEnabled";

    /// <summary>Tez ishga tushirish yoqilganmi. Aniqlab bo'lmasa — false.</summary>
    public static bool FastStartupEnabled()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(KeyPath);
            return key?.GetValue(ValueName) is int v && v != 0;
        }
        catch (Exception ex) when (ex is System.Security.SecurityException or UnauthorizedAccessException)
        {
            return false;
        }
    }
}
