using Synapse.OptimizationCore.Common.Impl;
using Synapse.OptimizationCore.Interfaces;
using Synapse.Problems.JSP;
using Synapse.Problems.QAP;
using Synapse.Problems.TSP;

namespace Synapse.Test;

public class TestSimilarityMeasures : IMetaheuristicTester
{
    private static void CheckSimilarity(ISolution s1, ISolution s2, ISolutionSimilarity sim)
    {
        double similarity1 = sim.GetSimilarity(s1, s2);
        
        Console.WriteLine("=================================================================");
        Console.WriteLine($"{s1.GetType().Name} | {sim.GetType().Name}");
        Console.WriteLine(s1.ToString());
        Console.WriteLine(s2.ToString());
        Console.WriteLine($"Similarity : {similarity1:P3}%\n");
    }
    
    public static Task Run()
    {
        CheckSimilarity(
            new PermutationSolution(new int[] { 1, 4, 3, 2, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 }),
            new PermutationSolution(new int[] { 1, 4, 3 }),
            new PermutationSolutionSimilarity());
        
        CheckSimilarity(
            new PermutationSolution(new int[] { 1, 4, 3, 2, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 }),
            new PermutationSolution(new int[] { 1, 4, 3 }),
            new PermutationSolutionSequenceSimilarity());
        
        CheckSimilarity(
            new TspSolution(new int[] { 1, 2, 3, 4, 5 }),
            new TspSolution(new int[] { 5, 1, 2, 3, 4 }),
            new TspSolutionSimilarity());
        
        CheckSimilarity(
            new TspSolution(new int[] { 1, 2, 3, 4, 5 }),
            new TspSolution(new int[] { 4, 3, 2, 1, 5 }),
            new TspSolutionSimilarity());
        
        CheckSimilarity(
            new TspSolution(new int[] { 1, 2, 3, 4, 5 }),
            new TspSolution(new int[] { 1, 2 }),
            new TspSolutionSimilarity());
        
        CheckSimilarity(
            new QapSolution(new int [] { 1, 4, 3, 2, 5 }),
            new QapSolution(new int [] { 1, 2, 3, 4, 5 }),
            new QapSolutionSimilarity());
        
        CheckSimilarity(
            new RealValueSolution(new double [] { 1, 4, 3, 2, 5 }),
            new RealValueSolution(new double [] { 1, 2, 3, 4, 5 }),
            new RealValueSolutionCosineSimilarity());
        
        CheckSimilarity(
            new RealValueSolution(new double [] { 1, 4, 3, 2, 5 }),
            new RealValueSolution(new double [] { 1, 2, 3, 4, 5 }),
            new RealValueSolutionEuclideanSimilarity());
        
        var jsp1 = new JspSolution(3, 2);
        jsp1.SetParameters(new int[]  { 1, 2, 3, 4, 6, 5 });
        var jsp2 = new JspSolution(3, 2);
        jsp2.SetParameters(new int[]  { 1, 2, 3, 4, 5, 6 });
        CheckSimilarity(jsp1, jsp2, new JspSolutionSimilarity());


        var baseline = "7 12 23 6 14 19 20 1 21 9 17 2 24 8 16 18 13 4 15 22 10 3 5 11 0".Split(' ').Select(int.Parse).ToArray();
        var preferenceOnly = "7 13 9 19 2 14 21 5 20 0 17 24 18 22 1 6 23 3 4 15 10 12 11 16 8".Split(' ').Select(int.Parse).ToArray();
        var preferenceThreshold80 = "5 11 9 0 14 1 24 17 23 7 19 3 16 4 20 18 10 12 13 15 2 22 21 6 8".Split(' ').Select(int.Parse).ToArray();
        var preferenceThreshold99 = "1 11 3 19 14 2 13 17 16 9 7 23 6 10 20 18 24 15 4 8 22 5 21 12 0".Split(' ').Select(int.Parse).ToArray();
        
        var referenceSolution = new QapSolution(new int[] {
            0, 1, 2, 3, 4, 5, 6, 7, 8, 9,
            11, 10, 12, 22, 16, 13, 14, 15,
            17, 18, 19, 20, 21, 23, 24
        });
        Console.WriteLine("Baseline");
        CheckSimilarity(referenceSolution, new QapSolution(baseline), new QapSolutionSimilarity());
        Console.WriteLine("Preference Only");
        CheckSimilarity(referenceSolution, new QapSolution(preferenceOnly), new QapSolutionSimilarity());
        Console.WriteLine("Preference Threshold 80%");
        CheckSimilarity(referenceSolution, new QapSolution(preferenceThreshold80), new QapSolutionSimilarity());
        Console.WriteLine("Preference Threshold 99%");
        CheckSimilarity(referenceSolution, new QapSolution(preferenceThreshold99), new QapSolutionSimilarity());
        
        return Task.CompletedTask;
    }
}
