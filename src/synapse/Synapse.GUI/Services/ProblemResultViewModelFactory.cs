using System;
using Synapse.GUI.ViewModels.Controls.AlgorithmResults;
using Synapse.OptimizationCore.Common;
using Synapse.OptimizationCore.Interfaces;

namespace Synapse.GUI.Services;

public class ProblemResultViewModelFactory(Func<ProblemType, ProblemResultViewModel?> factory) : IProblemResultViewModelFactory
{
    public ProblemResultViewModel? GetViewModel(ProblemType problemType) => factory(problemType);
    public ProblemResultViewModel? GetStaticViewModel(ProblemType problemType, ISolution solution)
    {
        var viewModel = factory(problemType);
        viewModel?.SetStaticSolution(solution);
        return viewModel;
    }
}
