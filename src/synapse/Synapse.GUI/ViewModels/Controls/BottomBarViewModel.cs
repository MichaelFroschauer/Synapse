using System;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Synapse.GUI.Services;
using Synapse.JobManagement;
using Synapse.OptimizationCore.Common;

namespace Synapse.GUI.ViewModels.Controls;

public partial class BottomBarViewModel : ViewModelBase
{
    private readonly IJobManager _jobManager;
    private readonly IJobSelectorService _jobSelector;
    
    [ObservableProperty] private string? _lastAlgorithmMessage;
    [ObservableProperty] private string _selectedJobStatus = "—";
    [ObservableProperty] private string _bestFitnessText = "—";
    [ObservableProperty] private string _iterationText = "—";

    public BottomBarViewModel(IJobManager jobManager, IJobSelectorService jobSelector)
    {
        _jobManager = jobManager;
        _jobSelector = jobSelector;
        _jobSelector.SelectedJobChanged += OnSelectedJobChanged;
        _jobManager.JobProgress += OnJobProgressChanged;
        _jobManager.JobStatusChanged += OnJobStatusChanged;
        RefreshFromJobInfo();
    }

    private void RefreshFromJobInfo()
    {
        var info = _jobSelector.SelectedJobInfo;
        LastAlgorithmMessage = info?.LastMessage;
        SelectedJobStatus = info?.Status.ToString() ?? "—";
        BestFitnessText = info?.BestFitness.HasValue == true ? $"{Math.Abs(info.BestFitness.Value):G6}" : "—";
        IterationText = info is not null ? info.IterationCount.ToString() : "—";
    }

    private void OnSelectedJobChanged(object? sender, SelectedJobChangedEventArgs e)
    {
        Dispatcher.UIThread.Post(RefreshFromJobInfo);
    }

    private void OnJobProgressChanged(object? sender, (Guid JobId, ProgressEventArgs Progress) e)
    {
        if (e.JobId != _jobSelector.SelectedJobId) return;
        Dispatcher.UIThread.Post(RefreshFromJobInfo);
    }

    private void OnJobStatusChanged(object? sender, (Guid JobId, JobStatus Status) e)
    {
        if (e.JobId != _jobSelector.SelectedJobId) return;
        Dispatcher.UIThread.Post(RefreshFromJobInfo);
    }
    
    protected override void DisposeManaged()
    {
        _jobSelector.SelectedJobChanged -= OnSelectedJobChanged;
        _jobManager.JobProgress -= OnJobProgressChanged;
        _jobManager.JobStatusChanged -= OnJobStatusChanged;
        base.DisposeManaged();
    }
}
