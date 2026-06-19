using System;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Synapse.GUI.Services;
using Synapse.JobManagement;
using Synapse.OptimizationCore.HITL;

namespace Synapse.GUI.ViewModels.Controls;

public partial class InteractionTriggersViewModel : ViewModelBase
{
    private readonly IJobSelectorService _jobSelector;
    private readonly IJobManager _jobManager;

    // ── Trigger enable flags ──
    [ObservableProperty] private bool _periodicEnabled;
    [ObservableProperty] private bool _stagnationEnabled;
    [ObservableProperty] private bool _diversityLossEnabled;
    [ObservableProperty] private bool _qualityThresholdEnabled;

    // ── Periodic trigger params ──
    [ObservableProperty] private int _periodicInterval = 50;

    // ── Stagnation trigger params ──
    [ObservableProperty] private int _stagnationWindow = 50;
    [ObservableProperty] private double _stagnationMinImprovement = 0.01;
    [ObservableProperty] private int _stagnationCooldown = 20;

    // ── Diversity loss trigger params ──
    [ObservableProperty] private double _diversityThreshold = 0.2;
    [ObservableProperty] private int _diversityConsecutiveIterations = 5;
    [ObservableProperty] private int _diversityCooldown = 30;

    // ── Quality threshold trigger params ──
    [ObservableProperty] private double _qualityTargetFitness = 100.0;
    [ObservableProperty] private bool _qualityMinimize = true;
    [ObservableProperty] private bool _qualityFireOnce = true;

    // ── Status display ──
    [ObservableProperty] private string? _lastTriggerReason;
    [ObservableProperty] private bool _hasTriggered;

    public InteractionTriggersViewModel(IJobSelectorService jobSelector, IJobManager jobManager)
    {
        _jobSelector = jobSelector;
        _jobManager = jobManager;

        _jobSelector.SelectedJobChanged += OnSelectedJobChanged;
        OnSelectedJobChanged(null, null);
    }

    private IHitlController? _previousHitlController;

    private void OnSelectedJobChanged(object? sender, SelectedJobChangedEventArgs? e)
    {
        HasTriggered = false;
        LastTriggerReason = null;

        // Unsubscribe from previous controller
        if (_previousHitlController is not null)
            _previousHitlController.InteractionTriggered -= OnInteractionTriggered;

        var hitlCtrl = GetCurrentHitlController();
        _previousHitlController = hitlCtrl;
        if (hitlCtrl is null) return;

        // Read existing trigger configuration from the controller
        LoadFromController(hitlCtrl);
        
        // Subscribe to trigger events
        hitlCtrl.InteractionTriggered += OnInteractionTriggered;
    }

    private void OnInteractionTriggered(string reason)
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            HasTriggered = true;
            LastTriggerReason = reason;
        });
    }

    // Rebuild the CompositeTrigger whenever any setting changes
    partial void OnPeriodicEnabledChanged(bool value) => RebuildTrigger();
    partial void OnStagnationEnabledChanged(bool value) => RebuildTrigger();
    partial void OnDiversityLossEnabledChanged(bool value) => RebuildTrigger();
    partial void OnQualityThresholdEnabledChanged(bool value) => RebuildTrigger();

    partial void OnPeriodicIntervalChanged(int value) => RebuildTrigger();
    partial void OnStagnationWindowChanged(int value) => RebuildTrigger();
    partial void OnStagnationMinImprovementChanged(double value) => RebuildTrigger();
    partial void OnStagnationCooldownChanged(int value) => RebuildTrigger();
    partial void OnDiversityThresholdChanged(double value) => RebuildTrigger();
    partial void OnDiversityConsecutiveIterationsChanged(int value) => RebuildTrigger();
    partial void OnDiversityCooldownChanged(int value) => RebuildTrigger();
    partial void OnQualityTargetFitnessChanged(double value) => RebuildTrigger();
    partial void OnQualityMinimizeChanged(bool value) => RebuildTrigger();
    partial void OnQualityFireOnceChanged(bool value) => RebuildTrigger();

    private void RebuildTrigger()
    {
        var hitlCtrl = GetCurrentHitlController();
        if (hitlCtrl is null) return;

        var composite = new CompositeTrigger();

        if (PeriodicEnabled)
            composite.Add(new PeriodicTrigger(Math.Max(1, PeriodicInterval)));

        if (StagnationEnabled)
            composite.Add(new StagnationTrigger(
                Math.Max(2, StagnationWindow),
                Math.Max(0, StagnationMinImprovement / 100.0), // GUI shows percentage, convert to fraction
                Math.Max(0, StagnationCooldown)));

        if (DiversityLossEnabled)
            composite.Add(new DiversityLossTrigger(
                Math.Clamp(DiversityThreshold, 0.0, 1.0),
                Math.Max(1, DiversityConsecutiveIterations),
                Math.Max(0, DiversityCooldown)));

        if (QualityThresholdEnabled)
            composite.Add(new QualityThresholdTrigger(
                QualityTargetFitness,
                QualityMinimize,
                QualityFireOnce));

        hitlCtrl.InteractionTrigger = composite.Triggers.Count > 0 ? composite : null;
    }

    private void LoadFromController(IHitlController hitlCtrl)
    {
        var trigger = hitlCtrl.InteractionTrigger;
        if (trigger is null)
        {
            PeriodicEnabled = false;
            StagnationEnabled = false;
            DiversityLossEnabled = false;
            QualityThresholdEnabled = false;
            return;
        }

        // If it's a composite, read child triggers
        if (trigger is CompositeTrigger composite)
        {
            foreach (var child in composite.Triggers)
                LoadSingleTrigger(child);
        }
        else
        {
            LoadSingleTrigger(trigger);
        }
    }

    private void LoadSingleTrigger(IInteractionTrigger trigger)
    {
        switch (trigger)
        {
            case PeriodicTrigger pt:
                PeriodicEnabled = true;
                PeriodicInterval = pt.Interval;
                break;
            case StagnationTrigger st:
                StagnationEnabled = true;
                StagnationWindow = st.StagnationWindow;
                StagnationMinImprovement = st.MinImprovementFraction * 100.0; // fraction -> percentage
                StagnationCooldown = st.CooldownIterations;
                break;
            case DiversityLossTrigger dt:
                DiversityLossEnabled = true;
                DiversityThreshold = dt.DiversityThreshold;
                DiversityConsecutiveIterations = dt.ConsecutiveIterations;
                DiversityCooldown = dt.CooldownIterations;
                break;
            case QualityThresholdTrigger qt:
                QualityThresholdEnabled = true;
                QualityTargetFitness = qt.TargetFitness;
                QualityMinimize = qt.Minimize;
                QualityFireOnce = qt.FireOnce;
                break;
        }
    }

    private IHitlController? GetCurrentHitlController()
    {
        if (_jobSelector.SelectedJobId is null) return null;
        var jobInfo = _jobManager.GetJobInfo(_jobSelector.SelectedJobId);
        return jobInfo?.Config.HitlController;
    }

    protected override void DisposeManaged()
    {
        if (_previousHitlController is not null)
            _previousHitlController.InteractionTriggered -= OnInteractionTriggered;
        _jobSelector.SelectedJobChanged -= OnSelectedJobChanged;
        base.DisposeManaged();
    }
}



