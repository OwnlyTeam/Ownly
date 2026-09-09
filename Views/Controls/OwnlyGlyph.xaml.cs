using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Ownly.Views.Controls;

/// <summary>
/// The Ownly mark: a broken rounded-square ring (the "O", and a nod to a window) with the
/// "Ownly." dot. Pure vector, monochrome, recolours via <see cref="GlyphBrush"/>.
/// </summary>
public sealed partial class OwnlyGlyph : UserControl
{
    public static readonly DependencyProperty GlyphBrushProperty = DependencyProperty.Register(
        nameof(GlyphBrush), typeof(Brush), typeof(OwnlyGlyph),
        new PropertyMetadata(null, OnGlyphBrushChanged));

    public OwnlyGlyph()
    {
        InitializeComponent();
        Apply();
    }

    public Brush? GlyphBrush
    {
        get => (Brush?)GetValue(GlyphBrushProperty);
        set => SetValue(GlyphBrushProperty, value);
    }

    private static void OnGlyphBrushChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((OwnlyGlyph)d).Apply();

    private void Apply()
    {
        var brush = GlyphBrush
            ?? (Application.Current.Resources.TryGetValue("OwnlyTextBrush", out var b) ? (Brush)b : new SolidColorBrush(Colors.White));
        RingPath.Stroke = brush;
    }
}
