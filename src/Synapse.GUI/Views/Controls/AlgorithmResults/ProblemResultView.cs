using System;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Threading;

namespace Synapse.GUI.Views.Controls.AlgorithmResults;

public abstract class ProblemResultView : UserControl
{
    protected INotifyPropertyChanged? _subscribedVm;
    
    public ProblemResultView()
    {
        SizeChanged += ProblemResultsView_SizeChanged;
        DataContextChanged += ProblemResultsView_DataContextChanged;
    }
    
    private void ProblemResultsView_DataContextChanged(object? sender, EventArgs e)
    {
        if (_subscribedVm != null)
        {
            _subscribedVm.PropertyChanged -= Vm_PropertyChanged;
            _subscribedVm = null;
        }

        if (DataContext is INotifyPropertyChanged inpc)
        {
            _subscribedVm = inpc;
            _subscribedVm.PropertyChanged += Vm_PropertyChanged;
        }
        //TryUpdateMapping();
        Dispatcher.UIThread.InvokeAsync(TryUpdateMapping);
    }

    private void ProblemResultsView_SizeChanged(object? sender, SizeChangedEventArgs e)
    {
        //TryUpdateMapping();
        Dispatcher.UIThread.InvokeAsync(TryUpdateMapping);
    }
    
    protected abstract void Vm_PropertyChanged(object? sender, PropertyChangedEventArgs e);

    protected abstract void TryUpdateMapping();
}
