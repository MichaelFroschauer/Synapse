using System;
using Synapse.JobManagement;

namespace Synapse.GUI.Services;

public class JobSelectorService : IJobSelectorService
{
    private readonly IJobManager _jobManager;
    private Guid? _selectedJobId = null;

    public Guid? SelectedJobId
    {
        get => _selectedJobId;
        set
        {
            var old = _selectedJobId;
            _selectedJobId = value;
            
            var handler = SelectedJobChanged;
            handler?.Invoke(this, new SelectedJobChangedEventArgs(old, _selectedJobId, _jobManager.GetJobInfo(_selectedJobId)));
        }
    }

    public JobInfo? SelectedJobInfo => _jobManager.GetJobInfo(_selectedJobId);
    
    public event EventHandler<SelectedJobChangedEventArgs>? SelectedJobChanged;

    public JobSelectorService(IJobManager jobManager)
    {
        _jobManager = jobManager;
    }
}

public class SelectedJobChangedEventArgs : EventArgs
{
    public Guid? OldSelectedJobId { get; }
    public Guid? NewSelectedJobId { get; }
    
    public JobInfo? SelectedJobInfo { get; }

    public SelectedJobChangedEventArgs(Guid? oldId, Guid? newId, JobInfo? selectedJobInfo)
    {
        OldSelectedJobId = oldId;
        NewSelectedJobId = newId;
        SelectedJobInfo = selectedJobInfo;
    }
}
