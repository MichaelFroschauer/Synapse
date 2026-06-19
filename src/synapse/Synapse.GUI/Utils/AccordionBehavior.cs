using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Interactivity;

namespace Synapse.GUI.Utils;

/// <summary>
/// Attached behavior that enforces accordion semantics on an <see cref="ItemsControl"/>:
/// only one <see cref="Expander"/> child can be open at a time.
/// Usage in XAML: <code>utils:AccordionBehavior.IsAccordion="True"</code>
/// </summary>
public static class AccordionBehavior
{
    public static readonly AttachedProperty<bool> IsAccordionProperty =
        AvaloniaProperty.RegisterAttached<ItemsControl, bool>("IsAccordion", typeof(AccordionBehavior), false);

    public static bool GetIsAccordion(ItemsControl control) => control.GetValue(IsAccordionProperty);
    public static void SetIsAccordion(ItemsControl control, bool value) => control.SetValue(IsAccordionProperty, value);

    static AccordionBehavior()
    {
        IsAccordionProperty.Changed.AddClassHandler<ItemsControl>(OnIsAccordionChanged);
    }

    private static void OnIsAccordionChanged(ItemsControl control, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.NewValue is true)
        {
            control.AddHandler(Expander.ExpandedEvent, OnExpanderExpanded, RoutingStrategies.Bubble);
        }
        else
        {
            control.RemoveHandler(Expander.ExpandedEvent, OnExpanderExpanded);
        }
    }

    private static void OnExpanderExpanded(object? sender, RoutedEventArgs e)
    {
        if (sender is not ItemsControl itemsControl || e.Source is not Expander expandedExpander)
            return;

        // Collapse all other expanders within this ItemsControl
        foreach (var container in itemsControl.GetRealizedContainers())
        {
            var expander = FindChildExpander(container);
            if (expander is not null && expander != expandedExpander)
                expander.IsExpanded = false;
        }
    }

    private static Expander? FindChildExpander(Control control)
    {
        if (control is Expander exp) return exp;
        
        if (control is ContentPresenter { Child: { } child })
            return FindChildExpander(child);

        if (control is Panel panel)
        {
            foreach (var c in panel.Children)
            {
                var found = FindChildExpander(c);
                if (found is not null) return found;
            }
        }
        
        if (control is ContentControl { Content: Expander innerExp })
            return innerExp;

        return null;
    }
}


