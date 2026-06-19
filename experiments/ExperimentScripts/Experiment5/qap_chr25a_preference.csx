// Experiment 5: RAPGA Diversity Threshold and User Preferences — Preference script
// Algorithm: RAPGA | Problem: chr25a (QAP, N=25) | Seed: 12345678
//
// This script is used for configurations (b), (c), and (d).
// The similarity diversity threshold is set in the RAPGA algorithm configuration:
//   (b) threshold = 0.95 (default)
//   (c) threshold = 0.80 (low — accepts more similar offspring)
//   (d) threshold = 0.99 (high — rejects nearly all similar offspring)
// Configuration (a) is the baseline: no preference, threshold = 0, no script.

// BEGIN NAME
QAP chr25a Similarity Preference (Weight 0.2)
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

    // Similarity-based preference using QAP assignment match similarity.
    // Weight 0.2: solutions more similar to the reference are scored higher.
    g.HitlController.AddSolutionPreference(
        new QapSolutionSimilarity(),
        referenceSolution,
        weight: 0.2,
        name: "central-facilities-preference");

    await Task.CompletedTask;
}
// END SCRIPT
