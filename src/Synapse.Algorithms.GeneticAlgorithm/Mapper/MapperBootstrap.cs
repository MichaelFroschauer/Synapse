using GeneticSharp;
using GeneticSharp.Extensions;
using Synapse.Algorithms.GeneticAlgorithm.GsExtension;
using Synapse.OptimizationCore.Common;
using Synapse.Problems.Function;
using Synapse.Problems.JSP;
using Synapse.Problems.QAP;
using Synapse.Problems.TSP;

namespace Synapse.Algorithms.GeneticAlgorithm.Mapper;

public static class MapperBootstrap
{
    public static void RegisterAllMappings()
    {
        SolutionChromosomeRegistry.Register<TspChromosome, TspSolution>(
            chrom =>
            {
                var tour = chrom.GetGenes().Select(g => new Parameter(g.Value)).ToArray();
                return new TspSolution(tour);
            },
            sol =>
            {
                var chrom = new TspChromosome(sol.Length);
                for (int i = 0; i < sol.Length; i++)
                    chrom.ReplaceGene(i, new Gene(sol.GetParameter(i).Value));
                return chrom;
            });
        
        SolutionChromosomeRegistry.Register<FloatingPointChromosome, FunctionSolution>(
            chrom =>
            {
                var genes = chrom.GetGenes().Select(g => Convert.ToDouble(g.Value)).ToArray();
                return new FunctionSolution(genes);
            },
            sol =>
            {
                // TODO check how to set these properly
                var minValues = Enumerable.Repeat(-3.0, sol.Length).ToArray();  // The minimum values of numbers inside the chromosome.
                var maxValues = Enumerable.Repeat(3.0, sol.Length).ToArray();   // The maximum values of numbers inside the chromosome.
                var bits = Enumerable.Repeat(64 * 8, sol.Length).ToArray();        // The total bits used to represent each number.
                var fractionDigits = Enumerable.Repeat(10, sol.Length).ToArray();  // The number of fraction (scale or decimal) part of the number.
                var solutionValues = sol.GetParametersWithType();
                
                var chrom = new FloatingPointChromosome(minValues, maxValues, bits, fractionDigits, solutionValues);
                return chrom;
            });
        
        
        SolutionChromosomeRegistry.Register<QapChromosome, QapSolution>(
            chrom =>
            {
                var param = chrom.GetGenes().Select(g => new Parameter(g.Value)).ToArray();
                return new QapSolution(param);
            },
            sol =>
            {
                var chrom = new QapChromosome(sol.Length);
                for (int i = 0; i < sol.Length; i++)
                    chrom.ReplaceGene(i, new Gene(sol.GetParameter(i).Value));
                return chrom;
            });
        
        SolutionChromosomeRegistry.Register<JspChromosome, JspSolution>(
            chrom =>
            {
                var param = chrom.GetGenes().Select(g => new Parameter(g.Value)).ToArray();
                return new JspSolution(param);
            },
            sol =>
            {
                var chrom = new JspChromosome(sol.Length);
                for (int i = 0; i < sol.Length; i++)
                    chrom.ReplaceGene(i, new Gene(sol.GetParameter(i).Value));
                return chrom;
            });
    }
}
