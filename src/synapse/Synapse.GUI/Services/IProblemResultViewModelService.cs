using System;
using Synapse.GUI.ViewModels.Controls.AlgorithmResults;
using Synapse.OptimizationCore.Common;
using Synapse.OptimizationCore.Interfaces;

namespace Synapse.GUI.Services;

public interface IProblemResultViewModelService
{
    void SetProblemType(ProblemType problemType);
    ProblemResultViewModel? CurrentProblemResultViewModel { get; }
    ProblemResultViewModel? BestProblemResultViewModel { get; }
    ProblemResultViewModel? CreateProblemResultViewModel(ISolution solution);
    event Action? CurrentProblemTypeChanged;
}
