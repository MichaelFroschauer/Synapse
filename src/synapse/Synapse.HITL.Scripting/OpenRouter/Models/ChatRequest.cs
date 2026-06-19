using System.Text.Json.Serialization;

namespace Synapse.HITL.Scripting.OpenRouter.Models;

public class ChatRequest
{
    [JsonPropertyName("model")] public string Model { get; set; } = null!;

    [JsonPropertyName("messages")] public ChatMessage[] Messages { get; set; } = Array.Empty<ChatMessage>();

    [JsonPropertyName("max_tokens")] public int? MaxTokens { get; set; }

    [JsonPropertyName("temperature")] public double? Temperature { get; set; }

    [JsonPropertyName("stream")] public bool Stream { get; set; } = false;
}
