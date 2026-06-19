using Synapse.Test.Experiments;

namespace Synapse.Test;

class Program
{
    static async Task Main(string[] args)
    {
        // await TestGeneticAlgorithm.Run();
        // await TestGeneticAlgorithmRealValues.Run();
        // await TestParticleSwarmAlgorithm.Run();
        // await TestJobManager.Run();
        // await TestAIScriptGenerator.Run();
        // await TestQapSolver.Run();
        // await TestReflectionFinder.Run();
        // await TestSimilarityMeasures.Run();
        // await TestRAPGA.Run();
        await TestSimilarityMeasures.Run();

        // await ExperimentRunner.ConfigureAndRunAsync();
    }
}
