using Synapse.HITL.Scripting.OpenRouter.Models;

namespace Synapse.HITL.Scripting.Abstractions;

public interface IApiClient
{
    Task<string> RequestAsync(ChatRequest request, CancellationToken ct = default);
}
