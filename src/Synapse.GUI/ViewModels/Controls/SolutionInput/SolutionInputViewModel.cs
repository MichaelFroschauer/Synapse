using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.Input;
using Synapse.GUI.Models;
using Synapse.OptimizationCore.Interfaces;

namespace Synapse.GUI.ViewModels.Controls.SolutionInput;

public abstract partial class SolutionInputViewModel : ViewModelBase
{
    public int SolutionLength { get; protected set; }
    
    protected SolutionInputViewModel(int solutionLength)
    {
        SolutionLength = solutionLength;
    }
    
    protected SolutionInputViewModel(ISolution solution)
    {
        SolutionLength = solution.Length;
    }
    
    public abstract ISolution Solution { get; }
}
