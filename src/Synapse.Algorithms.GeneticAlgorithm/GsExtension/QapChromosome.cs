using GeneticSharp;

namespace Synapse.Algorithms.GeneticAlgorithm.GsExtension;

public class QapChromosome : ChromosomeBase
{
    private readonly int _n;
    
    public QapChromosome(int length) : base(length)
    {
        _n = length;
        var perm = RandomizationProvider.Current.GetUniqueInts(_n, 0, _n);
        for (int i = 0; i < _n; i++)
        {
            ReplaceGene(i, new Gene(perm[i]));
        }
    }

    public override Gene GenerateGene(int geneIndex)
    {
        return new Gene(RandomizationProvider.Current.GetInt(0, _n));
    }

    public override IChromosome CreateNew()
    {
        return new QapChromosome(_n);
    }
}
