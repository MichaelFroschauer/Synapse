using CommunityToolkit.Mvvm.ComponentModel;
using Synapse.GUI.Services;
using Synapse.GUI.ViewModels.Controls;
using Synapse.GUI.ViewModels.Pages;

namespace Synapse.GUI.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    public BottomBarViewModel BottomBarVm { get; }
    public SidebarLeftViewModel SidebarLeftVm { get; }
    public SidebarRightViewModel SidebarRightVm { get; }
    
    
    private readonly INavigationService _navigationService;
    
    [ObservableProperty] private PageViewModel? _currentPage;
    [ObservableProperty] private bool _hasCurrentPage;

    public MainViewModel(
        INavigationService navigationService,
        BottomBarViewModel bottomBarVm,
        SidebarLeftViewModel sidebarLeftVm,
        SidebarRightViewModel sidebarRightVm)
    {
        _navigationService = navigationService;
        _navigationService.CurrentPageChanged += OnCurrentPageChanged;
        
        BottomBarVm = bottomBarVm;
        SidebarLeftVm = sidebarLeftVm;
        SidebarRightVm = sidebarRightVm;

        CurrentPage = _navigationService.CurrentPage;
        HasCurrentPage = CurrentPage is not null;
    }
    
    private void OnCurrentPageChanged()
    {
        CurrentPage = _navigationService.CurrentPage;
        HasCurrentPage = CurrentPage is not null;
    }

    protected override void DisposeManaged()
    {
        _navigationService.CurrentPageChanged -= OnCurrentPageChanged;
        base.DisposeManaged();
    }
}
