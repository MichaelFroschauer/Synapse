using Synapse.GUI.Models;
using Synapse.GUI.ViewModels.Pages;

namespace Synapse.GUI.Services;

public interface IPageFactory
{
    PageViewModel GetPageViewModel(ApplicationPageNames pageName);
}