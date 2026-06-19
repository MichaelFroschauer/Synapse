using System;
using System.ComponentModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.VisualTree;
using Synapse.GUI.Converters;
using Synapse.GUI.Views.Controls;
using Synapse.Reflection.Models;

namespace Synapse.GUI.Models;

public sealed class PropertyTemplateSelector : IDataTemplate
{
    public bool Match(object? data) => data is UserSettableProperty;

    public Control Build(object? param)
    {
        var p = (UserSettableProperty)param!;
        var t = p.Type;

        var panel = new StackPanel { Spacing = 4 };
        panel.Children.Add(new TextBlock
        {
            Text = p.DisplayName,
            Classes = { "theme-form-control-label" }
        });
        
        Control editor;

        if (t == typeof(string))
        {
            var tb = new TextBox();
            tb.Bind(TextBox.TextProperty, new Binding(nameof(UserSettableProperty.Value))
            {
                Source = p,
                Mode = BindingMode.TwoWay,
                Converter = BoxedStringConverter.Instance
            });
            editor = tb;
        }
        else if (t == typeof(bool))
        {
            var sw = new ToggleSwitch();
            sw.Bind(ToggleButton.IsCheckedProperty, new Binding(nameof(UserSettableProperty.Value))
            {
                Source = p,
                Mode = BindingMode.TwoWay,
                Converter = BoxedBoolConverter.Instance
            });
            editor = sw;
        }
        else if (t == typeof(int))
        {
            var nud = new NumericUpDown
            {
                // Optional – wenn du Min/Max/Step in UserSettableProperty ergänzt, hier anwenden:
                Minimum = 0, Maximum = 1_000_000, Increment = 1
            };
            nud.Bind(NumericUpDown.ValueProperty, new Binding(nameof(UserSettableProperty.Value))
            {
                Source = p,
                Mode = BindingMode.TwoWay,
                Converter = BoxedIntConverter.Instance
            });
            editor = nud;
        }
        else if (t == typeof(long))
        {
            var nud = new NumericUpDown
            {
                // Optional – wenn du Min/Max/Step in UserSettableProperty ergänzt, hier anwenden:
                Minimum = long.MinValue, Maximum = long.MaxValue, Increment = 1
            };
            nud.Bind(NumericUpDown.ValueProperty, new Binding(nameof(UserSettableProperty.Value))
            {
                Source = p,
                Mode = BindingMode.TwoWay,
                Converter = BoxedLongConverter.Instance
            });
            editor = nud;
        }
        else if (t == typeof(double))
        {
            var nud = new NumericUpDown { Minimum = decimal.MinValue, Maximum = decimal.MaxValue, Increment = (decimal)0.1 };
            nud.Bind(NumericUpDown.ValueProperty, new Binding(nameof(UserSettableProperty.Value))
            {
                Source = p, Mode = BindingMode.TwoWay, Converter = BoxedDoubleConverter.Instance
            });
            editor = nud;
        }
        else if (t == typeof(decimal))
        {
            var nud = new NumericUpDown { Minimum = (decimal)-1e9, Maximum = (decimal)1e9, Increment = (decimal)0.1 };
            nud.Bind(NumericUpDown.ValueProperty, new Binding(nameof(UserSettableProperty.Value))
            {
                Source = p, Mode = BindingMode.TwoWay, Converter = BoxedDecimalConverter.Instance
            });
            editor = nud;
        }
        else if (t.IsEnum)
        {
            var values = Enum.GetValues(t).Cast<object>().ToArray();
            var cb = new ComboBox { ItemsSource = values };
            cb.Bind(SelectingItemsControl.SelectedItemProperty, new Binding(nameof(UserSettableProperty.Value))
            {
                Source = p, Mode = BindingMode.TwoWay
            });
            editor = cb;
        }
        else if (typeof(System.Collections.IEnumerable).IsAssignableFrom(t) && t != typeof(string))
        {
            var tb = new TextBox
            {
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                MinHeight = 100,
                Watermark = "One entry each line"
            };

            var enumerableConverter = new EnumerableTextConverter(t);
            tb.Bind(TextBox.TextProperty, new Binding(nameof(UserSettableProperty.Value))
            {
                Source = p,
                Mode = BindingMode.TwoWay,
                Converter = enumerableConverter
            });

            editor = tb;
        }
        else
        {
            editor = new TextBlock { Text = $"No Editor for type: {t.Name}" };
        }
        
        PropertyChangedEventHandler? handler = null;
        handler = (_, e) =>
        {
            if (e.PropertyName == nameof(UserSettableProperty.Value))
                OnValueChanged(p, p.Value, editor);
        };

        editor.AttachedToVisualTree += (_, __) => p.PropertyChanged += handler;
        editor.DetachedFromVisualTree += (_, __) => p.PropertyChanged -= handler;
        
        editor.Classes.Add("theme-outline");
        panel.Children.Add(editor);
        //panel.Margin = new Thickness(8, 0, 8, 8);
        return panel;
    }

    private static void OnValueChanged(UserSettableProperty prop, object? newValue, Control editor)
    {
        var panel = editor.FindAncestorOfType<PropertyEditorPanel>();
        var cmd = panel?.ValueChangedCommand;

        if (cmd?.CanExecute((prop, newValue)) == true)
            cmd.Execute((prop, newValue));
    }
}
