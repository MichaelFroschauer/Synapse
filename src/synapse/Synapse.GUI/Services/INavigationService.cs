using System;
using Synapse.GUI.Models;
using Synapse.GUI.ViewModels.Pages;

namespace Synapse.GUI.Services;

public interface INavigationService
{
    void Navigate(ApplicationPageNames pageName);
    PageViewModel? CurrentPage { get; }
    ApplicationPageNames CurrentPageName { get; }
    event Action? CurrentPageChanged;
}
