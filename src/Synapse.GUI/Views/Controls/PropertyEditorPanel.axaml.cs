using System;
using System.Collections.Generic;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Synapse.Reflection.Models;

namespace Synapse.GUI.Views.Controls;

public enum PropertyEditorLayoutKind
{
    Stack,
    Wrap,
    UniformGrid
}

public partial class PropertyEditorPanel : UserControl
{
    // ===== Data Source =====
    public static readonly StyledProperty<IEnumerable<UserSettableProperty>?> PropertiesProperty =
        AvaloniaProperty.Register<PropertyEditorPanel, IEnumerable<UserSettableProperty>?>(nameof(Properties));
    public IEnumerable<UserSettableProperty>? Properties
    {
        get => GetValue(PropertiesProperty);
        set => SetValue(PropertiesProperty, value);
    }

    // ===== Layout-Choice =====
    public static readonly StyledProperty<PropertyEditorLayoutKind> LayoutKindProperty =
        AvaloniaProperty.Register<PropertyEditorPanel, PropertyEditorLayoutKind>(nameof(LayoutKind), PropertyEditorLayoutKind.Stack);
    public PropertyEditorLayoutKind LayoutKind
    {
        get => GetValue(LayoutKindProperty);
        set => SetValue(LayoutKindProperty, value);
    }

    // ===== Orientation (Stack/Wrap) =====
    public static readonly StyledProperty<Orientation> OrientationProperty =
        AvaloniaProperty.Register<PropertyEditorPanel, Orientation>(nameof(Orientation), Orientation.Vertical);
    public Orientation Orientation
    {
        get => GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    // ===== Spacing (only for StackPanel) =====
    public static readonly StyledProperty<double> SpacingProperty =
        AvaloniaProperty.Register<PropertyEditorPanel, double>(nameof(Spacing), 12d);
    public double Spacing
    {
        get => GetValue(SpacingProperty);
        set => SetValue(SpacingProperty, value);
    }

    // ===== Grid (UniformGrid) =====
    public static readonly StyledProperty<int> ColumnsProperty =
        AvaloniaProperty.Register<PropertyEditorPanel, int>(nameof(Columns), 3);
    public int Columns
    {
        get => GetValue(ColumnsProperty);
        set => SetValue(ColumnsProperty, value);
    }

    // ===== ItemMargin =====
    public static readonly StyledProperty<Thickness> ItemMarginProperty =
        AvaloniaProperty.Register<PropertyEditorPanel, Thickness>(nameof(ItemMargin), new Thickness(0));
    public Thickness ItemMargin
    {
        get => GetValue(ItemMarginProperty);
        set => SetValue(ItemMarginProperty, value);
    }

    // ===== Optionally set your own ItemTemplate (otherwise DefaultPropertyTemplateSelector) =====
    public static readonly StyledProperty<IDataTemplate?> ItemTemplateProperty =
        AvaloniaProperty.Register<PropertyEditorPanel, IDataTemplate?>(nameof(ItemTemplate), default);
    public IDataTemplate? ItemTemplate
    {
        get => GetValue(ItemTemplateProperty);
        set => SetValue(ItemTemplateProperty, value);
    }
    

    public static readonly StyledProperty<ICommand?> ValueChangedCommandProperty =
        AvaloniaProperty.Register<PropertyEditorPanel, ICommand?>(nameof(ValueChangedCommand));
    public ICommand? ValueChangedCommand
    {
        get => GetValue(ValueChangedCommandProperty);
        set => SetValue(ValueChangedCommandProperty, value);
    }

    private Grid? _root;
    
    public PropertyEditorPanel()
    {
        InitializeComponent();
        _root = this.FindControl<Grid>("Root");

        AttachedToVisualTree += (_, __) => Rebuild();
    }
    
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        var p = change.Property;

        if (p == PropertiesProperty)
        {
            UpdateItemsOnly();
        }
        else if (p == LayoutKindProperty
                 || p == OrientationProperty
                 || p == SpacingProperty
                 || p == ColumnsProperty
                 || p == ItemMarginProperty
                 || p == ItemTemplateProperty)
        {
            Rebuild();
        }
    }

    private IDataTemplate ResolveItemTemplate()
    {
        if (ItemTemplate is not null)
            return ItemTemplate;

        if (this.Resources["DefaultPropertyTemplateSelector"] is IDataTemplate t)
            return t;

        return new FuncDataTemplate<object>((item, _) =>
            new TextBlock { Text = item?.ToString() ?? "(null)" }, true);
    }

    private void Rebuild()
    {
        if (_root is null) return;
        _root.Children.Clear();

        _root.Children.Add(LayoutKind switch
        {
            PropertyEditorLayoutKind.Stack       => BuildItemsControlStack(),
            PropertyEditorLayoutKind.Wrap        => BuildItemsControlWrap(),
            PropertyEditorLayoutKind.UniformGrid => BuildItemsControlUniformGrid(),
            _ => BuildItemsControlStack()
        });
    }

    private void UpdateItemsOnly()
    {
        if (_root?.Children.Count != 1) return;
        if (_root.Children[0] is ItemsControl ic)
            ic.ItemsSource = Properties;
    }

    // ---------- Builder ----------
    private ItemsControl BuildItemsControlStack()
    {
        var panelFactory = new FuncTemplate<Panel>(() =>
        {
            var sp = new StackPanel { Orientation = Orientation, Spacing = Spacing };
            return sp;
        });

        return new ItemsControl
        {
            ItemsSource = Properties,
            ItemTemplate = WrapItemInMargin(ResolveItemTemplate()),
            ItemsPanel = panelFactory!
        };
    }

    private ItemsControl BuildItemsControlWrap()
    {
        var panelFactory = new FuncTemplate<Panel>(() =>
        {
            var wp = new WrapPanel { Orientation = Orientation };
            return wp;
        });

        return new ItemsControl
        {
            ItemsSource = Properties,
            ItemTemplate = WrapItemInMargin(ResolveItemTemplate()),
            ItemsPanel = panelFactory!
        };
    }

    private ItemsControl BuildItemsControlUniformGrid()
    {
        var panelFactory = new FuncTemplate<Panel>(() =>
            new UniformGrid { Columns = Math.Max(1, Columns) });

        return new ItemsControl
        {
            ItemsSource = Properties,
            ItemTemplate = WrapItemInMargin(ResolveItemTemplate()),
            ItemsPanel = panelFactory!
        };
    }
    
    private IDataTemplate WrapItemInMargin(IDataTemplate inner)
    {
        return new FuncDataTemplate<object>((data, _) =>
        {
            var built = inner.Build(data);
            if (ItemMargin == default)
                return built;

            return new Border
            {
                Margin = ItemMargin,
                Child = built
            };
        }, supportsRecycling: true);
    }
}