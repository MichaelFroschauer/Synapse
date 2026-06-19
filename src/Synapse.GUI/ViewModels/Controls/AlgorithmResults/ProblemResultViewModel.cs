using System;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Synapse.GUI.Services;
using Synapse.JobManagement;
using Synapse.OptimizationCore.Common;
using Synapse.OptimizationCore.Interfaces;

namespace Synapse.GUI.ViewModels.Controls.AlgorithmResults;

public abstract partial class ProblemResultViewModel : ViewModelBase
{
    [ObservableProperty] private ProblemType _problemType;
    [ObservableProperty] private double? _fitness;
    [ObservableProperty] private DateTimeOffset _createdAt;
    [ObservableProperty] private DateTimeOffset? _startedAt;
    [ObservableProperty] private DateTimeOffset? _finishedAt;
    [ObservableProperty] private int _iteration;
    
    protected readonly IJobManager JobManager;
    protected readonly IJobSelectorService JobSelector;
    protected Guid LastSelectedJobId;
    
    public bool ShowCurrentNonOptimalResults { get; set; } = false;

    private EventHandler<(Guid JobId, ProgressEventArgs Progress)> _jobProgressHandler;
    private EventHandler<SelectedJobChangedEventArgs> _selectedJobChangedHandler;

    protected ProblemResultViewModel(IJobManager jobManager, IJobSelectorService jobSelector)
    {
        JobManager = jobManager;
        JobSelector = jobSelector;
        
        _jobProgressHandler = (s, jobProgress) => UpdateJobProgress(jobProgress);
        _selectedJobChangedHandler = (s, e) => UpdateJobProgress();
        
        JobManager.JobProgress += _jobProgressHandler;
        JobSelector.SelectedJobChanged += _selectedJobChangedHandler; 
        
        UpdateJobProgress();
    }

    private void UpdateJobProgress((Guid JobId, ProgressEventArgs Progress) jobProgress)
    {
        if (jobProgress.JobId != JobSelector.SelectedJobId) return;
        UpdateJobProgress();
    }
    
    private void UpdateJobProgress()
    {
        if (StaticSolution is not null) return;
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (StaticSolution is not null) return;
            JobProgressChanged();
        });
    }

    protected abstract void JobProgressChanged();
    protected abstract void SetStaticSolution();
    
    protected ISolution? StaticSolution { get; set; } = null;
    public void SetStaticSolution(ISolution solution)
    {
        StaticSolution = solution;
        SetStaticSolution();
    }

    protected override void DisposeManaged()
    {
        JobManager.JobProgress -= _jobProgressHandler;
        JobSelector.SelectedJobChanged -= _selectedJobChangedHandler;
        base.DisposeManaged();
    }
}
