using Synapse.HITL.Scripting.OpenRouter;
using Synapse.HITL.Scripting.Prompt;

namespace Synapse.Test;

public class TestAIScriptGenerator : IMetaheuristicTester
{
    public static async Task Run()
    {
        var key = "";
        var client = new OpenRouterClient(key);

        //var userPrompt = "Please create a solution where the first three cities in the tsp are always 1, 2, 3.";
        var userPrompt = "Ich denke es ist am besten, wenn die städte 4,14,3,5 hintereinander kommen. Möchte ich mit stadt 10 beginnen und ich bin mir relativ sicher, die Städte 10, 20 und 21 direkt hintereinander kommen sollten.";
        //var prompt = ChatPromptFactory.Get(PromptType.Tsp, userPrompt);
        var prompt = ChatPromptFactory.Get(PromptType.HitlTsp, userPrompt);
        var scriptGenerator = new OpenRouterScriptProvider(client);
        var script = await scriptGenerator.GenerateScriptAsync(prompt);
        Console.WriteLine(script);
    }
}
