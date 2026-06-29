// Experiment 5: RAPGA Diversity Threshold and User Preferences - Measure Final Similarity
// Algorithm: RAPGA | Problem: chr25a (QAP, N=25) | Seed: 12345678
//
// This script is used for measuring the final solution similarity to the
// reference solution at regular intervals during the optimization process.
// The similarity is measured using the same QAP assignment match similarity as in the preference.
// The results are appended to text files for later analysis.
// Execute Once: False

// BEGIN NAME
QAP chr25a Similarity Measurement
// END NAME
// BEGIN SCRIPT
async (g) => {
    var simMeasure = new QapSolutionSimilarity();

    // Reference solution: three highest-flow facilities placed at central locations.
    // Remaining facilities are assigned to the remaining locations in order.
    var referenceSolution = new QapSolution(new int[] {
        0, 1, 2, 3, 4, 5, 6, 7, 8, 9,
        11, 10, 12, 22, 16, 13, 14, 15,
        17, 18, 19, 20, 21, 23, 24
    });

    // Same reference solution as used in the preference definition
    var globalBest = g.Best as QapSolution;
    var populationBest = g.Current as QapSolution;

    double simGlobal = simMeasure.GetSimilarity(refSol, globalBest);
    double simPopulation = simMeasure.GetSimilarity(refSol, populationBest);

    File.AppendAllText(@"./globalBestSim.txt", simGlobal + Environment.NewLine);
    File.AppendAllText(@"./populationBestSim.txt", simPopulation + Environment.NewLine);
    await Task.CompletedTask;
}
// END SCRIPT
