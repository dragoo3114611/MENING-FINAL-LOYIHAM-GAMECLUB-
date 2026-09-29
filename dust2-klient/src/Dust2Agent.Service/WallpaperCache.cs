using Dust2Agent.Common;

namespace Dust2Agent.Service;

/// <summary>
/// Qulf ekrani fon rasmi keshi. Rasm har safar yuborilmaydi — admin faqat
/// hashini beradi, farq bo'lsa agent rasmni alohida so'raydi ([qaror 4]).
/// </summary>
public sealed class WallpaperCache
{
    private readonly AgentLog _log;
    private readonly string _hashFile;

    public WallpaperCache(AgentLog log)
    {
        _log = log;
        _hashFile = System.IO.Path.Combine(AgentPaths.Root, "wallpaper.hash");
        Path = FindExisting();
    }

    /// <summary>Keshdagi rasm fayli (yo'q bo'lsa bo'sh satr).</summary>
    public string Path { get; private set; } = "";

    public string CurrentHash
    {
        get
        {
            try
            {
                return File.Exists(_hashFile) ? File.ReadAllText(_hashFile).Trim() : "";
            }
            catch (IOException)
            {
                return "";
            }
        }
    }

    public bool Has(string hash) => !string.IsNullOrEmpty(Path) && CurrentHash == hash;

    public void Save(string? hash, string mime, string base64)
    {
        if (string.IsNullOrEmpty(hash) || string.IsNullOrEmpty(base64))
        {
            Clear();
            return;
        }
        try
        {
            AgentPaths.EnsureCreated();
            var ext = mime.Contains("png") ? ".png" : mime.Contains("webp") ? ".webp" : ".jpg";
            var file = System.IO.Path.Combine(AgentPaths.Root, "wallpaper" + ext);
            File.WriteAllBytes(file, Convert.FromBase64String(base64));
            File.WriteAllText(_hashFile, hash);
            Path = file;
            _log.Info($"Qulf ekrani fon rasmi keshga saqlandi ({base64.Length / 1024} KB)");
        }
        catch (Exception ex)
        {
            _log.Warn($"Fon rasmini saqlab bo'lmadi: {ex.Message}");
        }
    }

    public void Clear()
    {
        try
        {
            foreach (var ext in new[] { ".jpg", ".png", ".webp" })
            {
                var f = System.IO.Path.Combine(AgentPaths.Root, "wallpaper" + ext);
                if (File.Exists(f)) File.Delete(f);
            }
            if (File.Exists(_hashFile)) File.Delete(_hashFile);
        }
        catch (IOException)
        {
            // keyingi safar tozalanadi
        }
        Path = "";
    }

    private static string FindExisting()
    {
        foreach (var ext in new[] { ".jpg", ".png", ".webp" })
        {
            var f = System.IO.Path.Combine(AgentPaths.Root, "wallpaper" + ext);
            if (File.Exists(f)) return f;
        }
        return "";
    }
}
