using System;
using Synapse.GUI.Models;
using Synapse.GUI.ViewModels.Pages;

namespace Synapse.GUI.Services;

public class PageFactory(Func<ApplicationPageNames, PageViewModel> factory) : IPageFactory
{
    public PageViewModel GetPageViewModel(ApplicationPageNames pageName) => factory(pageName);
}
