// Experiment 6: Direct Solution Edits versus Constraint-Based Steering
// Algorithm: GA | Problem: TSP berlin52 | Seed: 12345678
// Execute Once: False

// BEGIN NAME
TSP berlin52 Constraint
// END NAME
// BEGIN SCRIPT
async (g) => {
    if (g.Iteration == 100)
    {
        g.HitlController.AddConstraint(new TspClusterConstraint([16, 2, 44, 30, 20]));
        g.HitlController.AddConstraint(new TspClusterConstraint([17, 33]));
        await Task.CompletedTask;
    }
}
// END SCRIPT
