using GeneticSharp;
using Synapse.Algorithms.GeneticAlgorithm.Mapper;
using Synapse.OptimizationCore.HITL;

namespace Synapse.Algorithms.GeneticAlgorithm.HITL;

public class HitlCrossover : ICrossover
{
    private readonly ICrossover _baseCrossover;
    private readonly IHitlController? _hitlCtrl;
    
    public int ParentsNumber { get; }
    public int ChildrenNumber { get; }
    public int MinChromosomeLength { get; }
    public bool IsOrdered { get; }

    public HitlCrossover(IHitlController? hitlCtrl, ICrossover baseCrossover)
    {
        _hitlCtrl = hitlCtrl;
        _baseCrossover = baseCrossover;
        ParentsNumber = baseCrossover.ParentsNumber;
        ChildrenNumber = baseCrossover.ChildrenNumber;
        MinChromosomeLength = baseCrossover.MinChromosomeLength;
        IsOrdered = _baseCrossover.IsOrdered;
    }
    
    public IList<IChromosome> Cross(IList<IChromosome> parents)
    {
        var chromosomes = _baseCrossover.Cross(parents);
        // todo
        return chromosomes;
    }
}