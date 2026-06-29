// Experiment 3: Preference-Based Guidance in GA and PSO
// Algorithm: GA or PSO | Problem: TSP berlin52 | Seed: 12345678
// Reference tour: 10 geographically close cities in the left-center area of berlin52.
// Execute Once: True

// BEGIN NAME
TSP berlin52 Preference (Weight 0.2)
// END NAME
// BEGIN SCRIPT
async (g) => {
    var solSim = new TspSolutionSequenceSimilarity();
    var refSol = new TspSolution([16,2,17,30,21,0,48,31,44,18]);
    g.HitlController.AddSolutionPreference(solSim, refSol, 0.2);
    await Task.CompletedTask;
}
// END SCRIPT
