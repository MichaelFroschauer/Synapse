using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.Input;
using Synapse.GUI.Models;
using Synapse.OptimizationCore.Interfaces;
using Synapse.Problems.QAP;

namespace Synapse.GUI.ViewModels.Controls.SolutionInput;

public partial class QapInputViewModel : SolutionInputViewModel
{
    public ObservableCollection<IntValue> SolutionValues { get; } = new();
    
    public QapInputViewModel(int solutionLength) : base(solutionLength)
    {
        for (int i = 0; i < solutionLength; i++)
        {
            AddItem(0);
        }
    }

    public QapInputViewModel(ISolution solution) : base(solution)
    {
        if (solution is QapSolution s)
        {
            SolutionLength = s.Length;
            var p = s.GetParametersWithType();
            foreach (var t in p)
            {
                AddItem(t);
            }
        }
    }
    
    public QapInputViewModel() : base(0)
    {
        AddItem(0);
    }

    
    // TODO fix - this is currently wrong ... also fix the QAP manual edit applier
    public override ISolution Solution =>
        new QapSolution(
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
        var intValue = new IntValue
        {
            Index = null,
            Value = value
        };
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
