using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using Synapse.Algorithms.ParticleSwarm;
using Synapse.Algorithms.ParticleSwarm.Mapper;
using Synapse.GUI.Services;
using Synapse.JobManagement;
using Synapse.OptimizationCore.Common;
using Synapse.OptimizationCore.Interfaces;
using Synapse.Reflection;

namespace Synapse.GUI.ViewModels.Controls.AlgorithmConfig;

public partial class PsoAlgorithmConfigViewModel : BaseAlgorithmConfigViewModel
{
    public sealed class PositionBoundRow : ObservableObject
    {
        public int DimensionIndex { get; }
        public string Label => $"Dim {DimensionIndex + 1}";

        private double _min;
        public double Min
        {
            get => _min;
            set => SetProperty(ref _min, value);
        }

        private double _max;
        public double Max
        {
            get => _max;
            set => SetProperty(ref _max, value);
        }

        public PositionBoundRow(int dimensionIndex, double min, double max)
        {
            DimensionIndex = dimensionIndex;
            _min = min;
            _max = max;
        }
    }

    protected sealed override IAlgorithmConfig Config { get; set; }
    
    private ParticleSwarmConfig TypedConfig {
        get => (ParticleSwarmConfig)Config;
        set => Config = value;
    }

    private const int ParameterPrecision = 2;
    
    [ObservableProperty]
    private int _swarmSize;
    
    [ObservableProperty]
    private double _inertia;
    
    [ObservableProperty]
    private double _cognitive;

    [ObservableProperty]
    private double _social;
    
    [ObservableProperty] 
    private double _velocityClampFactor;

    [ObservableProperty]
    private double[]? _positionMin;

    [ObservableProperty]
    private double[]? _positionMax;

    [ObservableProperty]
    private ObservableCollection<PositionBoundRow> _positionBounds = new();

    [ObservableProperty]
    private string _boundsHint = "Select a problem instance to configure per-dimension bounds.";

    private IProblem? _selectedProblem;
    private bool _isSyncingBounds;

    public PsoAlgorithmConfigViewModel(INavigationService navigationService, IJobManager jobManager,
        IJobSelectorService jobSelector, ITypeRegistry typeRegistry)
        : base(navigationService, jobManager, jobSelector, typeRegistry)
    {
        AlgorithmType = AlgorithmType.ParticleSwarm;

        JobActive = false;
        var jobInfo = _jobSelector.SelectedJobInfo;
        if (jobInfo is not null && jobInfo.Config.AlgorithmType == AlgorithmType.ParticleSwarm)
        {
            Config = (jobInfo.Config as ParticleSwarmConfig)!;
            JobActive = true;
        }
        else
        {
            Config = new ParticleSwarmConfig();
        }

        SetValuesFromConfig();
        UpdateConfig();
    }

    protected override void OnProblemContextChanged(IProblem? problem)
    {
        _selectedProblem = problem;
        SetAvailableConfig();
    }

    protected override void SetValuesFromConfig()
    {
        base.SetValuesFromConfig();
        SwarmSize = TypedConfig.SwarmSize;
        Inertia = RoundToPrecision(TypedConfig.Inertia);
        Cognitive = RoundToPrecision(TypedConfig.Cognitive);
        Social = RoundToPrecision(TypedConfig.Social);
        VelocityClampFactor = RoundToPrecision(TypedConfig.VelocityClampFactor);
        PositionMin = TypedConfig.PositionMin;
        PositionMax = TypedConfig.PositionMax;
        SetAvailableConfig();
    }

    partial void OnSwarmSizeChanged(int value)
    {
        TypedConfig.SwarmSize = value;
        UpdateConfig();
    }
    
    partial void OnInertiaChanged(double value) 
    {
        var rounded = RoundToPrecision(value);
        if (!AreClose(value, rounded))
        {
            Inertia = rounded;
            return;
        }

        TypedConfig.Inertia = rounded;
        UpdateConfig();
    }
    
    partial void OnCognitiveChanged(double value) 
    {
        var rounded = RoundToPrecision(value);
        if (!AreClose(value, rounded))
        {
            Cognitive = rounded;
            return;
        }

        TypedConfig.Cognitive = rounded;
        UpdateConfig();
    }
    
    partial void OnSocialChanged(double value) 
    {
        var rounded = RoundToPrecision(value);
        if (!AreClose(value, rounded))
        {
            Social = rounded;
            return;
        }

        TypedConfig.Social = rounded;
        UpdateConfig();
    }
    
    partial void OnVelocityClampFactorChanged(double value) 
    {
        var rounded = RoundToPrecision(value);
        if (!AreClose(value, rounded))
        {
            VelocityClampFactor = rounded;
            return;
        }

        TypedConfig.VelocityClampFactor = rounded;
        UpdateConfig();
    }
    
    partial void OnPositionMinChanged(double[]? value) 
    {
        TypedConfig.PositionMin = value;
        if (!_isSyncingBounds)
            SetAvailableConfig();
        UpdateConfig();
    }
    
    partial void OnPositionMaxChanged(double[]? value) 
    {
        TypedConfig.PositionMax = value;
        if (!_isSyncingBounds)
            SetAvailableConfig();
        UpdateConfig();
    }

    private static double RoundToPrecision(double value) =>
        Math.Round(value, ParameterPrecision, MidpointRounding.AwayFromZero);

    private static bool AreClose(double left, double right) =>
        Math.Abs(left - right) < 1e-9;

    private void SetAvailableConfig()
    {
        if (Config is not ParticleSwarmConfig)
        {
            PositionBounds.Clear();
            BoundsHint = "No dimensions available yet. Select a problem instance first.";
            return;
        }

        foreach (var row in PositionBounds)
            row.PropertyChanged -= OnPositionBoundRowChanged;

        PositionBounds.Clear();

        var mapper = TryResolveMapper();
        var dimensions = GetDimensions(mapper);
        if (dimensions <= 0)
        {
            BoundsHint = "No dimensions available yet. Select a problem instance first.";
            return;
        }

        var mapperBounds = mapper?.Bounds;
        var mapperMin = mapperBounds.HasValue ? mapperBounds.Value.Min : null;
        var mapperMax = mapperBounds.HasValue ? mapperBounds.Value.Max : null;

        var mins = BuildBoundArray(TypedConfig.PositionMin, mapperMin, dimensions, fallback: -10000.0);
        var maxs = BuildBoundArray(TypedConfig.PositionMax, mapperMax, dimensions, fallback: 10000.0);

        for (var i = 0; i < dimensions; i++)
        {
            if (mins[i] > maxs[i])
                (mins[i], maxs[i]) = (maxs[i], mins[i]);

            var row = new PositionBoundRow(i, RoundToPrecision(mins[i]), RoundToPrecision(maxs[i]));
            row.PropertyChanged += OnPositionBoundRowChanged;
            PositionBounds.Add(row);
        }

        SyncBoundsFromRows(updateConfig: false);
        BoundsHint = "Restrict each dimension by setting Min/Max bounds.";
    }

    private IParticleMapper? TryResolveMapper()
    {
        var problem = _jobSelector.SelectedJobInfo?.Problem ?? _selectedProblem;
        if (problem is null) return null;

        try
        {
            var solution = problem.CreateRandomSolution();
            return solution.GetMapper();
        }
        catch
        {
            return null;
        }
    }

    private static int GetDimensions(IParticleMapper? mapper)
    {
        if (mapper is not null && mapper.Dimensions > 0)
            return mapper.Dimensions;

        return 0;
    }

    private static double[] BuildBoundArray(double[]? configBounds, double[]? mapperBounds, int dimensions, double fallback)
    {
        var result = new double[dimensions];
        for (var i = 0; i < dimensions; i++)
        {
            if (mapperBounds is not null && i < mapperBounds.Length)
            {
                result[i] = mapperBounds[i];
                continue;
            }
            
            if (configBounds is not null && i < configBounds.Length)
            {
                result[i] = configBounds[i];
                continue;
            }

            result[i] = fallback;
        }

        return result;
    }

    private void OnPositionBoundRowChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not PositionBoundRow row) return;
        if (e.PropertyName is not nameof(PositionBoundRow.Min) and not nameof(PositionBoundRow.Max)) return;

        row.Min = RoundToPrecision(row.Min);
        row.Max = RoundToPrecision(row.Max);
        if (row.Min > row.Max)
        {
            if (e.PropertyName == nameof(PositionBoundRow.Min))
                row.Max = row.Min;
            else
                row.Min = row.Max;
        }

        UpdateBoundsForRow(row, updateConfig: true);
    }

    private void UpdateBoundsForRow(PositionBoundRow row, bool updateConfig)
    {
        var dimensions = PositionBounds.Count;
        if (dimensions == 0) return;

        var min = PositionMin;
        var max = PositionMax;
        if (min is null || min.Length != dimensions)
            min = PositionBounds.Select(b => RoundToPrecision(b.Min)).ToArray();
        if (max is null || max.Length != dimensions)
            max = PositionBounds.Select(b => RoundToPrecision(b.Max)).ToArray();

        min[row.DimensionIndex] = RoundToPrecision(row.Min);
        max[row.DimensionIndex] = RoundToPrecision(row.Max);

        _isSyncingBounds = true;
        try
        {
            PositionMin = min;
            PositionMax = max;
            TypedConfig.PositionMin = min;
            TypedConfig.PositionMax = max;
        }
        finally
        {
            _isSyncingBounds = false;
        }

        if (updateConfig)
            UpdateConfig();
    }

    private void SyncBoundsFromRows(bool updateConfig)
    {
        if (PositionBounds.Count == 0)
        {
            _isSyncingBounds = true;
            try
            {
                PositionMin = null;
                PositionMax = null;
                TypedConfig.PositionMin = null;
                TypedConfig.PositionMax = null;
            }
            finally
            {
                _isSyncingBounds = false;
            }

            if (updateConfig)
                UpdateConfig();
            return;
        }

        var min = PositionBounds.Select(b => RoundToPrecision(b.Min)).ToArray();
        var max = PositionBounds.Select(b => RoundToPrecision(b.Max)).ToArray();

        _isSyncingBounds = true;
        try
        {
            PositionMin = min;
            PositionMax = max;
            TypedConfig.PositionMin = min;
            TypedConfig.PositionMax = max;
        }
        finally
        {
            _isSyncingBounds = false;
        }

        if (updateConfig)
            UpdateConfig();
    }

    public override void SetConfig(BaseAlgorithmConfigViewModel? currentAlgorithmConfigViewModel)
    {
        if (currentAlgorithmConfigViewModel is PsoAlgorithmConfigViewModel psoAlgorithmConfigViewModel)
        {
            JobActive = false;
            Config = (ParticleSwarmConfig)psoAlgorithmConfigViewModel.Config.Clone();
            SetValuesFromConfig();
            UpdateConfig();
        }
    }

    protected override void DisposeManaged()
    {
        foreach (var row in PositionBounds)
            row.PropertyChanged -= OnPositionBoundRowChanged;

        base.DisposeManaged();
    }
}
