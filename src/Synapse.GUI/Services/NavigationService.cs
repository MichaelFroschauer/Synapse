using System;
using Synapse.GUI.Models;
using Synapse.GUI.ViewModels.Pages;

namespace Synapse.GUI.Services;

public class NavigationService(IPageFactory pageFactory) : INavigationService
{
    private PageViewModel? _currentPage = null;
    private ApplicationPageNames _currentPageName = ApplicationPageNames.Unknown;

    
    public void Navigate(ApplicationPageNames pageName)
    {
        _currentPageName =  pageName;
        var pageVm = pageFactory.GetPageViewModel(pageName);
        CurrentPage = pageVm;
    }

    public PageViewModel? CurrentPage
    {
        get => _currentPage;
        private set
        {
            _currentPage = value;
            NotifyCurrentPageChanged();
        }
    }

    public ApplicationPageNames CurrentPageName => _currentPageName;
    public event Action? CurrentPageChanged;
    private void NotifyCurrentPageChanged() => CurrentPageChanged?.Invoke();
}
