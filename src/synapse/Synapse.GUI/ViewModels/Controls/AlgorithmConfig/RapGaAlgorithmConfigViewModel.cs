using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using AvaloniaEdit.Utils;
using CommunityToolkit.Mvvm.ComponentModel;
using Synapse.Algorithms.GeneticAlgorithm.Crossover;
using Synapse.Algorithms.GeneticAlgorithm.Mutator;
using Synapse.Algorithms.GeneticAlgorithm.Selector;
using Synapse.Algorithms.RAPGA;
using Synapse.GUI.Services;
using Synapse.GUI.ViewModels.Controls.AlgorithmConfig.RAPGA;
using Synapse.JobManagement;
using Synapse.OptimizationCore.Common;
using Synapse.OptimizationCore.Interfaces;
using Synapse.Reflection;

namespace Synapse.GUI.ViewModels.Controls.AlgorithmConfig;

public partial class RapGaAlgorithmConfigViewModel : BaseAlgorithmConfigViewModel
{
    protected sealed override IAlgorithmConfig Config { get; set; }
    private RAPGAConfig TypedConfig {
        get => (RAPGAConfig)Config;
        set => Config = value;
    }

    private List<Type> Selectors { get; set; } = new();
    public ObservableCollection<string> AvailableSelections { get; } = new();
    private List<Type> Crossovers { get; set; } = new();
    public ObservableCollection<string> AvailableCrossovers { get; } = new();
    private List<Type> Mutators { get; set; } = new();
    public ObservableCollection<string> AvailableMutations { get; } = new();
    
    [ObservableProperty] private RapGaConfigViewModel? _selectedSelection1Config;
    [ObservableProperty] private RapGaConfigViewModel? _selectedSelection2Config;
    
    [ObservableProperty] private int _populationSize;
    [ObservableProperty] private string? _selection1;
    [ObservableProperty] private string? _selection2;
    [ObservableProperty] private string? _crossover;
    [ObservableProperty] private string? _mutation;
    [ObservableProperty] private double _mutationProbability;
    [ObservableProperty] private double _comparisonFactor;
    [ObservableProperty] private double _similarityDiversityThreshold;
    [ObservableProperty] private int _effort;
    [ObservableProperty] private int _minimumPopulationSize;
    [ObservableProperty] private int _maximumPopulationSize;
    [ObservableProperty] private int _elites;

    private PropertyChangedEventHandler? _selectionConfigHandler;
    private PropertyChangedEventHandler? _selection2ConfigHandler;
    
    public RapGaAlgorithmConfigViewModel(INavigationService navigationService, IJobManager jobManager,
        IJobSelectorService jobSelector, ITypeRegistry typeRegistry)
        : base(navigationService, jobManager, jobSelector, typeRegistry)
    {
        AlgorithmType = AlgorithmType.RAPGA;
        SetAvailableConfig();
        
        JobActive = false;
        var jobInfo = _jobManager.GetJobInfo(_jobSelector.SelectedJobId);
        if (jobInfo is not null && jobInfo.Config.AlgorithmType == AlgorithmType.RAPGA)
        {
            Config = (jobInfo.Config as RAPGAConfig)!;
            JobActive = true;
        }
        else
        {
            Config = new RAPGAConfig();
        }
        
        SetValuesFromConfig();
        UpdateConfig();
    }
    
    private void SetAvailableConfig()
    {
        if (_typeRegistry is null) return;
        
        Selectors = _typeRegistry.GetAssignableTypes<ISelection>().ToList();
        var selectors = Selectors
            .Where(t => t is not null && !string.IsNullOrWhiteSpace(t.Name))
            .Select(t => t!.Name!)
            .ToList();
        AvailableSelections.Add("");
        AvailableSelections.AddRange(selectors);
        
        Crossovers = _typeRegistry.GetAssignableTypes<ICrossover>().ToList();
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

    private RapGaConfigViewModel? CreateSelectionConfigFor(string? name, bool primary)
    {
        return name switch
        {
            nameof(TournamentSelection) => new RapGaTournamentSelectionConfigViewModel(),
            _ => new RapGaGenericSelectionConfigViewModel()
        };
    }
    
    private void SubscribeToSelectionConfig(RapGaConfigViewModel? vm, bool primary)
    {
        if (primary)
        {
            // unsubscribe from old
            if (_selectionConfigHandler is not null && SelectedSelection1Config is not null)
                SelectedSelection1Config.PropertyChanged -= _selectionConfigHandler;

            if (vm is null)
            {
                _selectionConfigHandler = null;
                return;
            }

            _selectionConfigHandler = (s, e) =>
            {
                vm.ApplyTo(TypedConfig, null, primary: true);
                UpdateConfig();
            };

            vm.PropertyChanged += _selectionConfigHandler;
        }
        else
        {
            // same logic for secondary selection
            if (_selection2ConfigHandler is not null && SelectedSelection2Config is not null)
                SelectedSelection2Config.PropertyChanged -= _selection2ConfigHandler;

            if (vm is null)
            {
                _selection2ConfigHandler = null;
                return;
            }

            _selection2ConfigHandler = (s, e) =>
            {
                vm.ApplyTo(TypedConfig, null, primary: false);
                UpdateConfig();
            };

            vm.PropertyChanged += _selection2ConfigHandler;
        }
    }
    
    protected override void SetValuesFromConfig()
    {
        base.SetValuesFromConfig();
        PopulationSize = TypedConfig.PopulationSize;
        Selection1 = TypedConfig.Selection1?.GetType().Name;
        Selection2 = TypedConfig.Selection2?.GetType().Name;
        Crossover = TypedConfig.Crossover?.GetType().Name;
        Mutation = TypedConfig.Mutation?.GetType().Name;
        MutationProbability = TypedConfig.MutationProbability;
        ComparisonFactor = TypedConfig.ComparisonFactor;
        SimilarityDiversityThreshold = TypedConfig.SimilarityDiversityThreshold;
        Effort = TypedConfig.Effort;
        MinimumPopulationSize = TypedConfig.MinimumPopulationSize;
        MaximumPopulationSize = TypedConfig.MaximumPopulationSize;
        Elites = TypedConfig.Elites;
        
        SelectedSelection1Config = CreateSelectionConfigFor(Selection1, true);
        SelectedSelection1Config?.LoadFrom(TypedConfig, true);
        SubscribeToSelectionConfig(SelectedSelection1Config, true);

        SelectedSelection2Config = CreateSelectionConfigFor(Selection2, false);
        SelectedSelection2Config?.LoadFrom(TypedConfig, false);
        SubscribeToSelectionConfig(SelectedSelection2Config, false);
    }
    
    partial void OnPopulationSizeChanged(int value) 
    {
        TypedConfig.PopulationSize = value;
        UpdateConfig();
    }
    
    partial void OnSelection1Changed(string? value) 
    {
        SelectedSelection1Config = CreateSelectionConfigFor(value, primary: true);
        SelectedSelection1Config?.LoadFrom(TypedConfig, true);
        SubscribeToSelectionConfig(SelectedSelection1Config, primary: true);
        
        var selectionType = Selectors.FirstOrDefault(t => t?.Name == value);
        if (selectionType is null) return;
        var selection = selectionType.CreateInstanceWithDefaults() as ISelection;
        if (selection is null) return;
        SelectedSelection1Config?.ApplyTo(TypedConfig, selection, true);
        
        UpdateConfig();
    }
    partial void OnSelection2Changed(string? value) 
    {
        SelectedSelection2Config = CreateSelectionConfigFor(value, primary: false);
        SelectedSelection2Config?.LoadFrom(TypedConfig, false);
        SubscribeToSelectionConfig(SelectedSelection2Config, primary: false);
        
        var selectionType = Selectors.FirstOrDefault(t => t?.Name == value);
        if (selectionType is null) return;
        var selection = selectionType.CreateInstanceWithDefaults() as ISelection;
        if (selection is null) return;
        SelectedSelection2Config?.ApplyTo(TypedConfig, selection, false);
        
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
        var mutatorType = Crossovers.FirstOrDefault(t => t?.Name == value);
        if (mutatorType is null) return;
        var mutation = mutatorType.CreateInstanceWithDefaults() as IMutation;
        if (mutation is null) return;
        TypedConfig.Mutation = mutation;
        UpdateConfig();
    }
    partial void OnMutationProbabilityChanged(double value) 
    {
        TypedConfig.MutationProbability = value;
        UpdateConfig();
    }
    partial void OnElitesChanged(int value)
    {
        TypedConfig.Elites = value;
        UpdateConfig();
    }
    
    partial void OnComparisonFactorChanged(double value)
    {
        TypedConfig.ComparisonFactor = value;
        UpdateConfig();
    }
    partial void OnSimilarityDiversityThresholdChanged(double value)
    {
        TypedConfig.SimilarityDiversityThreshold = value;
        UpdateConfig();
    }
    partial void OnEffortChanged(int value)
    {
        TypedConfig.Effort = value;
        UpdateConfig();
    }
    partial void OnMinimumPopulationSizeChanged(int value)
    {
        TypedConfig.MinimumPopulationSize = value;
        UpdateConfig();
    }
    partial void OnMaximumPopulationSizeChanged(int value)
    {
        TypedConfig.MaximumPopulationSize = value;
        UpdateConfig();
    }
    
    public override void SetConfig(BaseAlgorithmConfigViewModel? currentAlgorithmConfigViewModel)
    {
        if (currentAlgorithmConfigViewModel is RapGaAlgorithmConfigViewModel rapGaAlgorithmConfigViewModel)
        {
            JobActive = false;
            Config = (RAPGAConfig)rapGaAlgorithmConfigViewModel.Config.Clone();
            SetValuesFromConfig();
            UpdateConfig();
        }
    }
    
    protected override void DisposeManaged()
    {
        // Unsubscribe PropertyChanged handlers
        if (_selectionConfigHandler is not null && SelectedSelection1Config is not null)
            SelectedSelection1Config.PropertyChanged -= _selectionConfigHandler;

        if (_selection2ConfigHandler is not null && SelectedSelection2Config is not null)
            SelectedSelection2Config.PropertyChanged -= _selection2ConfigHandler;

        // Dispose nested config VMs if they implement IDisposable
        (SelectedSelection1Config as IDisposable)?.Dispose();
        (SelectedSelection2Config as IDisposable)?.Dispose();

        base.DisposeManaged();
    }
}
