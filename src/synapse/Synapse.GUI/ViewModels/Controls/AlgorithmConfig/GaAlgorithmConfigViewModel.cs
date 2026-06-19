using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using ActiproSoftware.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using Synapse.Algorithms.GeneticAlgorithm;
using Synapse.Algorithms.GeneticAlgorithm.Crossover;
using Synapse.Algorithms.GeneticAlgorithm.Mutator;
using Synapse.Algorithms.GeneticAlgorithm.Selector;
using Synapse.GUI.Services;
using Synapse.GUI.ViewModels.Controls.AlgorithmConfig.GA;
using Synapse.JobManagement;
using Synapse.OptimizationCore.Common;
using Synapse.OptimizationCore.Interfaces;
using Synapse.Reflection;

namespace Synapse.GUI.ViewModels.Controls.AlgorithmConfig;

public partial class GaAlgorithmConfigViewModel : BaseAlgorithmConfigViewModel
{
    protected sealed override IAlgorithmConfig Config { get; set; }
    
    private GeneticAlgorithmConfig TypedConfig {
        get => (GeneticAlgorithmConfig)Config;
        set => Config = value;
    }

    private List<Type> Selectors { get; set; } = new();
    public ObservableCollection<string> AvailableSelections { get; } = new();
    private List<Type> Crossovers { get; set; } = new();
    public ObservableCollection<string> AvailableCrossovers { get; } = new();
    private List<Type> Mutators { get; set; } = new();
    public ObservableCollection<string> AvailableMutations { get; } = new();
    
    [ObservableProperty] private GaConfigViewModel? _selectedSelectionConfig;
    [ObservableProperty] private GaConfigViewModel? _selectedSelection2Config;

    [ObservableProperty] private int _populationSize;
    [ObservableProperty] private string? _selection;
    [ObservableProperty] private string? _selection2;
    [ObservableProperty] private string? _crossover;
    [ObservableProperty] private string? _mutation;
    [ObservableProperty] private double _crossoverProbability;
    [ObservableProperty] private double _mutationProbability;
    [ObservableProperty] private int _eliteCount;

    private PropertyChangedEventHandler? _selectionConfigHandler;
    private PropertyChangedEventHandler? _selection2ConfigHandler;
    
    public GaAlgorithmConfigViewModel(
        INavigationService navigationService, IJobManager jobManager,
        IJobSelectorService jobSelector, ITypeRegistry typeRegistry)
        : base(navigationService, jobManager, jobSelector, typeRegistry)
    {
        AlgorithmType = AlgorithmType.Genetic;
        SetAvailableConfig();
        
        JobActive = false;
        var jobInfo = _jobManager.GetJobInfo(_jobSelector.SelectedJobId);
        if (jobInfo is not null && jobInfo.Config.AlgorithmType == AlgorithmType.Genetic)
        {
            Config = (jobInfo.Config as GeneticAlgorithmConfig)!;
            JobActive = true;
        }
        else
        {
            Config = new GeneticAlgorithmConfig();
        }
        
        SetValuesFromConfig();
        UpdateConfig();
    }
    
    private GaConfigViewModel? CreateSelectionConfigFor(string? name, bool primary)
    {
        return name switch
        {
            nameof(TournamentSelection) => new TournamentSelectionConfigViewModel(),
            _ => new GenericSelectionConfigViewModel()
        };
    }
    
    private void SubscribeToSelectionConfig(GaConfigViewModel? vm, bool primary)
    {
        if (primary)
        {
            // unsubscribe from old
            if (_selectionConfigHandler is not null && SelectedSelectionConfig is not null)
                SelectedSelectionConfig.PropertyChanged -= _selectionConfigHandler;

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
        Selection = TypedConfig.Selection?.GetType().Name;
        Selection2 = TypedConfig.Selection2?.GetType().Name;
        Crossover = TypedConfig.Crossover?.GetType().Name;
        Mutation = TypedConfig.Mutation?.GetType().Name;
        CrossoverProbability = TypedConfig.CrossoverProbability;
        MutationProbability = TypedConfig.MutationProbability;
        EliteCount = 0;
        
        SelectedSelectionConfig = CreateSelectionConfigFor(Selection, true);
        SelectedSelectionConfig?.LoadFrom(TypedConfig, true);
        SubscribeToSelectionConfig(SelectedSelectionConfig, true);

        SelectedSelection2Config = CreateSelectionConfigFor(Selection2, false);
        SelectedSelection2Config?.LoadFrom(TypedConfig, false);
        SubscribeToSelectionConfig(SelectedSelection2Config, false);
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
    
    partial void OnPopulationSizeChanged(int value) 
    {
        TypedConfig.PopulationSize = value;
        UpdateConfig();
    }
    
    partial void OnSelectionChanged(string? value) 
    {
        SelectedSelectionConfig = CreateSelectionConfigFor(value, primary: true);
        SelectedSelectionConfig?.LoadFrom(TypedConfig, true);
        SubscribeToSelectionConfig(SelectedSelectionConfig, primary: true);
        
        var selectionType = Selectors.FirstOrDefault(t => t?.Name == value);
        if (selectionType is null) return;
        var selection = selectionType.CreateInstanceWithDefaults() as ISelection;
        if (selection is null) return;
        SelectedSelectionConfig?.ApplyTo(TypedConfig, selection, true);
        
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
        var mutatorType = Mutators.FirstOrDefault(t => t?.Name == value);
        if (mutatorType is null) return;
        var mutation = mutatorType.CreateInstanceWithDefaults() as IMutation;
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
    
    partial void OnEliteCountChanged(int value)
    {
        //Config.EliteCount = value;
        UpdateConfig();
    }
    
    public override void SetConfig(BaseAlgorithmConfigViewModel? currentAlgorithmConfigViewModel)
    {
        if (currentAlgorithmConfigViewModel is GaAlgorithmConfigViewModel gaAlgorithmConfigViewModel)
        {
            JobActive = false;
            Config = (GeneticAlgorithmConfig)gaAlgorithmConfigViewModel.Config.Clone();
            SetValuesFromConfig();
            UpdateConfig();
        }
    }
    
    protected override void DisposeManaged()
    {
        // Unsubscribe PropertyChanged handlers
        if (_selectionConfigHandler is not null && SelectedSelectionConfig is not null)
            SelectedSelectionConfig.PropertyChanged -= _selectionConfigHandler;

        if (_selection2ConfigHandler is not null && SelectedSelection2Config is not null)
            SelectedSelection2Config.PropertyChanged -= _selection2ConfigHandler;

        // Dispose nested config VMs if they implement IDisposable
        (SelectedSelectionConfig as IDisposable)?.Dispose();
        (SelectedSelection2Config as IDisposable)?.Dispose();

        base.DisposeManaged();
    }
}
