// Experiment 3: Preference-Based Guidance in GA and PSO
// Algorithm: GA or PSO | Problem: Rastrigin (n=10) | Seed: 12345678
// Configuration (b): preference weight 1.0 toward global optimum x* = 0

// BEGIN NAME
Rastrigin Preference (Weight 1.0)
// END NAME
// BEGIN SCRIPT
async (g) => {
    RealValueSolutionEuclideanSimilarity solSim = new RealValueSolutionEuclideanSimilarity();
    FunctionSolution refSol = new FunctionSolution([0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0]);
    g.HitlController.AddSolutionPreference(solSim, refSol, 1.0);
    await Task.CompletedTask;
}
// END SCRIPT
