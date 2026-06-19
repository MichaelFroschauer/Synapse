using Synapse.GUI.Services;
using Synapse.JobManagement;

namespace Synapse.GUI.ViewModels.Controls;

public partial class HitlParametersViewModel : ViewModelBase
{
    private readonly IJobManager _jobManager;
    private readonly IJobSelectorService _jobSelector;

    public HitlParametersViewModel(IJobManager jobManager, IJobSelectorService jobSelector)
    {
        _jobManager = jobManager;
        _jobSelector = jobSelector;
    }
}
