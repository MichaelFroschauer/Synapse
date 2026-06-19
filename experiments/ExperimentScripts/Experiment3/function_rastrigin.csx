// Experiment 2: Experiment~2: Timing of Human Intervention
// Algorithm: PSO | Problem: Rastrigin (10 Dimensions) | Seed: 12345678

// BEGIN NAME
Rastrigin (10 Dimensions) Preference
// END NAME
// BEGIN SCRIPT
async (g) => {
    RealValueSolutionEuclideanSimilarity solSim = new RealValueSolutionEuclideanSimilarity();
    FunctionSolution refSol = new FunctionSolution([0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0]);
    g.HitlController.AddSolutionPreference(solSim, refSol, 2.3);
    await Task.CompletedTask;
}
// END SCRIPT
