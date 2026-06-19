using GeneticSharp;
using Synapse.Algorithms.GeneticAlgorithm.Mapper;
using Synapse.OptimizationCore.HITL;

namespace Synapse.Algorithms.GeneticAlgorithm.HITL;

public class HitlPopulation : Population
{
    private int _minPopulationFillSize;
    private readonly IHitlController? _hitlCtrl;

    public HitlPopulation(IHitlController? hitlCtrl, GeneticAlgorithmConfig gaConfig, IChromosome adamChromosome)
        : base(gaConfig.PopulationSize, gaConfig.PopulationSize, adamChromosome)
    {
        _hitlCtrl = hitlCtrl;
        _minPopulationFillSize = gaConfig.PopulationSize;
    }

    public int MinPopulationFillSize
    {
        get => _minPopulationFillSize;
        set
        {
            if (value < 2) throw new ArgumentOutOfRangeException(nameof(value));
            _minPopulationFillSize = value;
        }
    }

    public int TryFillPopulationRandomlyCount { get; set; } = 0;
    
    
    public override void CreateNewGeneration(IList<IChromosome> chromosomes)
    {
        ExceptionHelper.ThrowIfNull(nameof (chromosomes), (object) chromosomes);
        chromosomes.ValidateGenes();
        
        if (_hitlCtrl is not null && _hitlCtrl.ConstraintsExist())
        {
            UpdateHitlParameter();
            
            var sols = chromosomes.ToSolutions().ToList();
            var constraintSols = sols.Select(s => _hitlCtrl.EnforceConstraints(s)).Where(s => s != null).ToList();
            var count = TryFillPopulationRandomlyCount;
            if (count > 0)
            {
                var adamSolution = AdamChromosome.ToSolution();
                for (int i = count; i >= 0 && constraintSols.Count < MinPopulationFillSize; --i)
                {
                    var randomSolution = adamSolution.CreateRandom();
                    var newSol = _hitlCtrl.EnforceConstraints(randomSolution);
                    if (newSol is not null)
                    {
                        constraintSols.Add(newSol);
                    }
                }
            }
            var repairedSols = sols.Take(MinPopulationFillSize).Select(s => _hitlCtrl.EnforceConstraints(s, true)).ToList();
            constraintSols.AddRange(repairedSols);
            
            chromosomes = constraintSols!.ToChromosomes().ToList();
        }
        
        CurrentGeneration = new Generation(++this.GenerationsNumber, chromosomes);
        Generations.Add(this.CurrentGeneration);
        GenerationStrategy.RegisterNewGeneration((IPopulation) this);
    }


    private void UpdateHitlParameter()
    {
        if (_hitlCtrl?.TryGetParameter(nameof(HitlParameter.MinPopulationFillSize), out int fillSize) == true)
        {
            MinPopulationFillSize = fillSize;
        }
        
        if (_hitlCtrl?.TryGetParameter(nameof(HitlParameter.TryFillPopulationRandomlyCount), out int rndCount) == true)
        {
            TryFillPopulationRandomlyCount = rndCount;
        }
    }
}
