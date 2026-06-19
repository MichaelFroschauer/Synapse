using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using Synapse.GUI.Models.Problems.Function;
using Synapse.GUI.Services;
using Synapse.JobManagement;
using Synapse.OptimizationCore.Common;
using Synapse.OptimizationCore.Interfaces;

namespace Synapse.GUI.ViewModels.Controls.AlgorithmResults;

public partial class FunctionResultViewModel : ProblemResultViewModel
{
    public ObservableCollection<FunctionDimensionEntry> Dimensions { get; } = new();

    [ObservableProperty] private int _dimensionCount;
    [ObservableProperty] private double[]? _solutionValues;

    // Bar chart series bound from XAML
    [ObservableProperty] private ISeries[] _chartSeries = [];
    [ObservableProperty] private Axis[] _chartXAxes = [];
    [ObservableProperty] private Axis[] _chartYAxes = [new Axis { Name = "Value", TextSize = 12 }];

    public FunctionResultViewModel(IJobManager jobManager, IJobSelectorService jobSelector)
        : base(jobManager, jobSelector)
    {
        ProblemType = ProblemType.Function;
    }

    protected override void JobProgressChanged() => UpdateFunctionSolution(false);
    protected override void SetStaticSolution() => UpdateFunctionSolution(true);

    private void UpdateFunctionSolution(bool useStaticSolution)
    {
        var jobInfo = JobManager.GetJobInfo(JobSelector.SelectedJobId);
        if (jobInfo is null || jobInfo.Problem.ProblemType != ProblemType.Function)
            return;

        ISolution? solution;
        double solutionFitness;
        if (useStaticSolution)
        {
            solution = StaticSolution;
            solutionFitness = Math.Abs(StaticSolution?.Fitness ?? 0.0);
        }
        else
        {
            solution = ShowCurrentNonOptimalResults ? jobInfo.CurrentSolution : jobInfo.BestSolution;
            solutionFitness = Math.Abs(jobInfo.BestFitness ?? 0.0);
        }

        if (solution is null) return;

        double[] values;
        try
        {
            values = solution.GetParameters().Select(p => (double)p.Value).ToArray();
        }
        catch
        {
            return;
        }

        // Skip update if solution hasn't changed
        if (SolutionValues is not null && SolutionValues.SequenceEqual(values))
            return;

        SolutionValues = values;
        Fitness = solutionFitness;
        CreatedAt = jobInfo.CreatedAt;
        StartedAt = jobInfo.StartedAt;
        FinishedAt = jobInfo.FinishedAt;
        Iteration = jobInfo.IterationCount;
        DimensionCount = values.Length;

        // Update table entries
        Dimensions.Clear();
        for (int i = 0; i < values.Length; i++)
        {
            Dimensions.Add(new FunctionDimensionEntry { Index = i, Value = values[i] });
        }

        // Update bar chart
        var observableValues = values.Select((v, i) => new ObservablePoint(i, v)).ToArray();

        ChartSeries =
        [
            new ColumnSeries<ObservablePoint>
            {
                Values = observableValues,
                Name = "Dimension Values",
                MaxBarWidth = 50,
            }
        ];

        ChartXAxes =
        [
            new Axis
            {
                Name = "Dimension",
                TextSize = 12,
                Labels = Enumerable.Range(0, values.Length).Select(i => $"x{i}").ToArray(),
                LabelsRotation = values.Length > 20 ? 90 : 0,
                ShowSeparatorLines = false,
            }
        ];
    }
}
