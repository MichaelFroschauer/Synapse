using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Threading;

namespace Synapse.GUI.Views.Controls.AlgorithmResults;

public partial class TspResultView : ProblemResultView
{
    private readonly Canvas? _canvas;

    public TspResultView()
    {
        InitializeComponent();
        _canvas = this.FindControl<Canvas>("SolutionResultCanvas");
    }
    
    protected override void Vm_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModels.Controls.AlgorithmResults.TspResultViewModel.SolutionValues) ||
            e.PropertyName == nameof(ViewModels.Controls.AlgorithmResults.TspResultViewModel.Cities))
        {
            Dispatcher.UIThread.InvokeAsync(TryUpdateMapping);
        }
    }

    protected override void TryUpdateMapping()
    {
        if (_canvas == null) return;
        if (DataContext is not Synapse.GUI.ViewModels.Controls.AlgorithmResults.TspResultViewModel vm) return;
        var bounds = _canvas.Bounds;
        double width = bounds.Width > 0 ? bounds.Width : _canvas.Bounds.Width;
        double height = bounds.Height > 0 ? bounds.Height : _canvas.Bounds.Height;
        if (width <= 0 || height <= 0) return;
        vm.UpdateMappedPoints(width, height);
    }
}
