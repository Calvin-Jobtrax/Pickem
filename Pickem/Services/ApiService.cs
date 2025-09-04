using System.Net.Http.Json;
using System.Text.Json;
using Pickem.Models;

namespace Pickem.Services;

public sealed class AppConfig
{
    public static AppConfig Shared { get; } = new();

    // Example: set from appsettings, Secrets, or compile-time constants
    // e.g., "https://jobtraxweb.com:52227/api/user"
    public string UserBaseUrl { get; set; } = "https://localhost:7037/api/user";

    // If false, we force Wi-Fi (mirrors your Swift check)
    public bool IsPublicServer { get; set; } = true;
}

public sealed class ApiService
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public ApiService(IHttpClientFactory factory)
    {
        _http = factory.CreateClient("Api");
    }

    // ---------- Low-level helpers ----------
    private async Task<T?> GetJsonSafeAsync<T>(string path)
    {
        using var resp = await _http.GetAsync(path);
        var text = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode)
            throw new HttpRequestException($"GET {path} failed {(int)resp.StatusCode}: {text}");
        return string.IsNullOrWhiteSpace(text) ? default : JsonSerializer.Deserialize<T>(text, _json);
    }

    private async Task<T?> TryGetJsonOrNullAsync<T>(string path)
    {
        using var resp = await _http.GetAsync(path);
        if (resp.StatusCode == System.Net.HttpStatusCode.NotFound)
            return default;

        var text = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode)
            throw new HttpRequestException($"GET {path} failed {(int)resp.StatusCode}: {text}");

        return string.IsNullOrWhiteSpace(text) ? default : JsonSerializer.Deserialize<T>(text, _json);
    }

    private async Task PutJsonSafeAsync<T>(string path, T payload)
    {
        using var resp = await _http.PutAsJsonAsync(path, payload, _json);
        var text = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode)
            throw new HttpRequestException($"PUT {path} failed {(int)resp.StatusCode}: {text}");
    }

    // ---------- API wrappers ----------

    /// <summary>
    /// Returns weekly standings for a given NFL year/week.
    /// Tries /pickem/standings/{year}/{week}, falls back to /pickem/standings?year=&week=
    /// </summary>
    // GET /pickem/standings?year=YYYY&week=W
    public async Task<List<StandingRow>> GetStandingsAsync(int year, int week)
    {
        var list = await GetJsonSafeAsync<List<StandingRow>>(
            $"/api/pickem/standings?year={year}&week={week}");
        return list ?? new List<StandingRow>();
    }


    /// <summary>
    /// Optional: season-to-date standings (no week).
    /// </summary>
    public async Task<List<StandingRow>> GetStandingsAsync(int year)
    {
        var list = await GetJsonSafeAsync<List<StandingRow>>($"/api/pickem/standings/{year}");
        return list ?? new List<StandingRow>();
    }

    public async Task<int> GetMaxWeekAsync(int year)
    {
        // endpoint returns: { "year": 2025, "maxWeek": 18 }
        var obj = await GetJsonSafeAsync<MaxWeekResponse>($"/api/pickem/maxweek/{year}");
        return obj?.MaxWeek ?? 0;  // obj can be null; MaxWeek is non-nullable int
    }

    public async Task<List<PodiumRow>> GetPodiumAsync(int year, int maxWeek)
    {
        var url = $"/api/pickem/podium?year={year}&maxWeek={maxWeek}";
        var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var rows = await _http.GetFromJsonAsync<List<PodiumRow>>(url, opts);
        return rows ?? new();
    }

    public Task<List<PoolRow>?> GetPoolAsync(int year, int week)
        => GetJsonSafeAsync<List<PoolRow>>($"/api/pickem/pool?year={year}&week={week}");

    public Task<List<WagerRow>?> GetWagersAsync(int year, int week, int playerId)
        => GetJsonSafeAsync<List<WagerRow>>($"/api/pickem/wagers/{year}/{week}/{playerId}");

    public Task<bool> SaveGameAsync(WagerRow row) =>
        UpdateGameAsync(row.Record, row.Wager, row.VisitorWin, row.HomeWin);

    public async Task<bool> UpdateGameAsync(int record, int wager, bool? visitorWin, bool? homeWin)
    {
        var dto = new UpdateGameDto(record, wager, visitorWin, homeWin);
        var res = await _http.PutAsJsonAsync($"/api/pickem/{record}", dto);
        return res.IsSuccessStatusCode;
    }

    // Optional text endpoint (e.g., /home)
    public Task<string> GetTextAsync(string path) => _http.GetStringAsync(path);

    public Task<PlayerCardDto?> GetPlayerCardAsync(int year, int week, int playerId)
        => _http.GetFromJsonAsync<PlayerCardDto>($"/api/pickem/playercard/{year}/{week}/{playerId}");
}

// ---------- DTOs used only by the client ----------
public sealed class MaxWeekResponse
{
    public int Year { get; set; }
    public int MaxWeek { get; set; }
}

public sealed record UpdateGameDto(int Record, int Wager, bool? VisitorWin, bool? HomeWin);
