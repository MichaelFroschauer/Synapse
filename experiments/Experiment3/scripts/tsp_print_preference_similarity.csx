
// This script is used to print the similarity of the global best and population best solutions
// to a reference solution at regular intervals during the optimization process.
// The similarity is measured using a TspSolutionSimilarity measure, which compares the structure of the tours.
// The results are appended to text files for later analysis.

async (g) => {
    var simMeasure = new TspSolutionSequenceSimilarity();

    // Same reference solution as used in the preference definition
    //var refSol = new TspSolution([21, 48, 38, 37, 4, 14, 42, 32, 9, 8]);
    var refSol = new TspSolution([16,2,17,30,21,0,48,31,44,18]);
    //var refSol = new TspSolution([112,64,98,91,88,124,103,109,84,85,86,87,108,95,118,62,101,100,82,81,125,80,83,116,77,75,74,68,69,70]);
    var globalBest = g.Best as TspSolution;
    var populationBest = g.Current as TspSolution;

    double simGlobal = simMeasure.GetSimilarity(refSol, globalBest);
    double simPopulation = simMeasure.GetSimilarity(refSol, populationBest);

    File.AppendAllText(@"/home/michael/Desktop/snapshot/globalBestSim.txt", simGlobal + Environment.NewLine);
    File.AppendAllText(@"/home/michael/Desktop/snapshot/populationBestSim.txt", simPopulation + Environment.NewLine);
    await Task.CompletedTask;
}
