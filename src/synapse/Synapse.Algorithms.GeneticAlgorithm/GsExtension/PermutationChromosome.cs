using GeneticSharp;

namespace Synapse.Algorithms.GeneticAlgorithm.GsExtension;

public class PermutationChromosome : ChromosomeBase
{
    private readonly int _length;

    public PermutationChromosome(int length) : base(length)
    {
        _length = length;
        int[] uniqueInts = RandomizationProvider.Current.GetUniqueInts(length, 0, length);
        for (int index = 0; index < length; ++index)
        {
            ReplaceGene(index, new Gene((object) uniqueInts[index]));
        }
    }

    public override Gene GenerateGene(int geneIndex)
    {
        return new Gene(RandomizationProvider.Current.GetInt(0, _length));
    }

    public override IChromosome CreateNew() => new PermutationChromosome(_length);
}
