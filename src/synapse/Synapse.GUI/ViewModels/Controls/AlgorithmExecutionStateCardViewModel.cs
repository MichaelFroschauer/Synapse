using System;
using System.Linq;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Synapse.JobManagement;

namespace Synapse.GUI.ViewModels.Controls;

public partial class AlgorithmExecutionStateCardViewModel : ViewModelBase
{
    private readonly IJobManager _jobManager;

    // Pre-allocated brushes to avoid GC pressure on every progress tick
    private static readonly IBrush RunningBg = new SolidColorBrush(Color.Parse("#f0fdf4"));
    private static readonly IBrush RunningBorder = new SolidColorBrush(Color.Parse("#bbf7d0"));
    private static readonly IBrush PausedBg = new SolidColorBrush(Color.Parse("#fef9c3"));
    private static readonly IBrush PausedBorder = new SolidColorBrush(Color.Parse("#fde047"));
    private static readonly IBrush StoppedBg = new SolidColorBrush(Color.Parse("#f3f4f6"));
    private static readonly IBrush StoppedBorder = new SolidColorBrush(Color.Parse("#d1d5db"));
    private static readonly IBrush FailedBg = new SolidColorBrush(Color.Parse("#fef2f2"));
    private static readonly IBrush FailedBorder = new SolidColorBrush(Color.Parse("#fca5a5"));
    private static readonly IBrush CompletedBg = new SolidColorBrush(Color.Parse("#ecfdf5"));
    private static readonly IBrush CompletedBorder = new SolidColorBrush(Color.Parse("#6ee7b7"));
    private static readonly IBrush DefaultBg = new SolidColorBrush(Color.Parse("#ffffff"));
    private static readonly IBrush DefaultBorder = new SolidColorBrush(Color.Parse("#e5e7eb"));

    [ObservableProperty] private Guid _id;
    [ObservableProperty] private string _name = null!;
    [ObservableProperty] private JobStatus _status;
    [ObservableProperty] private double _bestFitness;
    [ObservableProperty] private int _iterations;
    [ObservableProperty] private int _maxIterations;
    
    [ObservableProperty] private string _subTitle = null!;
    [ObservableProperty] private IBrush _cardBackground = DefaultBg;
    [ObservableProperty] private IBrush _cardBorderBrush = DefaultBorder;
    
    [ObservableProperty] private string? _triggerReason;
    [ObservableProperty] private bool _hasTriggerReason;
    
    [ObservableProperty] private string _idString = string.Empty;
    [ObservableProperty] private string _algorithmType = string.Empty;
    [ObservableProperty] private string _problemType = string.Empty;
    [ObservableProperty] private string _createdAtTime = string.Empty;
    [ObservableProperty] private string _startedAtTime = string.Empty;
    [ObservableProperty] private string _finishedAtTime = string.Empty;

    public AlgorithmExecutionStateCardViewModel(JobInfo jobInfo, IJobManager jobManager)
    {
        _jobManager = jobManager;
        SetProperties(jobInfo);
    }

    public void UpdateJob(JobInfo jobInfo)
    {
        SetProperties(jobInfo);
    }
    
    private static string FormatTime(DateTimeOffset? dt)
        => dt.HasValue ? dt.Value.ToString("yyyy-MM-dd HH:mm") : "-";

    private void SetProperties(JobInfo jobInfo)
    {
        Id = jobInfo.Id;
        var idName = jobInfo.Id.ToString().Split('-', 2).FirstOrDefault() ?? jobInfo.Id.ToString();
        Name = string.IsNullOrWhiteSpace(jobInfo.Config.Name) ? idName : jobInfo.Config.Name;
        Status = jobInfo.Status;
        BestFitness = Math.Abs(jobInfo.BestFitness ?? 0.0);
        Iterations = jobInfo.IterationCount;
        MaxIterations = jobInfo.Config.MaxIterations;
        
        IdString = jobInfo.Id.ToString();
        AlgorithmType = jobInfo.Config.AlgorithmType.ToString();
        ProblemType = jobInfo.Problem.ProblemType.ToString();
        CreatedAtTime = FormatTime(jobInfo.CreatedAt);
        StartedAtTime = FormatTime(jobInfo.StartedAt);
        FinishedAtTime = FormatTime(jobInfo.FinishedAt);
        
        SubTitle = $"Gen: {Iterations}/{MaxIterations} | Fitness: {BestFitness:F2}";
        
        // Show trigger reason when algorithm was paused by an interaction trigger
        var triggerReason = jobInfo.Config.HitlController?.LastTriggerReason;
        if (jobInfo.Status == JobStatus.Paused && !string.IsNullOrEmpty(triggerReason))
        {
            TriggerReason = triggerReason;
            HasTriggerReason = true;
        }
        else
        {
            TriggerReason = null;
            HasTriggerReason = false;
        }
        
        switch (jobInfo.Status)
        {
            case JobStatus.Running:
                CardBackground = RunningBg;
                CardBorderBrush = RunningBorder;
                break;
            case JobStatus.Paused:
                CardBackground = PausedBg;
                CardBorderBrush = PausedBorder;
                break;
            case JobStatus.Stopped:
                CardBackground = StoppedBg;
                CardBorderBrush = StoppedBorder;
                break;
            case JobStatus.Failed:
                CardBackground = FailedBg;
                CardBorderBrush = FailedBorder;
                break;
            case JobStatus.Completed:
                CardBackground = CompletedBg;
                CardBorderBrush = CompletedBorder;
                break;
            case JobStatus.Created:
            case JobStatus.Queued:
            case JobStatus.Cancelled:
            default:
                CardBackground = DefaultBg;
                CardBorderBrush = DefaultBorder;
                break;
        }
    }

    [RelayCommand]
    private void DeleteJob()
    {
        _ =_jobManager.DeleteJobAsync(Id, true);
    }
}