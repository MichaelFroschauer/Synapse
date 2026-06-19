using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Synapse.GUI.ViewModels.Controls;

public class ViewModelBase : ObservableObject, IDisposable
{
    private bool _isDisposed;
    
    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;
        DisposeManaged();
    }
    
    protected virtual void DisposeManaged() { }
}
