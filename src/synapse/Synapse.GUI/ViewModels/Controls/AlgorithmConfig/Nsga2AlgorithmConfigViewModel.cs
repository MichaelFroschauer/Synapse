using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ActiproSoftware.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using Synapse.Algorithms.GeneticAlgorithm.Crossover;
using Synapse.Algorithms.GeneticAlgorithm.Mutator;
using Synapse.Algorithms.NSGA_II;
using Synapse.GUI.Services;
using Synapse.JobManagement;
using Synapse.OptimizationCore.Common;
using Synapse.OptimizationCore.Interfaces;
using Synapse.Reflection;

namespace Synapse.GUI.ViewModels.Controls.AlgorithmConfig;

public partial class Nsga2AlgorithmConfigViewModel : BaseAlgorithmConfigViewModel
{
    protected sealed override IAlgorithmConfig Config { get; set; }

    private Nsga2AlgorithmConfig TypedConfig
    {
        get => (Nsga2AlgorithmConfig)Config;
        set => Config = value;
    }

    private List<Type> Crossovers { get; set; } = new();
    public ObservableCollection<string> AvailableCrossovers { get; } = new();

    private List<Type> Mutators { get; set; } = new();
    public ObservableCollection<string> AvailableMutations { get; } = new();

    [ObservableProperty] private string _objectiveImportanceWeights = string.Empty;
    [ObservableProperty] private int _populationSize;
    [ObservableProperty] private int _tournamentSize;
    [ObservableProperty] private string? _crossover;
    [ObservableProperty] private string? _mutation;
    [ObservableProperty] private double _crossoverProbability;
    [ObservableProperty] private double _mutationProbability;

    public Nsga2AlgorithmConfigViewModel(
        INavigationService navigationService,
        IJobManager jobManager,
        IJobSelectorService jobSelector,
        ITypeRegistry typeRegistry)
        : base(navigationService, jobManager, jobSelector, typeRegistry)
    {
        AlgorithmType = AlgorithmType.NSGA_II;
        SetAvailableConfig();

        JobActive = false;
        var jobInfo = _jobManager.GetJobInfo(_jobSelector.SelectedJobId);
        if (jobInfo is not null && jobInfo.Config.AlgorithmType == AlgorithmType.NSGA_II)
        {
            Config = (jobInfo.Config as Nsga2AlgorithmConfig)!;
            JobActive = true;
        }
        else
        {
            Config = new Nsga2AlgorithmConfig();
        }

        SetValuesFromConfig();
        UpdateConfig();
    }

    protected override void SetValuesFromConfig()
    {
        base.SetValuesFromConfig();

        ObjectiveImportanceWeights = string.Join(", ", TypedConfig.ObjectiveImportanceWeights.Select(w => w.ToString("0.###")));
        PopulationSize = TypedConfig.PopulationSize;
        TournamentSize = TypedConfig.TournamentSize;
        Crossover = TypedConfig.Crossover?.GetType().Name;
        Mutation = TypedConfig.Mutation?.GetType().Name;
        CrossoverProbability = TypedConfig.CrossoverProbability;
        MutationProbability = TypedConfig.MutationProbability;
    }

    private void SetAvailableConfig()
    {
        Crossovers = _typeRegistry!.GetAssignableTypes<ICrossover>().ToList();
        var crossovers = Crossovers
            .Where(t => t is not null && !string.IsNullOrWhiteSpace(t.Name))
            .Select(t => t!.Name!)
            .ToList();
        AvailableCrossovers.AddRange(crossovers);

        Mutators = _typeRegistry.GetAssignableTypes<IMutation>().ToList();
        var mutations = Mutators
            .Where(t => t is not null && !string.IsNullOrWhiteSpace(t.Name))
            .Select(t => t!.Name!)
            .ToList();
        AvailableMutations.AddRange(mutations);
    }

    partial void OnPopulationSizeChanged(int value)
    {
        TypedConfig.PopulationSize = value;
        UpdateConfig();
    }

    partial void OnObjectiveImportanceWeightsChanged(string value)
    {
        var parsed = value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(v => double.TryParse(v, out var d) ? d : double.NaN)
            .Where(d => !double.IsNaN(d) && d >= 0)
            .ToList();

        TypedConfig.ObjectiveImportanceWeights = parsed;
        UpdateConfig();
    }

    partial void OnTournamentSizeChanged(int value)
    {
        TypedConfig.TournamentSize = value;
        UpdateConfig();
    }

    partial void OnCrossoverChanged(string? value)
    {
        var crossoverType = Crossovers.FirstOrDefault(t => t?.Name == value);
        if (crossoverType is null) return;
        var crossover = crossoverType.CreateInstanceWithDefaults() as ICrossover;
        if (crossover is null) return;
        TypedConfig.Crossover = crossover;
        UpdateConfig();
    }

    partial void OnMutationChanged(string? value)
    {
        var mutationType = Mutators.FirstOrDefault(t => t?.Name == value);
        if (mutationType is null) return;
        var mutation = mutationType.CreateInstanceWithDefaults() as IMutation;
        if (mutation is null) return;
        TypedConfig.Mutation = mutation;
        UpdateConfig();
    }

    partial void OnCrossoverProbabilityChanged(double value)
    {
        TypedConfig.CrossoverProbability = value;
        UpdateConfig();
    }

    partial void OnMutationProbabilityChanged(double value)
    {
        TypedConfig.MutationProbability = value;
        UpdateConfig();
    }

    public override void SetConfig(BaseAlgorithmConfigViewModel? currentAlgorithmConfigViewModel)
    {
        if (currentAlgorithmConfigViewModel is Nsga2AlgorithmConfigViewModel nsga2Vm)
        {
            JobActive = false;
            Config = (Nsga2AlgorithmConfig)nsga2Vm.Config.Clone();
            SetValuesFromConfig();
            UpdateConfig();
        }
    }
}
