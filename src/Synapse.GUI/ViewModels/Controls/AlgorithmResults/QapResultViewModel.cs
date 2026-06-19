using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using Synapse.GUI.Models.Problems.QAP;
using Synapse.GUI.Services;
using Synapse.GUI.Utils;
using Synapse.JobManagement;
using Synapse.Problems.QAP;
using Synapse.OptimizationCore.Common;
using Synapse.OptimizationCore.Common.Impl;
using Synapse.OptimizationCore.Interfaces;

namespace Synapse.GUI.ViewModels.Controls.AlgorithmResults;

public partial class QapResultViewModel : ProblemResultViewModel
{
    public ObservableCollection<QapNode> Nodes { get; } = new();
    public ObservableCollection<QapEdge> Edges { get; } = new();
    public ObservableCollection<QapSolutionEntry> NodeSolutionEntry { get; } = new();
    
    [ObservableProperty] private int[]? _solutionValues;

    // raw matrices from problem
    private int[,]? _distances;
    private int[,]? _flows;

    // coordinates from MDS (raw)
    private double[,]? _rawCoordinates;

    // settings for edge drawing
    private const double MinEdgeThickness = 0.8;
    private const double MaxEdgeThickness = 8.0;
    private const double Padding = 100.0;

    public QapResultViewModel(IJobManager jobManager, IJobSelectorService jobSelector)
        : base(jobManager, jobSelector)
    {
        ProblemType = ProblemType.Qap;
    }

    protected override void JobProgressChanged() => DrawQapSolution(false);
    protected override void SetStaticSolution() => DrawQapSolution(true);

    private void DrawQapSolution(bool useStaticSolution)
    {
        var jobInfo = JobManager.GetJobInfo(JobSelector.SelectedJobId);
        if (jobInfo is not null && jobInfo.Problem.ProblemType == ProblemType.Qap)
        {
            if (jobInfo.Id != LastSelectedJobId)
            {
                LastSelectedJobId = jobInfo.Id;

                // load matrices and compute coordinates
                var problem = (jobInfo.Problem as QapProblem)!;
                _distances = problem.DistanceMatrix;
                _flows = problem.FlowMatrix;

                // compute raw coordinates from distances (MDS). 
                if (_distances != null)
                {
                    try
                    {
                        _rawCoordinates = MultidimensionalScaling.ClassicalMds(_distances, 2);
                    }
                    catch
                    {
                        _rawCoordinates = null;
                    }
                }

                Nodes.Clear();
                if (_rawCoordinates != null)
                {
                    int n = _rawCoordinates.GetLength(0);
                    for (int i = 0; i < n; i++)
                    {
                        double x = _rawCoordinates[i, 0];
                        double y = _rawCoordinates[i, 1];
                        Nodes.Add(new QapNode(i, x, y));
                    }
                }
                else if (_distances != null)
                {
                    // fallback: place nodes on circle if MDS failed
                    int n = _distances.GetLength(0);
                    for (int i = 0; i < n; i++)
                    {
                        double ang = 2.0 * Math.PI * i / n;
                        Nodes.Add(new QapNode(i, Math.Cos(ang), Math.Sin(ang)));
                    }
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
            if (solution is PermutationSolution qapSolution)
            {
                var newSolutionValues = qapSolution.GetParametersWithType();
                if (!newSolutionValues.SequenceEqual(SolutionValues ?? Array.Empty<int>()))
                {
                    SolutionValues = newSolutionValues;
                    // recompute edges immediately if we already have mapped points (UpdateMappedPoints will also call UpdateEdges)
                    UpdateEdgesIfPossible();
                }

                Fitness = solutionFitness;
                CreatedAt = jobInfo.CreatedAt;
                StartedAt = jobInfo.StartedAt;
                FinishedAt = jobInfo.FinishedAt;
                Iteration = jobInfo.IterationCount;
            }
        }
    }

    public void UpdateMappedPoints(double width, double height)
    {
        if (width <= 0 || height <= 0) return;
        if (Nodes.Count == 0) return;
        double maxX = Nodes.Max(c => c.X);
        double maxY = Nodes.Max(c => c.Y);
        double minX = Nodes.Min(c => c.X);
        double minY = Nodes.Min(c => c.Y);
        if (Math.Abs(maxX - minX) < 1e-6)
        {
            maxX = minX + 1.0;
        }

        if (Math.Abs(maxY - minY) < 1e-6)
        {
            maxY = minY + 1.0;
        }

        var mapX = new RangeMapper(minX, maxX, Padding, Math.Max(Padding, width - Padding));
        var mapY = new RangeMapper(minY, maxY, height - Padding, Padding);
        foreach (var node in Nodes)
        {
            node.DisplayX = mapX.Map(node.X);
            node.DisplayY = mapY.Map(node.Y);
        }

        // Update edges to new display coordinates
        UpdateEdgesIfPossible();
    }

    private void UpdateEdgesIfPossible()
    {
        if (Nodes.Count == 0 || SolutionValues == null || _flows == null) return;
        int nLoc = Nodes.Count;
        if (SolutionValues.Length < nLoc) return;
        
        var flowsList = new List<double>();
        for (int locI = 0; locI < nLoc; locI++)
        {
            for (int locJ = locI + 1; locJ < nLoc; locJ++)
            {
                int facI = SolutionValues[locI];
                int facJ = SolutionValues[locJ];
                if (facI < 0 || facJ < 0 || facI >= _flows.GetLength(0) || facJ >= _flows.GetLength(1)) continue;
                double f = Math.Abs(_flows[facI, facJ]);
                if (f > 0) flowsList.Add(f);
            }
        }

        double minFlow = flowsList.Count > 0 ? flowsList.Min() : 0.0;
        double maxFlow = flowsList.Count > 0 ? flowsList.Max() : 1.0;
        if (Math.Abs(maxFlow - minFlow) < 1e-9)
        {
            // avoid division by zero in scaling
            minFlow = 0.0;
        }

        Edges.Clear();
        for (int locI = 0; locI < nLoc; locI++)
        {
            for (int locJ = locI + 1; locJ < nLoc; locJ++)
            {
                int facI = SolutionValues[locI];
                int facJ = SolutionValues[locJ];
                if (facI < 0 || facJ < 0) continue;
                if (facI >= _flows.GetLength(0) || facJ >= _flows.GetLength(1)) continue;
                double flow = _flows[facI, facJ];
                double absFlow = Math.Abs(flow);
                if (absFlow <= 0.0) continue; // skip zero flows to reduce clutter
                var nodeA = Nodes[locI];
                var nodeB = Nodes[locJ];
                double normalized = (minFlow == 0 && maxFlow == 0)
                    ? 1.0
                    : (Math.Max(0.0, absFlow - minFlow) / Math.Max(1e-9, (maxFlow - minFlow)));
                double thickness = MinEdgeThickness + normalized * (MaxEdgeThickness - MinEdgeThickness);
                
                // Color deterministic based of the facility pairs
                var brush = GetBrushForPair(facI, facJ);
                var edge = new QapEdge
                {
                    StartPoint = new Avalonia.Point(nodeA.DisplayX, nodeA.DisplayY),
                    EndPoint = new Avalonia.Point(nodeB.DisplayX, nodeB.DisplayY),
                    Thickness = thickness,
                    Brush = brush,
                    FlowValue = flow,
                    Opacity = 0.6 + 0.4 * normalized,
                    LocationA = locI,
                    LocationB = locJ,
                    FacilityA = facI,
                    FacilityB = facJ
                };
                Edges.Add(edge);
            }
        }
        
        NodeSolutionEntry.Clear();
        for (int loc = 0; loc < nLoc; loc++)
        {
            NodeSolutionEntry.Add(new QapSolutionEntry()
            {
                Location = loc,
                Facility = SolutionValues[loc],
                DisplayX = Nodes[loc].DisplayX,
                DisplayY = Nodes[loc].DisplayY
            });
        }
    }

    // deterministic color per pair (so colors remain stable between redraws)
    private IBrush GetBrushForPair(int facA, int facB)
    {
        int seed = facA * 73856093 ^ facB * 19349663;
        var rnd = new Random(seed);
        double hue = rnd.NextDouble() * 360.0;
        double s = 0.6 + rnd.NextDouble() * 0.4;
        double v = 0.6 + rnd.NextDouble() * 0.4;
        var color = HsvToColor(hue, s, v);
        return new SolidColorBrush(color);
    }

    private static Color HsvToColor(double h, double s, double v)
    {
        // h: 0..360, s:0..1, v:0..1
        double c = v * s;
        double hh = h / 60.0;
        double x = c * (1 - Math.Abs(hh % 2 - 1));
        double r = 0, g = 0, b = 0;
        if (0 <= hh && hh < 1)
        {
            r = c;
            g = x;
            b = 0;
        }
        else if (1 <= hh && hh < 2)
        {
            r = x;
            g = c;
            b = 0;
        }
        else if (2 <= hh && hh < 3)
        {
            r = 0;
            g = c;
            b = x;
        }
        else if (3 <= hh && hh < 4)
        {
            r = 0;
            g = x;
            b = c;
        }
        else if (4 <= hh && hh < 5)
        {
            r = x;
            g = 0;
            b = c;
        }
        else if (5 <= hh && hh < 6)
        {
            r = c;
            g = 0;
            b = x;
        }

        double m = v - c;
        byte R = (byte)Math.Round((r + m) * 255);
        byte G = (byte)Math.Round((g + m) * 255);
        byte B = (byte)Math.Round((b + m) * 255);
        return Color.FromRgb(R, G, B);
    }
}