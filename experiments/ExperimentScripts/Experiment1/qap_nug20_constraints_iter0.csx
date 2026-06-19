// Experiment 1: Hard Constraints on QAP nug20 — Early injection (iteration 0)
// Algorithm: GA | Problem: nug20 | Seed: 12345678

// BEGIN NAME
QAP nug20 Hard Constraints (Iteration 0)
// END NAME
// BEGIN SCRIPT
async (g) => {

    if (g.Iteration <= 1)
    {
        var qapProblem = (QapProblem)g.Problem;

        // Cluster constraint: facilities 1, 14, 19 (three highest total-flow facilities)
        // are restricted to locations 6, 7, 11, 12 (central positions on the 4×5 grid).
        g.HitlController.AddConstraint(
            new QapClusterConstraint(
                facilities: new[] { 1, 14, 19 },
                allowedLocations: new[] { 6, 7, 11, 12 }) { RepairSolution = true });

        // Fix assignment: facility 1 (highest total flow = 162) pinned to location 6 (central).
        g.HitlController.AddConstraint(
            new QapFixAssignmentConstraint(facility: 1, location: 6) { RepairSolution = true });

        // Pairwise constraint: facilities 14 and 19 (bidirectional flow = 20)
        // must be within distance 2 on the location grid.
        g.HitlController.AddConstraint(
            new QapPairwiseAssignmentConstraint(qapProblem, facilityA: 14, facilityB: 19, maxDistance: 2.0) { RepairSolution = true });
    }
    await Task.CompletedTask;
}
// END SCRIPT
