// Experiment 6: Direct Solution Edits versus Constraint-Based Steering
// Algorithm: GA | Problem: TSP berlin52 | Seed: 12345678

// BEGIN NAME
TSP berlin52 Manual Edit
// END NAME
// BEGIN SCRIPT
async (g) => {
    var edited = new TspSolution(new int[]{ 16, 17 });
    g.HitlController.AddManualEdit(edited, new ReverseSegmentByValuesManualEditApplier(), 0.9, 10);

    await Task.CompletedTask;
}
// END SCRIPT
