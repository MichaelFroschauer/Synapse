using Avalonia.Controls;
using Avalonia.Input;
using Synapse.GUI.ViewModels.Pages;

namespace Synapse.GUI.Views.Pages;

public partial class AiConfiguratorPageView : UserControl
{
    public AiConfiguratorPageView()
    {
        InitializeComponent();
    }

    private void PromptTextBox_KeyUp(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && !e.KeyModifiers.HasFlag(KeyModifiers.Shift))
        {
            e.Handled = true;

            if (DataContext is AiConfiguratorPageViewModel vm)
            {
                var cmd = vm.SendPromptCommand;
                if (cmd?.CanExecute(null) ?? false)
                {
                    cmd.Execute(null);
                }
            }
        }
    }
}
