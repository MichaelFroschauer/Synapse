using System.Linq;
using CommunityToolkit.Mvvm.Input;
using Synapse.GUI.Services;
using Synapse.JobManagement;

namespace Synapse.GUI.ViewModels.Controls;

public partial class SidebarRightViewModel : ViewModelBase
{
    private readonly IJobSelectorService _jobSelectorService;
    private readonly IJobManager _jobManager;
    public RunningJobsViewModel RunningJobsVm { get; }
    
    public SidebarRightViewModel(
        RunningJobsViewModel runningJobsVm,
        IJobSelectorService jobSelectorService,
        IJobManager jobManager)
    {
        RunningJobsVm = runningJobsVm;

        _jobSelectorService = jobSelectorService;
        _jobManager = jobManager;
    }
    
    [RelayCommand]
    void StartPauseAlgorithmSelection()
    {
        if (_jobSelectorService.SelectedJobId is null) return;

        var jobInfo = _jobManager.GetJobInfo(_jobSelectorService.SelectedJobId.Value);
        if (jobInfo is null) return;
        if (jobInfo.Status == JobStatus.Paused)
        {
            _jobManager.ResumeJobAsync(jobInfo.Id);
        }
        else if (jobInfo.Status == JobStatus.Running)
        {
            _jobManager.PauseJobAsync(jobInfo.Id);
        }
        else if (jobInfo.Status == JobStatus.Queued)
        {
            _ = _jobManager.StartQueuedJobAsync(jobInfo.Id);
        }
    }

    [RelayCommand]
    void StopAlgorithmSelection()
    {
        if (_jobSelectorService.SelectedJobId == null) return;
        var jobInfo = _jobManager.GetJobInfo(_jobSelectorService.SelectedJobId.Value);
        if (jobInfo is null) return;
        _jobManager.StopJobAsync(jobInfo.Id);
    }

    [RelayCommand]
    void StartAllAlgorithms()
    {
        var jobList = _jobManager.ListJobs();
        jobList.ToList().ForEach(j =>
        {
            if (j.Status == JobStatus.Queued)
            {
                _ = _jobManager.StartQueuedJobAsync(j.Id);
            }
            else
            {
                _jobManager.ResumeJobAsync(j.Id);
            }
        });
    }

    [RelayCommand]
    void PauseAllAlgorithms()
    {
        var jobList = _jobManager.ListJobs();
        jobList.ToList().ForEach(j => _jobManager.PauseJobAsync(j.Id));
    }

    [RelayCommand]
    void StopAllAlgorithms()
    {
        var jobList = _jobManager.ListJobs();
        jobList.ToList().ForEach(j => _jobManager.StopJobAsync(j.Id));
    }
}