using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using Synapse.GUI.Models;
using Synapse.GUI.Models.Problems.TSP;
using Synapse.GUI.Services;
using Synapse.GUI.Utils;
using Synapse.JobManagement;
using Synapse.OptimizationCore.Common;
using Synapse.OptimizationCore.Common.Impl;
using Synapse.OptimizationCore.Interfaces;
using Synapse.Problems.TSP;

namespace Synapse.GUI.ViewModels.Controls.AlgorithmResults;

public partial class TspResultViewModel : ProblemResultViewModel
{
    public ObservableCollection<City> Cities { get; } = new();
    [ObservableProperty] private Point _startCity; 
    public ObservableCollection<Arrow> Arrows { get; } = new();
    [ObservableProperty] private int[]? _solutionValues;
    
    private IEnumerable<Point> _routePoints = Enumerable.Empty<Point>();

    public TspResultViewModel(IJobManager jobManager, IJobSelectorService jobSelector)
        : base(jobManager, jobSelector)
    {
        ProblemType = ProblemType.Tsp;
    }

    protected override void JobProgressChanged() => DrawTspSolution(false);
    protected override void SetStaticSolution() => DrawTspSolution(true);

    private void DrawTspSolution(bool useStaticSolution)
    {
        var jobInfo = JobManager.GetJobInfo(JobSelector.SelectedJobId);
        if (jobInfo is not null && jobInfo.Problem.ProblemType == ProblemType.Tsp)
        {
            if (jobInfo.Id != LastSelectedJobId)
            {
                LastSelectedJobId = jobInfo.Id;
                
                Cities.Clear();
                var problem = (jobInfo.Problem as TspProblem)!;
                var coordinates = problem.Coordinates;
                for (int i = 0; i < coordinates.Count; i++)
                {
                    Cities.Add(new City(i, coordinates[i].X, coordinates[i].Y));
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
            if (solution is PermutationSolution tspSolution)
            {
                var newSolutionValues = tspSolution.GetParametersWithType();
                if (newSolutionValues.Equals(SolutionValues)) return;
                SolutionValues = newSolutionValues;
                Fitness = solutionFitness;
                CreatedAt = jobInfo.CreatedAt;
                StartedAt = jobInfo.StartedAt;
                FinishedAt = jobInfo.FinishedAt;
                Iteration = jobInfo.IterationCount;
            }
        }
    }

    public IEnumerable<Point> RoutePoints
    {
        get => _routePoints;
        private set => SetProperty(ref _routePoints, value);
    }

    public void UpdateMappedPoints(double width, double height)
    {
        if (width <= 0 || height <= 0) return;
        if (Cities.Count == 0) return;

        const double padding = 30.0;
        
        double maxX = Cities.Max(c => c.X);
        double maxY = Cities.Max(c => c.Y);
        double minX = Cities.Min(c => c.X);
        double minY = Cities.Min(c => c.Y);
        
        if (Math.Abs(maxX - minX) < 1e-6) { maxX = minX + 1.0; }
        if (Math.Abs(maxY - minY) < 1e-6) { maxY = minY + 1.0; }

        var mapX = new RangeMapper(minX, maxX, padding, Math.Max(padding, width - padding));
        var mapY = new RangeMapper(minY, maxY, height - padding, padding);

        foreach (var city in Cities)
        {
            city.DisplayX = mapX.Map(city.X);
            city.DisplayY = mapY.Map(city.Y);
        }
        
        Arrows.Clear();
        if (SolutionValues is not null && SolutionValues.Length > 0)
        {
            var pts = new List<Point>(SolutionValues.Length + 1);
            foreach (var idx in SolutionValues)
            {
                if (idx < 0 || idx >= Cities.Count) continue;
                var c = Cities[idx];
                pts.Add(new Point(c.DisplayX, c.DisplayY));
            }
            // Close tour
            var startIdx = SolutionValues[0];
            if (startIdx >= 0 && startIdx < Cities.Count)
            {
                var s = Cities[startIdx];
                pts.Add(new Point(s.DisplayX, s.DisplayY));
                StartCity = new Point(s.DisplayX, s.DisplayY);
            }
            // RoutePoints Property
            RoutePoints = pts;
            
            for (int i = 0; i < pts.Count - 1; i += 4)
            {
                var p1 = pts[i];
                var p2 = pts[i + 1];
                double dx = p2.X - p1.X;
                double dy = p2.Y - p1.Y;
                double angleRad = Math.Atan2(dy, dx) + Math.PI / 2.0;
                double angleDeg = angleRad * 180.0 / Math.PI;
                
                // position at the middle of the edge
                double t = 0.5;
                double ax = p1.X + t * dx;
                double ay = p1.Y + t * dy;
                
                Arrows.Add(new Arrow(ax, ay, angleDeg));
            }
        }
        else
        {
            // if no solution -> empty route
            RoutePoints = Enumerable.Empty<Point>();
        }
    }
}
