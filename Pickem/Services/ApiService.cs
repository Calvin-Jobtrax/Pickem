using System.Net.Http.Json;
using System.Text.Json;
using Pickem.Models;

namespace Pickem.Services;

public sealed class ApiService
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public ApiService(IHttpClientFactory factory)
    {
        _http = factory.CreateClient("Api");
    }

    public async Task<(bool ok, string url, int status, string? error)> ProbeAsync(CancellationToken ct = default)
    {
        // Try a few lightweight endpoints; keep only the one your API exposes
        var candidates = new[] { "health", "user/ping", "" };

        foreach (var path in candidates)
        {
            var url = path;
            try
            {
                using var resp = await _http.GetAsync(url, ct);
                var code = (int)resp.StatusCode;
                if (code < 500) // consider 2xx/3xx/4xx as “reachable”
                    return (true, new Uri(_http.BaseAddress!, url).ToString(), code, null);

                // 5xx means server reachable but error; still useful to surface
                var text = await resp.Content.ReadAsStringAsync(ct);
                return (false, new Uri(_http.BaseAddress!, url).ToString(), code, text);
            }
            catch (Exception ex)
            {
                // try next candidate
                System.Diagnostics.Debug.WriteLine($"[Probe] {url} failed: {ex.Message}");
                if (path == candidates[^1]) // last one
                    return (false, new Uri(_http.BaseAddress!, url).ToString(), 0, ex.Message);
            }
        }

        return (false, new Uri(_http.BaseAddress!, ".").ToString(), 0, "Unknown");
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

    public async Task<List<StandingRow>> GetStandingsAsync(int year, int week)
    {
        var list = await GetJsonSafeAsync<List<StandingRow>>(
            $"pickem/standings?year={year}&week={week}");
        return list ?? new List<StandingRow>();
    }

    public async Task<List<StandingRow>> GetStandingsAsync(int year)
    {
        var list = await GetJsonSafeAsync<List<StandingRow>>(
            $"pickem/standings/{year}");
        return list ?? new List<StandingRow>();
    }

    public async Task<int> GetMaxWeekAsync(int year)
    {
        var obj = await GetJsonSafeAsync<MaxWeekResponse>(
            $"pickem/maxweek/{year}");
        return obj?.MaxWeek ?? 0;
    }

    public async Task<List<PodiumRow>> GetPodiumAsync(int year, int maxWeek)
    {
        var url = $"pickem/podium?year={year}&maxWeek={maxWeek}";
        var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var rows = await _http.GetFromJsonAsync<List<PodiumRow>>(url, opts);
        return rows ?? new();
    }

    public Task<List<PoolRow>?> GetPoolAsync(int year, int week) =>
        GetJsonSafeAsync<List<PoolRow>>($"pickem/pool?year={year}&week={week}");

    public Task<List<WagerRow>?> GetWagersAsync(int year, int week, int playerId) =>
        GetJsonSafeAsync<List<WagerRow>>($"pickem/wagers/{year}/{week}/{playerId}");

    public Task<PlayerCardDto?> GetPlayerCardAsync(int year, int week, int playerId) =>
        _http.GetFromJsonAsync<PlayerCardDto>($"pickem/playercard/{year}/{week}/{playerId}");

    public Task<bool> SaveGameAsync(WagerRow row) =>
        UpdateGameAsync(row.Record, row.Wager, row.VisitorWin, row.HomeWin);

    public async Task<bool> UpdateGameAsync(int record, int wager, bool? visitorWin, bool? homeWin)
    {
        var dto = new UpdateGameDto(record, wager, visitorWin, homeWin);
        var res = await _http.PutAsJsonAsync($"pickem/{record}", dto);
        return res.IsSuccessStatusCode;
    }

    // Optional plain-text endpoint (still no leading slash)
    public Task<string> GetTextAsync(string path) => _http.GetStringAsync(path);

}

// ---------- DTOs used only by the client ----------
public sealed class MaxWeekResponse
{
    public int Year { get; set; }
    public int MaxWeek { get; set; }
}

public sealed record UpdateGameDto(int Record, int Wager, bool? VisitorWin, bool? HomeWin);
