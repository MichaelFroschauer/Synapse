using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Synapse.GUI.Models;
using Synapse.GUI.Services;

namespace Synapse.GUI.ViewModels.Controls;

public partial class SidebarLeftViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;

    [ObservableProperty] private bool _isAlgorithmSelected = false;
    [ObservableProperty] private bool _isHitlSelected = false;
    [ObservableProperty] private bool _isHitlScriptingSelected = false;
    [ObservableProperty] private bool _isResultsSelected = false;
    [ObservableProperty] private bool _isAiSelected = false;

    public SidebarLeftViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;

        _navigationService.CurrentPageChanged += OnCurrentPageChanged;
    }

    private void OnCurrentPageChanged()
    {
        IsAlgorithmSelected = false;
        IsHitlSelected = false;
        IsHitlScriptingSelected = false;
        IsResultsSelected = false;
        IsAiSelected = false;

        switch (_navigationService.CurrentPageName)
        {
            case ApplicationPageNames.AlgorithmConfigurator:
                IsAlgorithmSelected = true;
                break;
            case ApplicationPageNames.HitlConfigurator:
                IsHitlSelected = true;
                break;
            case ApplicationPageNames.HitlScripting:
                IsHitlScriptingSelected = true;
                break;
            case ApplicationPageNames.Results:
                IsResultsSelected = true;
                break;
            case ApplicationPageNames.AiConfigurator:
                IsAiSelected = true;
                break;
            case ApplicationPageNames.Unknown:
            default:
                break;
        }
    }
    
    [RelayCommand]
    private void NavigateToAlgorithm() => _navigationService.Navigate(ApplicationPageNames.AlgorithmConfigurator);

    [RelayCommand]
    private void NavigateToHitl() => _navigationService.Navigate(ApplicationPageNames.HitlConfigurator);
    [RelayCommand]
    private void NavigateToHitlScripting() => _navigationService.Navigate(ApplicationPageNames.HitlScripting);

    [RelayCommand]
    private void NavigateToResults() => _navigationService.Navigate(ApplicationPageNames.Results);

    [RelayCommand]
    private void NavigateToAi() => _navigationService.Navigate(ApplicationPageNames.AiConfigurator);

    protected override void DisposeManaged()
    {
        _navigationService.CurrentPageChanged -= OnCurrentPageChanged;
        base.DisposeManaged();
    }
}
