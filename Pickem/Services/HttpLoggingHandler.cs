using System.Diagnostics;

namespace Pickem.Services;

public sealed class HttpLoggingHandler : DelegatingHandler
{
    // IMPORTANT: no inner handler here; the factory will set it
    public HttpLoggingHandler() { }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken ct)
    {
        Debug.WriteLine($"[HTTP] → {request.Method} {request.RequestUri}");

        if (request.Content != null)
        {
            var reqText = await request.Content.ReadAsStringAsync(ct);
            if (!string.IsNullOrWhiteSpace(reqText))
                Debug.WriteLine($"[HTTP]   body: {Trunc(reqText)}");
        }

        var resp = await base.SendAsync(request, ct);

        var text = resp.Content != null ? await resp.Content.ReadAsStringAsync(ct) : "";
        Debug.WriteLine($"[HTTP] ← {(int)resp.StatusCode} {resp.ReasonPhrase}");
        if (!string.IsNullOrWhiteSpace(text))
            Debug.WriteLine($"[HTTP]   resp: {Trunc(text)}");

        return resp;

        static string Trunc(string s) => s.Length <= 400 ? s : s[..400] + " …";
    }
}
