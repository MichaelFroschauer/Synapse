using System.ComponentModel;
using Avalonia.Threading;
using Synapse.GUI.ViewModels.Controls.AlgorithmResults;

namespace Synapse.GUI.Views.Controls.AlgorithmResults;

public partial class QapResultView : ProblemResultView
{
    public QapResultView()
    {
        InitializeComponent();
    }

    protected override void Vm_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(QapResultViewModel.SolutionValues) ||
            e.PropertyName == nameof(QapResultViewModel.Nodes) ||
            e.PropertyName == nameof(QapResultViewModel.Edges))
        {
            Dispatcher.UIThread.InvokeAsync(TryUpdateMapping);
        }
    }
    
    protected override void TryUpdateMapping()
    {
        if (SolutionResultCanvas == null) return;
        if (DataContext is not QapResultViewModel vm) return;
        var bounds = SolutionResultCanvas.Bounds;
        double width = bounds.Width > 0 ? bounds.Width : SolutionResultCanvas.Bounds.Width;
        double height = bounds.Height > 0 ? bounds.Height : SolutionResultCanvas.Bounds.Height;
        if (width <= 0 || height <= 0) return;
        vm.UpdateMappedPoints(width, height);
    }
}
