using Synapse.HITL.Scripting.Abstractions;
using Synapse.HITL.Scripting.OpenRouter.Models;

namespace Synapse.HITL.Scripting.OpenRouter;

public class OpenRouterScriptProvider : IScriptProvider
{
    private readonly IApiClient _client;

    public OpenRouterScriptProvider(IApiClient client)
    {
        _client = client;
    }

    public async Task<GeneratedCode> GenerateScriptAsync(IChatPrompt prompt, string model = "openai/gpt-4o", int maxTokens = -1,
        double temperature = -1, CancellationToken ct = default)
    {
        var systemMessage = string.IsNullOrWhiteSpace(prompt.Message) 
            ? throw new ArgumentNullException(nameof(prompt.Message), ": Must contain a system prompt") 
            : prompt.Message;
        var userMessage = string.IsNullOrWhiteSpace(prompt.UserPrompt) ? "" : prompt.UserPrompt;
        
        var request = new ChatRequest
        {
            Model = model,
            Messages = new[]
            {
                new ChatMessage { Role = "system", Content = systemMessage },
                new ChatMessage { Role = "user", Content = userMessage }
            },
            Stream = false
        };
        if (maxTokens >= 0) request.MaxTokens = maxTokens;
        if (temperature >= 0) request.Temperature = temperature;

        var raw = await _client.RequestAsync(request, ct).ConfigureAwait(false);

        var generatedScript = new GeneratedCode();
        if (FoundError(raw))
        {
            generatedScript.ErrorMessage = raw;
            generatedScript.ErrorGeneratingCode = true;
        }
        else
        {
            var script = ExtractBetween(raw, "// BEGIN SCRIPT", "// END SCRIPT");
            var scriptName = ExtractBetween(raw, "// BEGIN NAME", "// END NAME");
            generatedScript.Name = scriptName;
            generatedScript.Code = script;
        }
        
        return generatedScript;
    }

    private static bool FoundError(string text, string error = "ERROR")
    {
        if (text is null) throw new ArgumentNullException(nameof(text));
        var errorIdx = text.IndexOf(error, StringComparison.Ordinal);
        return errorIdx > -1;
    }

    private static string ExtractBetween(string text, string start = "// BEGIN SCRIPT", string end = "// END SCRIPT")
    {
        if (text is null) throw new ArgumentNullException(nameof(text));
        var si = text.IndexOf(start, StringComparison.Ordinal);
        var ei = text.IndexOf(end, StringComparison.Ordinal);

        if (si < 0 || ei < 0 || ei <= si)
        {
            throw new InvalidOperationException("Expected start/end markers not found in response.");
        }

        var content = text.Substring(si + start.Length, ei - (si + start.Length));
        return content.Trim();
    }
}
