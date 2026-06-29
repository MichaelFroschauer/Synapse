// Experiment 5: RAPGA Diversity Threshold and User Preferences - Preference script
// Algorithm: RAPGA | Problem: chr25a (QAP, N=25) | Seed: 12345678
//
// This script is used for configurations (b), (c), and (d).
// The similarity diversity threshold is set in the RAPGA algorithm configuration:
//   (a) is the baseline: no preference, threshold = 0, no script.
//   (b) threshold = 0.0 (default)
//   (c) threshold = 0.80 (low - accepts more similar offspring)
//   (d) threshold = 0.99 (high - rejects nearly all similar offspring)
// Execute Once: True

// BEGIN NAME
QAP chr25a Similarity Preference (Weight 0.2)
// END NAME
// BEGIN SCRIPT
async (g) => {
    
    // Reference solution: three highest-flow facilities placed at central locations.
    var referenceSolution = new QapSolution(new int[] {
        0, 1, 2, 3, 4, 5, 6, 7, 8, 9,
        11, 10, 12, 22, 16, 13, 14, 15,
        17, 18, 19, 20, 21, 23, 24
    });

    // Similarity-based preference using QAP assignment match similarity.
    g.HitlController.AddSolutionPreference(
        new QapSolutionSimilarity(),
        referenceSolution,
        weight: 0.2,
        name: "central-facilities-preference");

    await Task.CompletedTask;
}
// END SCRIPT
