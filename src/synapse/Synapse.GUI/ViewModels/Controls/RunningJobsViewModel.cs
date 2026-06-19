using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Synapse.GUI.Models.Messages;
using Synapse.GUI.Services;
using Synapse.JobManagement;
using Synapse.OptimizationCore.Common;

namespace Synapse.GUI.ViewModels.Controls;

public partial class RunningJobsViewModel : ViewModelBase
{
    private readonly IJobSelectorService _jobSelectorService;
    private readonly IJobManager _jobManagerService;
    public ObservableCollection<AlgorithmExecutionStateCardViewModel> Jobs { get; } = new();
    
    [ObservableProperty] private string _activeBadge;

    [ObservableProperty] private AlgorithmExecutionStateCardViewModel? _selectedJob;
    
    private EventHandler<(Guid JobId, ProgressEventArgs Progress)>? _jobProgressHandler;
    private EventHandler<(Guid JobId, JobStatus Status)>? _jobStatusHandler;
    private EventHandler<SelectedJobChangedEventArgs>? _selectedJobChangedHandler;

    public RunningJobsViewModel(IJobManager jobManager, IJobSelectorService jobSelectorService)
    {
        _jobSelectorService = jobSelectorService;
        _jobManagerService = jobManager;
        
        
        _jobProgressHandler = async void (sender, jobProgress) =>
        {
            try
            {
                await JobProgressChanged(jobProgress.JobId, jobProgress.Progress);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RunningJobsVM] Progress handler error: {ex.Message}");
            }
        };

        _jobStatusHandler = async void (sender, jobStatus) =>
        {
            try
            {
                await JobStatusChanged(jobStatus.JobId, jobStatus.Status);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RunningJobsVM] Status handler error: {ex.Message}");
            }
        };

        _selectedJobChangedHandler = (s, e) =>
        {
            SelectedJob = _jobSelectorService.SelectedJobId is null
                ? null
                : Jobs.FirstOrDefault(j => j.Id == _jobSelectorService.SelectedJobId);
        };
        
        _jobManagerService.JobProgress += _jobProgressHandler;
        _jobManagerService.JobStatusChanged += _jobStatusHandler;
        _jobSelectorService.SelectedJobChanged += _selectedJobChangedHandler;
        
        WeakReferenceMessenger.Default.Register<AlgorithmStartedMessage>(this, void (r, msg) =>
        {
            JobInfo? jobInfo = jobManager.GetJobInfo(msg.JobId);
            SetOrUpdateJob(msg.JobId, jobInfo);
        });
        
        ActiveBadge = "0 Active";
    }

    private async Task JobStatusChanged(Guid guid, JobStatus status) => await JobChanged(guid);
    private async Task JobProgressChanged(Guid guid, ProgressEventArgs progress) => await JobChanged(guid);
    private async Task JobChanged(Guid guid)
    {
        var info = _jobManagerService.GetJobInfo(guid);
        await Dispatcher.UIThread.InvokeAsync(() => SetOrUpdateJob(guid, info));
    }

    private void SetOrUpdateJob(Guid guid, JobInfo? jobInfo)
    {
        if (jobInfo is null)
        {
            var jobToRemove = Jobs.FirstOrDefault(j => j.Id == guid);
            if (jobToRemove != null) Jobs.Remove(jobToRemove);
        }
        else
        {
            var existingJob = Jobs.SingleOrDefault(j => j.Id == guid);
            if (existingJob != null)
            {
                existingJob.UpdateJob(jobInfo);
            }
            else
            {
                Jobs.Insert(0, new AlgorithmExecutionStateCardViewModel(jobInfo, _jobManagerService));
            }
        }
        
        ActiveBadge = $"{Jobs.Count} Active";
    }

    partial void OnSelectedJobChanged(AlgorithmExecutionStateCardViewModel? value)
    {
        _jobSelectorService.SelectedJobId = value?.Id;
    }

    protected override void DisposeManaged()
    {
        _jobManagerService.JobProgress -= _jobProgressHandler;
        _jobManagerService.JobStatusChanged -= _jobStatusHandler;
        _jobSelectorService.SelectedJobChanged -= _selectedJobChangedHandler;
        WeakReferenceMessenger.Default.UnregisterAll(this);
        base.DisposeManaged();
    }
}
