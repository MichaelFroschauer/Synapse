// Experiment 3: Preference-Based Guidance in GA and PSO
// Algorithm: GA or PSO | Problem: Rastrigin (n=10) | Seed: 12345678
// Configuration (c): preference weight 1.3 toward global optimum x* = 0

// BEGIN NAME
Rastrigin Preference (Weight 1.3)
// END NAME
// BEGIN SCRIPT
async (g) => {
    RealValueSolutionEuclideanSimilarity solSim = new RealValueSolutionEuclideanSimilarity();
    FunctionSolution refSol = new FunctionSolution([0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0]);
    g.HitlController.AddSolutionPreference(solSim, refSol, 1.3);
    await Task.CompletedTask;
}
// END SCRIPT
