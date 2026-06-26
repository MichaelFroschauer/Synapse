// Experiment 3: Preference-Based Guidance in GA and PSO
// Algorithm: GA or PSO | Problem: TSP berlin52 | Seed: 12345678
// Configuration (b): preference weight 1.2 toward a 10-city reference cluster
//
// Reference tour: 10 geographically close cities in the right-center area of berlin52,
// ordered by nearest-neighbor to form a visually good local sequence.
//   City 34 (700,580) -> 35 (685,595) -> 36 (685,610) -> 39 (720,635) ->
//   40 (760,650) -> 38 (795,645) -> 37 (770,610) -> 48 (830,610) ->
//   24 (835,625) -> 15 (845,680)
// (City numbers are 1-indexed as in the .tsp file; solution indices are 0-based.)

// BEGIN NAME
TSP berlin52 Preference (Weight 0.2)
// END NAME
// BEGIN SCRIPT
async (g) => {
    var solSim = new TspSolutionSequenceSimilarity();
    //var refSol = new TspSolution([21, 48, 38, 37, 4, 14, 42, 32, 9, 8]);
    var refSol = new TspSolution([16,2,17,30,21,0,48,31,44,18]);
    g.HitlController.AddSolutionPreference(solSim, refSol, 0.2);
    await Task.CompletedTask;
}
// END SCRIPT
