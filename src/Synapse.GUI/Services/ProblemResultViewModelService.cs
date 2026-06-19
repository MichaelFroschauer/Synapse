using System;
using Synapse.GUI.ViewModels.Controls.AlgorithmResults;
using Synapse.OptimizationCore.Common;
using Synapse.OptimizationCore.Interfaces;

namespace Synapse.GUI.Services;

public class ProblemResultViewModelService(IProblemResultViewModelFactory factory) : IProblemResultViewModelService
{
    public void SetProblemType(ProblemType problemType)
    {
        _currentProblemType = problemType;
        _currentProblemTypeChanged = true;
        _bestProblemTypeChanged = true;
        NotifyCurrentProblemTypeChanged();
    }

    private ProblemType _currentProblemType = ProblemType.Unknown;
    private ProblemResultViewModel? _currentProblemResultViewModel;
    private bool _currentProblemTypeChanged;
    private ProblemResultViewModel? _bestProblemResultViewModel;
    private bool _bestProblemTypeChanged;

    public ProblemResultViewModel? CurrentProblemResultViewModel
    {
        get
        {
            if (_currentProblemTypeChanged)
            {
                _currentProblemResultViewModel = factory.GetViewModel(_currentProblemType);
                _currentProblemTypeChanged = false;    
            }
            return _currentProblemResultViewModel;
        }
    }
    
    public ProblemResultViewModel? BestProblemResultViewModel
    {
        get
        {
            if (_bestProblemTypeChanged)
            {
                _bestProblemResultViewModel = factory.GetViewModel(_currentProblemType);
                _bestProblemTypeChanged = false;    
            }
            return _bestProblemResultViewModel;
        }
    }

    public ProblemResultViewModel? CreateProblemResultViewModel(ISolution solution)
    {
        return factory.GetStaticViewModel(_currentProblemType, solution);
    }

    public event Action? CurrentProblemTypeChanged;
    private void NotifyCurrentProblemTypeChanged() => CurrentProblemTypeChanged?.Invoke();
}
