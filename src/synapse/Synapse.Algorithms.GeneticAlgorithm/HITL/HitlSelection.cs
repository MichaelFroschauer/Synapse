using GeneticSharp;
using Synapse.Algorithms.GeneticAlgorithm.Mapper;
using Synapse.OptimizationCore.Common.Impl;
using Synapse.OptimizationCore.HITL;
using Synapse.OptimizationCore.Random;

namespace Synapse.Algorithms.GeneticAlgorithm.HITL;

public class HitlSelection(IHitlController? hitlCtrl, ISelection baseSelection1, ISelection? baseSelection2 = null) : ISelection
{
    private readonly IRandom _rnd = RandomProvider.Value;
    
    public IList<IChromosome> SelectChromosomes(int number, Generation generation)
    {
        IList<IChromosome> selection;
        if (hitlCtrl is not null && 
            hitlCtrl.AskPreferenceInterval != 0 && 
            hitlCtrl.AskPreference is not null &&
            generation.Number % hitlCtrl.AskPreferenceInterval == 0)
        {
            var selection1 = baseSelection1.SelectChromosomes(number, generation);
            var selection2 = baseSelection2 is not null 
                ? baseSelection2.SelectChromosomes(number, generation) 
                : baseSelection1.SelectChromosomes(number, generation);;
        
            var bestSelection1 = selection1.OrderBy(s => s.Fitness).ToList().GetRange(0, 1);
            var bestSelection2 = selection2.OrderBy(s => s.Fitness).ToList().GetRange(0, 1);
            
            var solutions1 = bestSelection1.ToSolutions();
            var solutions2 = bestSelection2.ToSolutions();
            hitlCtrl.AskPreference(solutions1, solutions2);
            //var userSelection = hitlCtrl.GetUserResponseAsync().GetAwaiter().GetResult();
            var userSelection = hitlCtrl.GetUserResponseAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            //var userSelection = 1;
            if (userSelection < 1 || userSelection > 2)
            {
                Console.WriteLine($"{nameof(HitlSelection)} user selection out of range, choosing selection 1");
            }
            selection = userSelection switch
            {
                1 => selection1,
                2 => selection2,
                _ => selection1
            };
            
            // Prefer similar solutions in the future
            var solutionSelection = selection.First().ToSolution();
            hitlCtrl.AddSolutionPreference(hitlCtrl.GetSolutionSimilarity(solutionSelection), solutionSelection,
                hitlCtrl.SolutionSimilarityPreferenceWeight, "HitlSelectionAskPreferenceSolution");
        }
        else
        {
            // TODO maybe not choose one for a whole iteration but maybe mix them up
            if (baseSelection2 is null || _rnd.GetDouble() > 0.5)
            {
                selection = baseSelection1.SelectChromosomes(number, generation);    
            }
            else
            {
                selection = baseSelection2.SelectChromosomes(number, generation);
            }
        }

        return selection;
    }
}
