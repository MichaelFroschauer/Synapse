// Experiment 5: RAPGA Diversity Threshold and User Preferences — Measure Final Similarity
// Algorithm: RAPGA | Problem: chr25a (QAP, N=25) | Seed: 12345678
//
// This script is used for measuring the final solution similarity to the reference solution 
// at the end of the search for configurations (b), (c), and (d) from the preference script.
// The similarity is measured using the same QAP assignment match similarity as in the preference.

// BEGIN NAME
QAP chr25a Similarity Measurement
// END NAME
// BEGIN SCRIPT
async (g) => {
    
    // Reference solution: three highest-flow facilities placed at central locations.
    //   Facility 13 (total flow 1900) -> Location 22 (most central, total dist 5)
    //   Facility 14 (total flow 1644) -> Location 16 (total dist 7)
    //   Facility 11 (total flow 1562) -> Location 10 (total dist 8)
    // Remaining facilities are assigned to the remaining locations in order.
    var referenceSolution = new QapSolution(new int[] {
        0, 1, 2, 3, 4, 5, 6, 7, 8, 9,
        11, 10, 12, 22, 16, 13, 14, 15,
        17, 18, 19, 20, 21, 23, 24
    });

    var solutionSimilarity = new QapSolutionSimilarity();
    var similarity = solutionSimilarity.GetSimilarity(g.Best, referenceSolution);

    System.Console.WriteLine($"Final solution similarity to reference: {similarity:F4}");
    System.Console.WriteLine($"Iteration: {g.Iteration}");

    await Task.CompletedTask;
}
// END SCRIPT
