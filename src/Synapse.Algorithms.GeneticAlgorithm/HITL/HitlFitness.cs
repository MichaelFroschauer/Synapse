using GeneticSharp;
using Synapse.Algorithms.GeneticAlgorithm.Mapper;
using Synapse.OptimizationCore.HITL;
using Synapse.OptimizationCore.Interfaces;

namespace Synapse.Algorithms.GeneticAlgorithm.HITL;

public class HitlFitness(IHitlController? hitlCtrl, IFitnessEvaluator baseFitnessEval) : IFitness
{
    public double Evaluate(IChromosome chromosome)
    {
        var sol = chromosome.ToSolution();
        return EvaluateSolution(sol);
    }
    
    public double EvaluateSolution(ISolution sol)
    {
        var fitness = hitlCtrl?.GetPreferenceAdjustedFitness(sol, baseFitnessEval) ?? baseFitnessEval.Evaluate(sol);
        if (baseFitnessEval.Minimize) fitness = -fitness;

        return fitness;
    }
}
