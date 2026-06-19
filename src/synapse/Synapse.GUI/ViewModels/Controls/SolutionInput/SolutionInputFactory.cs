using Synapse.OptimizationCore.Interfaces;
using Synapse.Problems.QAP;
using Synapse.Problems.TSP;

namespace Synapse.GUI.ViewModels.Controls.SolutionInput;

public class SolutionInputFactory
{
    public SolutionInputViewModel? GetViewModel(ISolution solution)
    {
        return solution switch
        {
            TspSolution t => new TspInputViewModel(),
            QapSolution q => new QapInputViewModel(),
            _ => (SolutionInputViewModel?)null
        };
    }
}
