using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using ActiproSoftware.Extensions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Synapse.GUI.Models.Messages;
using Synapse.GUI.Services;
using Synapse.JobManagement;
using Synapse.OptimizationCore.Common;
using Synapse.OptimizationCore.Interfaces;
using Synapse.Reflection;
using Synapse.Reflection.Descriptors;
using Synapse.Reflection.Models;

namespace Synapse.GUI.ViewModels.Controls;

public partial class AlgorithmSelectionViewModel : ViewModelBase
{
    private readonly IJobSelectorService _jobSelectorService;
    private readonly IJobManager _jobManager;
    private readonly IAlgorithmConfigViewModelService _algorithmParameterViewModelService;
    
    [ObservableProperty] private ObservableCollection<AlgorithmDescriptor> _algorithms = new(); 
    [ObservableProperty] private AlgorithmDescriptor? _selectedAlgorithmType;
    
    [ObservableProperty] private ObservableCollection<ProblemDescriptor> _problems = new(); 
    [ObservableProperty] private ProblemDescriptor? _selectedProblem;
    
    [ObservableProperty] private ObservableCollection<ProblemInstanceDescriptor> _problemInstances = new(); 
    [ObservableProperty] private ProblemInstanceDescriptor? _selectedProblemInstance;
    [ObservableProperty] private bool _problemInstanceSelected;
    
    [ObservableProperty] private ObservableCollection<UserSettableProperty> _selectedUserSettableProperties = new(); 
    
    [ObservableProperty] private bool _jobNotActive;
    
    public AlgorithmSelectionViewModel(
        IJobSelectorService jobSelectorService,
        IJobManager jobManager,
        IDescriptorRegistry descriptorRegistry,
        IAlgorithmConfigViewModelService algorithmParameterViewModelService)
    {
        _jobManager = jobManager;
        _jobSelectorService = jobSelectorService;
        _jobSelectorService.SelectedJobChanged += OnSelectedJobChanged;
        
        _algorithmParameterViewModelService = algorithmParameterViewModelService;

        Algorithms.AddRange(descriptorRegistry.Get<AlgorithmDescriptor>());
        Problems.AddRange(descriptorRegistry.Get<ProblemDescriptor>());
        
        JobNotActive = !jobManager.JobExists(jobSelectorService.SelectedJobId);
        WeakReferenceMessenger.Default.Register<AlgorithmStartedMessage>(this, void (r, msg) => JobNotActive = false);
        WeakReferenceMessenger.Default.Register<AlgorithmConfigCloned>(this, void (r, msg) => JobNotActive = true);

        OnSelectedJobChanged(null, null);
    }

    private void OnSelectedJobChanged(object? sender, SelectedJobChangedEventArgs? e)
    {
        var jobInfo = _jobManager.GetJobInfo(_jobSelectorService.SelectedJobId);
        if (jobInfo is not null)
        {
            _algorithmParameterViewModelService.SetAlgorithmType(jobInfo.Config.AlgorithmType);
            JobNotActive = false;
            
            SelectedAlgorithmType = Algorithms.FirstOrDefault(a => a.AlgorithmType == jobInfo.Config.AlgorithmType);
            SelectedProblem = Problems.FirstOrDefault(p => p.ProblemType == jobInfo.Problem.ProblemType);
            SelectedProblemInstance = ProblemInstances.FirstOrDefault(p => p.Name == jobInfo.ProblemInstance.Name);
        }
        else
        {
            SelectedAlgorithmType = null;
            SelectedProblem = null;
            _algorithmParameterViewModelService.SetAlgorithmType(AlgorithmType.Unknown);
            JobNotActive = true;
        }
    }

    partial void OnSelectedAlgorithmTypeChanged(AlgorithmDescriptor? value)
    {
        if (value is null) return;
        _algorithmParameterViewModelService.SetAlgorithmType(value.AlgorithmType);
    }

    partial void OnSelectedProblemChanged(ProblemDescriptor? value)
    {
        ProblemInstanceSelected = false;
        if (value is null) return;

        ProblemInstances.Clear();
        SelectedUserSettableProperties.Clear();
        ProblemInstances.AddRange(value.ProblemInstances);
        ProblemInstanceSelected = true;
    }

    partial void OnSelectedProblemInstanceChanged(ProblemInstanceDescriptor? value)
    {
        SelectedUserSettableProperties.Clear();
        if (SelectedProblemInstance is null || string.IsNullOrEmpty(SelectedProblemInstance.Name) || SelectedProblem is null) return;
        
        if (SelectedProblemInstance.SettableClass?.UserSettableProperties is not null)
        {
            SelectedUserSettableProperties.AddRange(SelectedProblemInstance.SettableClass.UserSettableProperties);
        }
        var selectedProblem = SelectedProblemInstance.CreateProblem();
        WeakReferenceMessenger.Default.Send(new ProblemSelectedMessage(selectedProblem, GetSelectedProblemInstance()));
    }

    [RelayCommand]
    private void PropertyValueChanged((UserSettableProperty Prop, object? NewValue) args)
    {
        if (SelectedProblemInstance is null || string.IsNullOrEmpty(SelectedProblemInstance.Name) || SelectedProblem is null) return;
        var (prop, newValue) = args;
        var selectedProblem = SelectedProblemInstance.CreateProblem();
        WeakReferenceMessenger.Default.Send(new ProblemSelectedMessage(selectedProblem, GetSelectedProblemInstance()));
    }

    private IProblemInstance? GetSelectedProblemInstance()
    {
        if (SelectedProblemInstance is null) return null;
        
        IProblemInstance instance = new ProblemInstance
        {
            Name = SelectedProblemInstance.Name,
            ProblemType = SelectedProblemInstance.ProblemTypeEnum
        };
        return instance;
    }

    [RelayCommand]
    private async Task OpenFileDialogCommando()
    {
        var window = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        if (window is null) return;

        var selectedPath = await ShowFileDialogAsync(window);
        if (selectedPath != null && SelectedProblem != null /* && SelectedProblem.Parser != null */)
        {
            // TODO
            //SelectedProblem.ProblemInstances[0]
            
            //FileProblemInstances.Add(new ProblemInstance(selectedPath));
            //SelectedFileProblemInstance = new ProblemInstance(selectedPath);
        }
    }
    
    private static async Task<string?> ShowFileDialogAsync(Window parent)
    {
        var topLevel = TopLevel.GetTopLevel(parent);
        if (topLevel is null) return null;
        
        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open Text File",
            AllowMultiple = false
        });
        
        return files?.Count > 0 ? files[0].TryGetLocalPath() : null;
    }
    
    protected override void DisposeManaged()
    {
        _jobSelectorService.SelectedJobChanged -= OnSelectedJobChanged;
        WeakReferenceMessenger.Default.UnregisterAll(this);
        base.DisposeManaged();
    }
}
