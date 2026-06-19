using System.Text.Json.Serialization;

namespace Synapse.HITL.Scripting.OpenRouter.Models;

public class ChatMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = default!;

    [JsonPropertyName("content")]
    public string Content { get; set; } = default!;
}
