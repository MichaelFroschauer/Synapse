using CommunityToolkit.Mvvm.ComponentModel;
using Synapse.GUI.Models;
using Synapse.GUI.ViewModels.Controls;

namespace Synapse.GUI.ViewModels.Pages;

public partial class PageViewModel : ViewModelBase
{
    [ObservableProperty]
    private ApplicationPageNames _pageName;
}
