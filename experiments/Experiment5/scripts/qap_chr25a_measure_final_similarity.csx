// Experiment 5: RAPGA Diversity Threshold and User Preferences — Measure Final Similarity
// Algorithm: RAPGA | Problem: chr25a (QAP, N=25) | Seed: 12345678
//
// This script is used for measuring the final solution similarity to the reference solution 
// at the end of the search for configurations (b), (c), and (d) from the preference script.
// The similarity is measured using the same QAP assignment match similarity as in the preference.

// BEGIN NAME
QAP chr25a Similarity Measurement
// END NAME
// BEGIN SCRIPT
async (g) => {
    var simMeasure = new QapSolutionSimilarity();

    // Reference solution: three highest-flow facilities placed at central locations.
    //   Facility 13 (total flow 1900) -> Location 22 (most central, total dist 5)
    //   Facility 14 (total flow 1644) -> Location 16 (total dist 7)
    //   Facility 11 (total flow 1562) -> Location 10 (total dist 8)
    // Remaining facilities are assigned to the remaining locations in order.
    var referenceSolution = new QapSolution(new int[] {
        0, 1, 2, 3, 4, 5, 6, 7, 8, 9,
        11, 10, 12, 22, 16, 13, 14, 15,
        17, 18, 19, 20, 21, 23, 24
    });

    // Same reference solution as used in the preference definition
    //var refSol = new QapSolution([24,11,4,2,17,3,15,7,19,9,13,5,14,22,23,18,12,0,20,10,16,1,21,6,8]);
    //var refSol = new QapSolution([1,5,3,2,7,0,11,19,21,14,17,6,4,22,24,13,15,9,18,8,16,10,23,20,12]);
    var globalBest = g.Best as QapSolution;
    var populationBest = g.Current as QapSolution;

    double simGlobal = simMeasure.GetSimilarity(refSol, globalBest);
    double simPopulation = simMeasure.GetSimilarity(refSol, populationBest);

    File.AppendAllText(@"/home/michael/Desktop/snapshot/globalBestSim.txt", simGlobal + Environment.NewLine);
    File.AppendAllText(@"/home/michael/Desktop/snapshot/populationBestSim.txt", simPopulation + Environment.NewLine);
    await Task.CompletedTask;
}
// END SCRIPT

async (g) =>
{
	var globalBest = (g.Best as QapSolution).GetParametersWithType();
        var populationBest = (g.Current as QapSolution).GetParametersWithType();
        
        int globalBestMatch = 0;
        int populationBestMatch = 0;
        if (globalBest[22] == 13) globalBestMatch++;
        if (globalBest[16] == 14) globalBestMatch++;
        if (globalBest[10] == 11) globalBestMatch++;
        if (populationBest[22] == 13) populationBestMatch++;
        if (populationBest[16] == 14) populationBestMatch++;
        if (populationBest[10] == 11) populationBestMatch++;
        
        File.AppendAllText(@"/home/michael/Desktop/snapshot/globalBestSim.txt", globalBestMatch + Environment.NewLine);
        File.AppendAllText(@"/home/michael/Desktop/snapshot/populationBestSim.txt", populationBestMatch + Environment.NewLine);
        await Task.CompletedTask;
}