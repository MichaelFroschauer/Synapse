using Synapse.HITL.Scripting.OpenRouter.Models;

namespace Synapse.HITL.Scripting.Abstractions;

public interface IScriptProvider
{
    Task<GeneratedCode> GenerateScriptAsync(IChatPrompt prompt, string model, int maxTokens, double temperature, CancellationToken ct = default);
}
