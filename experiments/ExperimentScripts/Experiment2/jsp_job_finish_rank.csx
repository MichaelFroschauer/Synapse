// Experiment 2: Experiment~2: Timing of Human Intervention
// Algorithm: GA | Problem: ft10 | Seed: 12345678

// BEGIN NAME
TSP bier127 Preference
// END NAME
// BEGIN SCRIPT
async (g) => {
    var constraint = new JobFinishRankConstraint((JspProblem)g.Problem, 1, 1) { RepairSolution = true };
    g.HitlController.AddConstraint(constraint);
    await Task.CompletedTask;
}
// END SCRIPT
