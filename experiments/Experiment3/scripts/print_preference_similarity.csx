// Experiment 3: Preference-Based Guidance in GA and PSO
// Algorithm: GA or PSO | Problem: TSP berlin52 | Seed: 12345678
//
// This script is used for measuring the final solution similarity to the
// reference solution at regular intervals during the optimization process.
// The similarity is measured using a TspSolutionSequenceSimilarity measure, which compares the structure of the tours.
// The results are appended to text files for later analysis.
// Execute Once: False

async (g) => {
    var simMeasure = new TspSolutionSequenceSimilarity();

    // Same reference solution as used in the preference definition
    var refSol = new TspSolution([16,2,17,30,21,0,48,31,44,18]);
    var globalBest = g.Best as TspSolution;
    var populationBest = g.Current as TspSolution;

    double simGlobal = simMeasure.GetSimilarity(refSol, globalBest);
    double simPopulation = simMeasure.GetSimilarity(refSol, populationBest);

    File.AppendAllText(@"./globalBestSim.txt", simGlobal + Environment.NewLine);
    File.AppendAllText(@"./populationBestSim.txt", simPopulation + Environment.NewLine);
    await Task.CompletedTask;
}
