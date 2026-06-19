using Avalonia;
using Avalonia.Controls;

namespace Synapse.GUI.Views.Templates;

public class Card : ContentControl
{
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<Card, string?>(nameof(Title));

    public static readonly StyledProperty<string?> SubtitleProperty =
        AvaloniaProperty.Register<Card, string?>(nameof(Subtitle));
    
    public static readonly StyledProperty<object?> HeaderRightProperty =
        AvaloniaProperty.Register<Card, object?>(nameof(HeaderRight));
    
    public static readonly StyledProperty<bool?> SeparatorVisibleProperty =
        AvaloniaProperty.Register<Card, bool?>(nameof(SeparatorVisible), true);
    
    public static readonly StyledProperty<double?> HeaderSpacingProperty =
        AvaloniaProperty.Register<Card, double?>(nameof(HeaderSpacing), 8);
    
    public static readonly StyledProperty<double?> TitleFontSizeProperty =
            AvaloniaProperty.Register<Card, double?>(nameof(TitleFontSize), 16);
    
    public static readonly StyledProperty<double?> SubtitleFontSizeProperty =
            AvaloniaProperty.Register<Card, double?>(nameof(SubtitleFontSize), 12);
    
    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string? Subtitle
    {
        get => GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }

    public object? HeaderRight
    {
        get => GetValue(HeaderRightProperty);
        set => SetValue(HeaderRightProperty, value);
    }
    
    public bool? SeparatorVisible
    {
        get => GetValue(SeparatorVisibleProperty);
        set => SetValue(SeparatorVisibleProperty, value);
    }
    
    public double? HeaderSpacing
    {
        get => GetValue(HeaderSpacingProperty);
        set => SetValue(HeaderSpacingProperty, value);
    }
    
    public double? TitleFontSize
    {
        get => GetValue(TitleFontSizeProperty);
        set => SetValue(TitleFontSizeProperty, value);
    }
    
    public double? SubtitleFontSize
    {
        get => GetValue(SubtitleFontSizeProperty);
        set => SetValue(SubtitleFontSizeProperty, value);
    }
}