using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using CommunityToolkit.Mvvm.Input;
using Synapse.GUI.Models;
using Synapse.OptimizationCore.Interfaces;
using Synapse.Problems.TSP;

namespace Synapse.GUI.ViewModels.Controls.SolutionInput;

public partial class TspInputViewModel : SolutionInputViewModel
{
    public ObservableCollection<IntValue> SolutionValues { get; } = new();

    public TspInputViewModel(int solutionLength) : base(solutionLength)
    {
        for (int i = 0; i < solutionLength; i++)
        {
            AddItem(0);
        }
    }
    
    public TspInputViewModel(ISolution solution) : base(solution)
    {
        if (solution is TspSolution s)
        {
            SolutionLength = s.Length;
            var p = s.GetParametersWithType();
            foreach (var t in p)
            {
                AddItem(t);
            }
        }
    }
    
    public TspInputViewModel() : base(0)
    {
        AddItem(0);
    }
    
    public override ISolution Solution =>
        new TspSolution(
            SolutionValues
                .Where((x, i) => !(i == SolutionValues.Count - 1 && x.Value == 0))
                .Select(x => x.Value)
                .ToArray()
        );

    [RelayCommand]
    private void DeleteSolutionInput()
    {
        var v = SolutionValues.LastOrDefault();
        if (v == null) return;
        v.PropertyChanged -= OnItemValueChanged;
        SolutionValues.Remove(v);
    }
    
    private void AddItem(int value)
    {
        var intValue = new IntValue { Value = value };
        intValue.PropertyChanged += OnItemValueChanged;
        SolutionValues.Add(intValue);
    }
    
    private void OnItemValueChanged(object? sender, EventArgs e)
    {
        if (sender is not IntValue item) return;

        var index = SolutionValues.IndexOf(item);
        if (index == -1) return;
        
        if (index == SolutionValues.Count - 1 && item.Value != 0)
        {
            AddItem(0);
        }
    }
}
