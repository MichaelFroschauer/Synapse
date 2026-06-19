using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Drawing.Geometries;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.VisualElements;
using LiveChartsCore.VisualElements;
using Synapse.GUI.Services;
using Synapse.JobManagement;
using Synapse.OptimizationCore.Common;
using Synapse.Problems.JSP;
using SkiaSharp;
using Synapse.OptimizationCore.Common.Impl;
using Synapse.OptimizationCore.Interfaces;

namespace Synapse.GUI.ViewModels.Controls.AlgorithmResults;

public partial class JspResultViewModel : ProblemResultViewModel
{
    [ObservableProperty] private int[]? _solutionValues;
    [ObservableProperty] private double? _minChartHeight = 200.0;
    public ObservableCollection<VisualElement> Visuals { get; } = new();
    
    public JspResultViewModel(IJobManager jobManager, IJobSelectorService jobSelector)
        : base(jobManager, jobSelector)
    {
        ProblemType = ProblemType.Jsp;
    }
    
    public Axis[] YAxes { get; set; } =
    {
        new Axis {
            Name = "MachineAxis",
            NamePadding = new LiveChartsCore.Drawing.Padding(0, 7),
            Labeler = (value) => $"M {((int)value).ToString()}",
            SeparatorsAtCenter = true,
            MinStep = 1,
            ForceStepToMin = true
        }
    };

    public Axis[] XAxes { get; set; } =
    {
        new Axis {
            Name = "Time",
            MinStep = 1,
            ForceStepToMin = false,
        }
    };


    protected override void JobProgressChanged() => DrawJspSolution(false);
    protected override void SetStaticSolution() => DrawJspSolution(true);

    private void DrawJspSolution(bool useStaticSolution)
    {
        var jobInfo = JobSelector.SelectedJobInfo;
        if (jobInfo is not null)
        {
            if (jobInfo.Id != LastSelectedJobId)
            {
                LastSelectedJobId = jobInfo.Id;

                if (jobInfo.Problem is not JspProblem problem) return;
                MinChartHeight = problem.NumMachines * 80;
                if (YAxes.Length > 0)
                {
                    YAxes[0].MinLimit = 0.5;
                    YAxes[0].MaxLimit = problem.MachinesCount + 0.5;
                }
            }
            
            ISolution? solution;
            double solutionFitness = 0.0;
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
            if (solution is PermutationSolution jspSolution)
            {
                var newSolutionValues = jspSolution.GetParametersWithType();
                if (newSolutionValues.Equals(SolutionValues)) return;
                SolutionValues = newSolutionValues;
                Fitness = solutionFitness;
                CreatedAt = jobInfo.CreatedAt;
                StartedAt = jobInfo.StartedAt;
                FinishedAt = jobInfo.FinishedAt;
                Iteration = jobInfo.IterationCount;
                
                UpdateGanttSeries(jspSolution, jobInfo.Problem as JspProblem);
            }
        }
    }

    private void UpdateGanttSeries(PermutationSolution solution, JspProblem? problem)
    {
        if (problem == null || solution == null) return;

        var schedule = JspFitnessEvaluator.DecodeSchedule(problem, solution);
        
        int timeDivider = 1;
        Visuals.Clear();
        
        foreach (var op in schedule)
        {
            var color = GetJobColor(op.Job);
            var rectWidth = op.ProcessingTime / (double)timeDivider;
            
            // Rectangle geometry at X = start, Y = machine index
            var rectVisual = new GeometryVisual<RectangleGeometry>()
            {
                // position in chart (use chart-values units so coordinates match your axes)
                X = op.Start / (double)timeDivider,
                Y = op.Machine + 1.4,
                LocationUnit = MeasureUnit.ChartValues,

                // size in chart-values (so width = duration in same unit as X axis)
                Width = rectWidth,
                Height = 0.8, // less than 1 to leave gap between machine rows
                SizeUnit = MeasureUnit.ChartValues,

                // paints
                Fill = new SolidColorPaint(color),
                Stroke = new SolidColorPaint(SKColors.Black) { StrokeThickness = 1 },
                Label = $"Job {op.Job} / Op{op.OpIdx} [{op.Start}-{op.Finish}]",
                LabelPaint = new SolidColorPaint(SKColors.Black) { ZIndex = 11 },
                LabelSize = 11,
            };

            Visuals.Add(rectVisual);
        }
    }

    private SKColor GetJobColor(int job)
    {
        int r = (job * 50) % 256;
        int g = (job * 80) % 256;
        int b = (job * 130) % 256;
        return new SKColor((byte)r, (byte)g, (byte)b, (byte)50);
    }
}
