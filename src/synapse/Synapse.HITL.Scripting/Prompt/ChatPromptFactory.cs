using Synapse.HITL.Scripting.Abstractions;
using Synapse.HITL.Scripting.Prompt.PromptTypes;

namespace Synapse.HITL.Scripting.Prompt;

public static class ChatPromptFactory
{
    public static IChatPrompt Get(string? userPrompt = null, IEnumerable<string>? generatedProblemSpecifics = null) 
        => Get(PromptType.Generic, userPrompt, generatedProblemSpecifics);
    
    public static IChatPrompt Get(PromptType type = PromptType.Generic, string? userPrompt = null,
        IEnumerable<string>? generatedProblemSpecifics = null)
    {
        return type switch
        {
            PromptType.Generic => throw new ArgumentException("Param value=" + type + " is currently not supported."),
            PromptType.Knapsack => throw new ArgumentException("Param value=" + type + " is currently not supported."),
            PromptType.Qap => throw new ArgumentException("Param value=" + type + " is currently not supported."),
            PromptType.Jsp => throw new ArgumentException("Param value=" + type + " is currently not supported."),
            PromptType.Function => throw new ArgumentException("Param value=" + type + " is currently not supported."),
            PromptType.Tsp => new PromptBuilder<TspChatPrompt>(userPrompt, generatedProblemSpecifics).Build(),
            PromptType.HitlTsp => new PromptBuilder<TspHitlChatPrompt>(userPrompt, generatedProblemSpecifics).Build(),
            PromptType.HitlQap => new PromptBuilder<QapHitlChatPrompt>(userPrompt, generatedProblemSpecifics).Build(),
            PromptType.HitlJsp => new PromptBuilder<JspHitlChatPrompt>(userPrompt, generatedProblemSpecifics).Build(),
            PromptType.HitlFunction => new PromptBuilder<FunctionHitlChatPrompt>(userPrompt, generatedProblemSpecifics).Build(),
            
            _ => throw new ArgumentException("Param value=" + type + " is not supported.")
        };
    }
}