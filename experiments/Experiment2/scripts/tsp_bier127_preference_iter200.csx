// Experiment 2: Experiment~2: Timing of Human Intervention
// Algorithm: RAPGA | Problem: bier127 | Seed: 12345678

// BEGIN NAME
TSP bier127 Preference Iteration 200
// END NAME
// BEGIN SCRIPT
async (g) => {
    if (g.Iteration == 200)
    {
        //var solSim = new TspSolutionSimilarity();
        var solSim = new TspSolutionSequenceSimilarity();
        var refSol = new TspSolution([112,64,98,91,88,124,103,109,84,85,86,87,108,95,118,62,101,100,82,81,125,80,83,116,77,75,74,68,69,70]);
        g.HitlController.AddSolutionPreference(solSim, refSol, 0.2);
    }

    await Task.CompletedTask;
}
// END SCRIPT
