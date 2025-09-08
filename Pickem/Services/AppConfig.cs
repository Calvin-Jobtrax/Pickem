// AppConfig.cs

namespace Pickem;

public sealed class AppConfig
{
    public static AppConfig Shared { get; } = new();

    public const int SeasonYear = 2025;

    // ---- Ports
    //    public const int HttpPort = 5064;
    public const int HttpPort = 52227;
    public const int HttpsPort = 7037;

    // ---- Known locations (edit these to your actuals)
    private const string OfficeIp = "192.168.10.201";
    private const string HomeIp = "192.168.0.198";

    // Optional local hostname you may set up on each router/DNS
    private const string LocalHostName = "pickem.lan";

    // ---- Prefs / Env
    private const string PrefKey = "Pickem.BaseRoot";
    private const string EnvVar = "PICKEM_BASEROOT";

    private string _baseRoot;

    private AppConfig()
    {
        // 1) Env override beats everything
        var env = Environment.GetEnvironmentVariable(EnvVar);

        // 2) Then last saved
        var saved = Preferences.Default.Get(PrefKey, "");

        // 3) Else platform default
        var def = GetPlatformDefault();

        _baseRoot = EnsureSlash(!string.IsNullOrWhiteSpace(env) ? env :
                                !string.IsNullOrWhiteSpace(saved) ? saved : def);
    }

    /// <summary>Current base root; persisted when set.</summary>
    public string BaseRoot
    {
        get => _baseRoot;
        set
        {
            _baseRoot = EnsureSlash(value);
            Preferences.Default.Set(PrefKey, _baseRoot);
        }
    }

    // Strongly-typed API roots
    public Uri UserBase => new(new Uri(BaseRoot), "api/user/");
    public Uri PickemBase => new(new Uri(BaseRoot), "api/pickem/");
    public string UserBaseUrl => UserBase.ToString();
    public string PickemBaseUrl => PickemBase.ToString();

    public bool IsPublicServer { get; set; } = true;

    // ---- Defaults per platform

    private static string GetPlatformDefault()
    {
#if ANDROID
        // Android emulator loopback; real devices will be corrected by AutoSelect()
        return $"http://10.0.2.2:{HttpPort}/";
#elif IOS
    // Real iPhone needs a LAN IP, not localhost
    // Use Office by default; AutoSelect() will switch to Home if that's where you are
    return $"http://{OfficeIp}:{HttpPort}/";
#else
    return $"https://localhost:{HttpsPort}/";
#endif
    }

#if ANDROID
    private static bool IsAndroidEmulator()
    {
        try
        {
            var fp = Android.OS.Build.Fingerprint ?? "";
            var prod = Android.OS.Build.Product ?? "";
            var mod = Android.OS.Build.Model ?? "";
            return fp.Contains("generic", StringComparison.OrdinalIgnoreCase)
                || fp.Contains("emulator", StringComparison.OrdinalIgnoreCase)
                || prod.Contains("sdk", StringComparison.OrdinalIgnoreCase)
                || prod.Contains("emulator", StringComparison.OrdinalIgnoreCase)
                || mod.Contains("Emulator", StringComparison.OrdinalIgnoreCase);
        }
        catch { return false; }
    }
#endif

    // ---- Auto-probe: call once on app start (non-blocking is fine)
    public async Task AutoSelectAsync(CancellationToken ct = default)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromMilliseconds(800) };

        foreach (var url in BuildCandidates())
        {
            try
            {
                if (await ProbeAsync(http, url, "health", ct)    // your /health if present
                 || await ProbeAsync(http, url, "index.html", ct)
                 || await ProbeAsync(http, url, "", ct))
                {
                    BaseRoot = url; // persists
                    return;
                }
            }
            catch { /* try next */ }
        }
        // keep current BaseRoot
    }

    private static async Task<bool> ProbeAsync(HttpClient http, string baseUrl, string path, CancellationToken ct)
    {
        var uri = new Uri(new Uri(baseUrl), path);
        using var resp = await http.GetAsync(uri, ct);
        return (int)resp.StatusCode < 500; // reachable (2xx/3xx/4xx)
    }

    private IEnumerable<string> BuildCandidates()
    {
        // 0) Whatever is currently set (env/prefs/default)
        yield return BaseRoot;

#if ANDROID
        // 1) Emulator (common at home & work)
        yield return $"http://10.0.2.2:{HttpPort}/";
        yield return $"https://10.0.2.2:{HttpsPort}/";
#endif

        // 2) Office/Home by IP - HTTP first to prove routing (no cert hassles)
        yield return $"http://{OfficeIp}:{HttpPort}/";
        yield return $"http://{HomeIp}:{HttpPort}/";

        // 3) Optional local hostname if you map it on each router/DNS
        yield return $"http://{LocalHostName}:{HttpPort}/";

        // 4) Same but HTTPS (when your device trusts certs)
        yield return $"https://{OfficeIp}:{HttpsPort}/";
        yield return $"https://{HomeIp}:{HttpsPort}/";
        yield return $"https://{LocalHostName}:{HttpsPort}/";

        // 5) Desktop/simulator fallbacks
        yield return $"http://localhost:{HttpPort}/";
        yield return $"https://localhost:{HttpsPort}/";
    }

    // Quick helpers for a settings screen (optional)
    public void UseOfficeHttp() => BaseRoot = $"http://{OfficeIp}:{HttpPort}/";
    public void UseHomeHttp() => BaseRoot = $"http://{HomeIp}:{HttpPort}/";
    public void UseOfficeHttps() => BaseRoot = $"https://{OfficeIp}:{HttpsPort}/";
    public void UseHomeHttps() => BaseRoot = $"https://{HomeIp}:{HttpsPort}/";

    private static string EnsureSlash(string u) =>
        string.IsNullOrWhiteSpace(u) ? u : (u.EndsWith("/") ? u : u + "/");
}
