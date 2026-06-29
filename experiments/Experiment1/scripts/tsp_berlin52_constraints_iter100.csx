// Experiment 1: Hard Constraints on TSP berlin52 - Mid-search injection (iteration 100)
// Algorithm: GA | Problem: berlin52 | Seed: 12345678
// Execute Once: False

// BEGIN NAME
TSP berlin52 Hard Constraints (Iteration 100)
// END NAME
// BEGIN SCRIPT
async (g) => {
    if (g.Iteration == 100)
    {
        // Cluster constraint: cities 33, 34, 35, 38, 39 must appear as a consecutive block.
        // These cities form a tight geographic cluster around.
        g.HitlController.AddConstraint(new TspClusterConstraint([33, 34, 35, 38, 39]) { RepairSolution = true });

        // Fixed edge constraint: enforce edges between the two closest city pairs.
        g.HitlController.AddConstraint(new TspFixedEdgeConstraint([(34, 35), (23, 47)]) { RepairSolution = true });

        // Forbidden edge constraint: prohibit the longest possible edge.
        g.HitlController.AddConstraint(new TspForbiddenEdgeConstraint(1, 51) { RepairSolution = true });
    }
    await Task.CompletedTask;
}
// END SCRIPT
