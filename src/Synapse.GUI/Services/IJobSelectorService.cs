using System;
using Synapse.JobManagement;

namespace Synapse.GUI.Services;

public interface IJobSelectorService
{
    Guid? SelectedJobId { get; set; }
    JobInfo? SelectedJobInfo { get; }
    event EventHandler<SelectedJobChangedEventArgs>? SelectedJobChanged;
}
