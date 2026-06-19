using GeneticSharp;
using Synapse.Algorithms.GeneticAlgorithm.Mapper;
using Synapse.OptimizationCore.HITL;

namespace Synapse.Algorithms.GeneticAlgorithm.HITL;

public class HitlMutation : MutationBase
{
    private readonly IMutation _mutation;
    private readonly IHitlController? _hitlCtrl;

    public HitlMutation(IHitlController? hitlCtrl, IMutation mutation)
    {
        _hitlCtrl = hitlCtrl;
        _mutation = mutation;
    }
    
    protected override void PerformMutate(IChromosome chromosome, float probability)
    {
        _mutation.Mutate(chromosome, probability);
        if (_hitlCtrl is not null && _hitlCtrl.ManualEditsExist())
        {
            var solution = _hitlCtrl?.MaybeApplyManualEdit(chromosome.ToSolution());
            if (solution is not null)
            {
                chromosome.ReplaceGenes(0, solution.ToChromosome().GetGenes());    
            }
        }
    }
}
