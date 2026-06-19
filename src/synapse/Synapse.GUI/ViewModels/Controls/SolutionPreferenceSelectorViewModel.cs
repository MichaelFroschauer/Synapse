using System;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Threading;
using AvaloniaEdit.Utils;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Synapse.Algorithms.NSGA_II;
using Synapse.GUI.Services;
using Synapse.GUI.ViewModels.Controls.AlgorithmResults;
using Synapse.OptimizationCore.Common;
using Synapse.OptimizationCore.Interfaces;
using Synapse.Reflection;
using Synapse.Reflection.Models;

namespace Synapse.GUI.ViewModels.Controls;

public partial class SolutionPreferenceSelectorViewModel : ViewModelBase
{
    private readonly IJobSelectorService _jobSelector;
    private readonly IProblemResultViewModelService _problemResultViewModelService;
    private readonly ITypeRegistry _typeRegistry;
    [ObservableProperty] private ProblemResultViewModel? _solutionChoice1;
    [ObservableProperty] private ProblemResultViewModel? _solutionChoice2;
    [ObservableProperty] private bool _solutionChoiceAvailable = false;
    [ObservableProperty] private int _solutionChoiceIteration = 0;
    
    public ObservableCollection<UserSettableClass> AvailableSolutionSimilarities { get; } = new();
    [ObservableProperty] private UserSettableClass? _selectedSolutionSimilarity;
    [ObservableProperty] private double _solutionSimilarityPreferenceWeight = 0.2;
    [ObservableProperty] private bool _showObjectiveWeightControls;
    [ObservableProperty] private string _runtimeObjectiveWeights = string.Empty;

    private const string ObjectiveWeightsParamKey = "nsga2.objectiveWeights";
    
    public SolutionPreferenceSelectorViewModel(
        IJobSelectorService jobSelector,
        IProblemResultViewModelService problemResultViewModelService,
        ITypeRegistry typeRegistry)
    {
        _jobSelector = jobSelector;
        _problemResultViewModelService = problemResultViewModelService;
        _typeRegistry = typeRegistry;

        _jobSelector.SelectedJobChanged += OnSelectedJobChanged;
        OnSelectedJobChanged(null, null);
    }

    private void OnSelectedJobChanged(object? sender, SelectedJobChangedEventArgs? e)
    {
        if (_jobSelector.SelectedJobInfo is null || _jobSelector.SelectedJobInfo.Config.HitlController is null) return;

        ShowObjectiveWeightControls = _jobSelector.SelectedJobInfo.Config.AlgorithmType == AlgorithmType.NSGA_II;
        if (ShowObjectiveWeightControls)
        {
            if (_jobSelector.SelectedJobInfo.Config is Nsga2AlgorithmConfig nsga2Config)
            {
                RuntimeObjectiveWeights = string.Join(", ", nsga2Config.ObjectiveImportanceWeights.Select(w => w.ToString("0.###")));
            }

            // Seed runtime parameter once so the algorithm can read it before first AskPreference interaction.
            OnRuntimeObjectiveWeightsChanged(RuntimeObjectiveWeights);
        }

        // Add current applicable solution similarity classes
        var currentSolution = _jobSelector.SelectedJobInfo.Problem.CreateRandomSolution();
        SelectedSolutionSimilarity = currentSolution.GetDefaultSolutionSimilarityClass().GetType().BuildUserSettableClass();
        AvailableSolutionSimilarities.Clear();
        var solutionSimilarityClasses = _typeRegistry
            .GetAssignableTypes(typeof(ISolutionSimilarity))
            .BuildUserSettableClasses().ApplicableToSolution(currentSolution.GetType()).ToList();
        AvailableSolutionSimilarities.AddRange(solutionSimilarityClasses);

        // TODO if user switches the current job
        _jobSelector.SelectedJobInfo.Config.HitlController.AskPreference = (s1, s2) =>
        {
            var sol1 = s1.ToList().First();
            var sol2 = s2.ToList().First();
            if (sol1.Fitness != null && sol2.Fitness != null &&
                Math.Abs(sol1.Fitness ?? 0.0 - sol2.Fitness ?? 0.0) < 1.0)
            {
                _jobSelector.SelectedJobInfo.Config.HitlController.SetUserPreference(1);
                return;
            };
            
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                SolutionChoiceAvailable = true;
                SolutionChoice1 = _problemResultViewModelService.CreateProblemResultViewModel(sol1);
                SolutionChoice2 = _problemResultViewModelService.CreateProblemResultViewModel(sol2);
                OnPropertyChanged(nameof(SolutionChoice1));
                OnPropertyChanged(nameof(SolutionChoice2));
            });
        };
    }

    partial void OnSolutionChoiceIterationChanged(int value)
    {
        if (_jobSelector.SelectedJobInfo is null || _jobSelector.SelectedJobInfo.Config.HitlController is null) return;
        if (value < 0) value = 0;
        _jobSelector.SelectedJobInfo.Config.HitlController.AskPreferenceInterval = value;
    }

    partial void OnSelectedSolutionSimilarityChanged(UserSettableClass? value)
    {
        if (value is not null &&
            value.TryGetInstance<ISolutionSimilarity>(out var solutionSimilarity, out _) &&
            solutionSimilarity is not null)
        {
            var hitlCtrl = _jobSelector.SelectedJobInfo?.Config.HitlController;
            if (hitlCtrl is not null)
            {
                hitlCtrl.SetSolutionSimilarity(solutionSimilarity);
                hitlCtrl.SolutionSimilarityPreferenceWeight = SolutionSimilarityPreferenceWeight;
            }
        }
    }

    partial void OnSolutionSimilarityPreferenceWeightChanged(double value)
    {
        var hitlCtrl = _jobSelector.SelectedJobInfo?.Config.HitlController;
        if (hitlCtrl is not null)
        {
            hitlCtrl.SolutionSimilarityPreferenceWeight = value;
        }
    }

    partial void OnRuntimeObjectiveWeightsChanged(string value)
    {
        var hitlCtrl = _jobSelector.SelectedJobInfo?.Config.HitlController;
        if (hitlCtrl is null || !ShowObjectiveWeightControls)
            return;

        var parsed = value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(v => double.TryParse(v, out var d) ? d : double.NaN)
            .Where(d => !double.IsNaN(d) && d > 0)
            .ToArray();

        if (parsed.Length > 0)
            hitlCtrl.SetParameter(ObjectiveWeightsParamKey, parsed);
    }

    [RelayCommand]
    private void UserSelectSolution(int selection)
    {
        if (!SolutionChoiceAvailable || _jobSelector.SelectedJobInfo?.Config.HitlController is null) return;

        if (selection is >= 1 and <= 2)
        {
            SolutionChoiceAvailable = false;
            _jobSelector.SelectedJobInfo.Config.HitlController.SetUserPreference(selection);
        }
    }

    protected override void DisposeManaged()
    {
        _jobSelector.SelectedJobChanged -= OnSelectedJobChanged;
        base.DisposeManaged();
    }
}
