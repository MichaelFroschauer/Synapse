using GeneticSharp;

namespace Synapse.Algorithms.GeneticAlgorithm.GsExtension;

public class RandomSelection : SelectionBase
{
    private List<IChromosome> _chromosomes = new();
  
    public RandomSelection() : base(2) { }

    protected override IList<IChromosome> PerformSelectChromosomes(int number, Generation generation)
    {
      _chromosomes.Clear();
      for (int i = 0; i < number; i++)
      {
        var index = RandomizationProvider.Current.GetInt(0, generation.Chromosomes.Count);
        _chromosomes.Add(generation.Chromosomes[index]);
      }

      return _chromosomes;
    }
}
