using Synapse.GUI.ViewModels.Controls.AlgorithmResults;
using Synapse.OptimizationCore.Common;
using Synapse.OptimizationCore.Interfaces;

namespace Synapse.GUI.Services;

public interface IProblemResultViewModelFactory
{
    ProblemResultViewModel? GetViewModel(ProblemType problemType);
    ProblemResultViewModel? GetStaticViewModel(ProblemType problemType, ISolution solution);
}
