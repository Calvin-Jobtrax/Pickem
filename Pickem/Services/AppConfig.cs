// AppConfig.cs

namespace Pickem
{
    public sealed class AppConfig
    {
        public static AppConfig Shared { get; } = new();

        public const int SeasonYear = 2025;

        // Deployed default (ensure trailing slash)
        public const string DefaultBaseRoot = "https://jobtraxweb.com/api/";

        // ---- Prefs / Env
        private const string PrefKey = "Pickem.BaseRoot";
        private const string EnvVar = "PICKEM_BASEROOT";

        private string _baseRoot;

        private AppConfig()
        {
            // 1) Env override
            var env = Environment.GetEnvironmentVariable(EnvVar);

            // 2) Last saved
            var saved = Preferences.Default.Get(PrefKey, "");

            // 3) Default (production)
            var def = DefaultBaseRoot;

            _baseRoot = EnsureSlash(!string.IsNullOrWhiteSpace(env) ? env :
                                    !string.IsNullOrWhiteSpace(saved) ? saved : def);
        }

        /// <summary>
        /// Current base root; persisted when set via SetBaseRoot(...).
        /// </summary>
        public string BaseRoot => _baseRoot;

        /// <summary>
        /// Explicitly change and persist the base root (e.g., for debugging).
        /// </summary>
        public void SetBaseRoot(string newRoot)
        {
            _baseRoot = EnsureSlash(newRoot);
            Preferences.Default.Set(PrefKey, _baseRoot);
        }

        // ---- Strongly-typed API roots
        // IMPORTANT: BaseRoot already ends with ".../api/", so don't prepend "api/" again.
        public Uri UserBase => new(new Uri(BaseRoot), "user/");
        public Uri PickemBase => new(new Uri(BaseRoot), "pickem/");

        public string UserBaseUrl => UserBase.ToString();
        public string PickemBaseUrl => PickemBase.ToString();

        /// <summary>
        /// Optional quick health probe against the current BaseRoot.
        /// </summary>
        public async Task<bool> ProbeAsync(CancellationToken ct = default)
        {
            try
            {
                using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
                // Try /health first (if you have it); otherwise "/" is fine.
                var uri = new Uri(new Uri(BaseRoot), "health");
                using var resp = await http.GetAsync(uri, ct);
                return (int)resp.StatusCode < 500;
            }
            catch
            {
                return false;
            }
        }

        private static string EnsureSlash(string u) =>
          string.IsNullOrWhiteSpace(u) ? u : (u.EndsWith("/") ? u : u + "/");
    }
}
