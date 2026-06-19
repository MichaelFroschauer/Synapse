using System;
using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using LiveChartsCore.Defaults;
using Synapse.GUI.Models;
using Synapse.GUI.Services;
using Synapse.GUI.ViewModels.Controls.AlgorithmResults;
using Synapse.JobManagement;
using Synapse.OptimizationCore.Common;

namespace Synapse.GUI.ViewModels.Pages;

public partial class ResultsPageViewModel : PageViewModel
{
    private readonly IProblemResultViewModelService _problemResultViewModelService;
    private readonly IJobManager _jobManager;
    private readonly IJobSelectorService _jobSelectorService;

    [ObservableProperty] private ProblemResultViewModel? _bestProblemResults;
    [ObservableProperty] private ProblemResultViewModel? _currentProblemResults;
    
    // Fitness statistics
    [ObservableProperty] private double? _bestFitness;
    [ObservableProperty] private double? _bestRawFitness;
    [ObservableProperty] private double? _currentBestFitness;
    [ObservableProperty] private double? _currentBestRawFitness;
    [ObservableProperty] private int _totalIterations;
    [ObservableProperty] private string _jobStatus = "—";
    [ObservableProperty] private string _elapsedTime = "—";
    [ObservableProperty] private string _improvementRate = "—";
    [ObservableProperty] private double? _firstFitness;
    [ObservableProperty] private string _fitnessImprovement = "—";
    [ObservableProperty] private string _algorithmName = "—";
    [ObservableProperty] private string _problemName = "—";
    [ObservableProperty] private double? _currentDiversity;
    
    public ObservableCollection<ObservablePoint> FitnessPerIterationValues { get; private set; } = new();
    public ObservableCollection<ObservablePoint> RawFitnessPerIterationValues { get; private set; } = new();
    public ObservableCollection<ObservablePoint> PopulationFitnessPerIterationValues { get; private set; } = new();
    public ObservableCollection<ObservablePoint> PopulationRawFitnessPerIterationValues { get; private set; } = new();

    public ObservableCollection<ObservablePoint> DiversityPerIterationValues { get; private set; } = new();
    
    private EventHandler<(Guid JobId, ProgressEventArgs Progress)>? _jobProgressHandler;
    private int _improvementCount;

    public ResultsPageViewModel(
        IProblemResultViewModelService problemResultViewModelService,
        IJobManager jobManager,
        IJobSelectorService jobSelectorService)
    {
        PageName = ApplicationPageNames.Results;
        
        _problemResultViewModelService = problemResultViewModelService;
        _jobManager = jobManager;
        _jobSelectorService = jobSelectorService;

        if (_jobSelectorService.SelectedJobInfo is not null) 
            _problemResultViewModelService.SetProblemType(_jobSelectorService.SelectedJobInfo.Problem.ProblemType);

        
        _jobProgressHandler = (sender, jobProgress) => UpdateJobProgress(jobProgress);
        _jobManager.JobProgress += _jobProgressHandler;
        _jobSelectorService.SelectedJobChanged += SelectedJobChanged;
        
        _problemResultViewModelService.CurrentProblemTypeChanged += OnCurrentProblemResultChanged;
        BestProblemResults = _problemResultViewModelService.BestProblemResultViewModel;
        CurrentProblemResults = _problemResultViewModelService.CurrentProblemResultViewModel;
        if (CurrentProblemResults is not null) CurrentProblemResults.ShowCurrentNonOptimalResults = true;

        UpdateFitnessLineChart();
        UpdateStatistics();
    }

    private void UpdateFitnessLineChart()
    {
        var jobInfo = _jobManager.GetJobInfo(_jobSelectorService.SelectedJobId);
        if (jobInfo is not null)
        {
            try
            {
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    FitnessPerIterationValues.Clear();
                    jobInfo.FitnessPerIteration.ForEach(v =>
                        FitnessPerIterationValues.Add(new ObservablePoint(v.Iteration, Math.Abs(v.Fitness))));
                    
                    RawFitnessPerIterationValues.Clear();
                    jobInfo.RawFitnessPerIteration.ForEach(v =>
                        RawFitnessPerIterationValues.Add(new ObservablePoint(v.Iteration, Math.Abs(v.Fitness))));
                    
                    PopulationFitnessPerIterationValues.Clear();
                    jobInfo.PopulationFitnessPerIteration.ForEach(v =>
                        PopulationFitnessPerIterationValues.Add(new ObservablePoint(v.Iteration, Math.Abs(v.Fitness))));
                    
                    PopulationRawFitnessPerIterationValues.Clear();
                    jobInfo.PopulationRawFitnessPerIteration.ForEach(v =>
                        PopulationRawFitnessPerIterationValues.Add(new ObservablePoint(v.Iteration, Math.Abs(v.Fitness))));
                    
                    DiversityPerIterationValues.Clear();
                    jobInfo.DiversityPerIteration.ForEach(v =>
                        DiversityPerIterationValues.Add(new ObservablePoint(v.Iteration, v.Diversity)));
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ResultsVM] Chart update error: {ex.Message}");
            }
        }
    }

    private void UpdateJobProgress((Guid JobId, ProgressEventArgs Progress) jobProgress)
    {
        if (jobProgress.JobId != _jobSelectorService.SelectedJobId) return;
        var iteration = jobProgress.Progress.Iteration;
        var fitness = Math.Abs(jobProgress.Progress.BestFitness);
        var rawFitness = Math.Abs(jobProgress.Progress.BestRawFitness);
        var populationFitness = Math.Abs(jobProgress.Progress.CurrentBestFitness);
        var populationRawFitness = Math.Abs(jobProgress.Progress.CurrentBestRawFitness);
        
        // Track if this is a fitness improvement
        if (BestFitness is null || fitness < BestFitness)
            _improvementCount++;
        
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            FitnessPerIterationValues.Add(new ObservablePoint(iteration, fitness));
            RawFitnessPerIterationValues.Add(new ObservablePoint(iteration, rawFitness));
            
            PopulationFitnessPerIterationValues.Add(new ObservablePoint(iteration, populationFitness));
            PopulationRawFitnessPerIterationValues.Add(new ObservablePoint(iteration, populationRawFitness));
            
            if (jobProgress.Progress.Diversity.HasValue)
            {
                DiversityPerIterationValues.Add(new ObservablePoint(iteration, jobProgress.Progress.Diversity.Value));
                CurrentDiversity = jobProgress.Progress.Diversity.Value;
            }
            
            BestFitness = fitness;
            BestRawFitness = rawFitness;
            CurrentBestFitness = Math.Abs(jobProgress.Progress.CurrentBestFitness);
            CurrentBestRawFitness = Math.Abs(jobProgress.Progress.CurrentBestRawFitness);
            TotalIterations = iteration;
            
            if (FirstFitness is null && FitnessPerIterationValues.Count > 0)
                FirstFitness = FitnessPerIterationValues[0].Y;
            
            UpdateStatistics();
        });
    }

    private void SelectedJobChanged(object? sender, SelectedJobChangedEventArgs? e)
    {
        var jobInfo = _jobManager.GetJobInfo(_jobSelectorService.SelectedJobId);
        if (jobInfo != null)
        {
            _problemResultViewModelService.SetProblemType(jobInfo.Problem.ProblemType);
            _improvementCount = 0;
            FirstFitness = null;
            UpdateFitnessLineChart();
            UpdateStatistics();
        }
    }

    private void OnCurrentProblemResultChanged()
    {
        BestProblemResults = _problemResultViewModelService.BestProblemResultViewModel;
        CurrentProblemResults = _problemResultViewModelService.CurrentProblemResultViewModel;
        if (CurrentProblemResults is not null) CurrentProblemResults.ShowCurrentNonOptimalResults = true;
    }

    private void UpdateStatistics()
    {
        var jobInfo = _jobManager.GetJobInfo(_jobSelectorService.SelectedJobId);
        if (jobInfo is null) return;
        
        BestFitness = jobInfo.BestFitness.HasValue ? Math.Abs(jobInfo.BestFitness.Value) : null;
        TotalIterations = jobInfo.IterationCount;
        JobStatus = jobInfo.Status.ToString();
        AlgorithmName = jobInfo.Config.AlgorithmType.ToString();
        ProblemName = jobInfo.Problem.ProblemType.ToString();
        
        // Elapsed time
        if (jobInfo.StartedAt.HasValue)
        {
            var end = jobInfo.FinishedAt ?? DateTimeOffset.UtcNow;
            var elapsed = end - jobInfo.StartedAt.Value;
            ElapsedTime = elapsed.TotalHours >= 1
                ? elapsed.ToString(@"hh\:mm\:ss")
                : elapsed.ToString(@"mm\:ss\.f");
        }
        else
        {
            ElapsedTime = "—";
        }
        
        // Fitness improvement percentage from first to best
        if (FirstFitness is null && FitnessPerIterationValues.Count > 0)
            FirstFitness = FitnessPerIterationValues[0].Y;
            
        if (FirstFitness.HasValue && BestFitness.HasValue && FirstFitness.Value != 0)
        {
            var pct = (FirstFitness.Value - BestFitness.Value) / FirstFitness.Value * 100.0;
            FitnessImprovement = $"{pct:F2}%";
        }
        else
        {
            FitnessImprovement = "—";
        }
        
        // Improvement rate: improvements per iteration
        if (TotalIterations > 0 && _improvementCount > 0)
        {
            var rate = (double)_improvementCount / TotalIterations * 100.0;
            ImprovementRate = $"{_improvementCount} ({rate:F1}% of iterations)";
        }
        else
        {
            ImprovementRate = "—";
        }
    }

    protected override void DisposeManaged()
    {
        _jobManager.JobProgress -= _jobProgressHandler;
        _jobSelectorService.SelectedJobChanged -= SelectedJobChanged;
        _problemResultViewModelService.CurrentProblemTypeChanged -= OnCurrentProblemResultChanged;
        base.DisposeManaged();
    }
}
