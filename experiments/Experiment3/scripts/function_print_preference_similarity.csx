
// This script is used to print the similarity of the global best and population best solutions
// to a reference solution at regular intervals during the optimization process.
// The similarity is measured using a TspSolutionSimilarity measure, which compares the structure of the tours.
// The results are appended to text files for later analysis.

async (g) => {
    var simMeasure = new RealValueSolutionEuclideanSimilarity();

    // Same reference solution as used in the preference definition
    FunctionSolution refSol = new FunctionSolution([0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0]);
    var globalBest = g.Best as RealValueSolution;
    var populationBest = g.Current as RealValueSolution;

    double simGlobal = simMeasure.GetSimilarity(refSol, globalBest);
    double simPopulation = simMeasure.GetSimilarity(refSol, populationBest);

    File.AppendAllText(@"/home/michael/Desktop/snapshot/globalBestSim.txt", simGlobal + Environment.NewLine);
    File.AppendAllText(@"/home/michael/Desktop/snapshot/populationBestSim.txt", simPopulation + Environment.NewLine);
    await Task.CompletedTask;
}
