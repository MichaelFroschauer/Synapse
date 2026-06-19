using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Synapse.HITL.Scripting.Abstractions;
using Synapse.HITL.Scripting.OpenRouter.Models;

namespace Synapse.HITL.Scripting.OpenRouter;

public class OpenRouterClient : IApiClient
{
    private readonly HttpClient _http;
    private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
    
    private readonly string _requestUrl = "https://openrouter.ai/api/v1/chat/completions";
    
    public OpenRouterClient(string apiKey, HttpClient? http = null)
    {
        _http = http ?? new HttpClient();
        _http.DefaultRequestHeaders.Authorization ??= new AuthenticationHeaderValue("Bearer", apiKey);
    }
    
    public async Task<string> RequestAsync(ChatRequest requestContent, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(requestContent, _jsonOptions);
        using var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
        using var resp = await _http.PostAsync(_requestUrl, httpContent, ct).ConfigureAwait(false);
        var body = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

        if (!resp.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"API request failed with {(int)resp.StatusCode}: {resp.ReasonPhrase}. Body: {Truncate(body, 200)}");
        }
        
        using var doc = JsonDocument.Parse(body);
        if (doc.RootElement.TryGetProperty("choices", out var choices) &&
            choices.ValueKind == JsonValueKind.Array &&
            choices.GetArrayLength() > 0)
        {
            var first = choices[0];
            if (first.TryGetProperty("message", out var message) &&
                message.TryGetProperty("content", out var contentProp) &&
                contentProp.ValueKind == JsonValueKind.String)
            {
                return contentProp.GetString() ?? string.Empty;
            }
        }
        
        throw new InvalidOperationException("Unexpected API response shape. Response body: " + Truncate(body, 500));
    }
    
    private static string Truncate(string s, int max) => s?.Length > max ? s.Substring(0, max) + "..." : s ?? string.Empty;
}
