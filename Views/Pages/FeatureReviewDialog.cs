using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Ownly.Core.Features;
using System;
using System.Threading.Tasks;

namespace Ownly.Views.Pages;

public static class FeatureReviewDialog
{
    public static async Task ShowAsync(XamlRoot xamlRoot, OwnlyFeature feature)
    {
        var content = new StackPanel { Spacing = 12, MaxWidth = 560 };
        content.Children.Add(new TextBlock
        {
            Text = feature.Description,
            TextWrapping = TextWrapping.Wrap,
            FontSize = 14,
            Foreground = GetBrush("OwnlyMutedTextBrush")
        });
        content.Children.Add(new TextBlock
        {
            Text = feature.Detail,
            TextWrapping = TextWrapping.Wrap,
            FontSize = 14,
            Foreground = GetBrush("OwnlyTextBrush")
        });
        content.Children.Add(BuildMetaText(feature));
        content.Children.Add(new TextBlock
        {
            Text = GetReviewNote(feature),
            TextWrapping = TextWrapping.Wrap,
            FontSize = 13,
            Foreground = GetBrush("OwnlyMutedTextBrush")
        });

        var dialog = new ContentDialog
        {
            XamlRoot = xamlRoot,
            Title = feature.Name,
            Content = content,
            CloseButtonText = "CLOSE"
        };

        await dialog.ShowAsync();
    }

    private static TextBlock BuildMetaText(OwnlyFeature feature)
    {
        var state = feature.Availability.Equals("Available", StringComparison.OrdinalIgnoreCase)
            ? "Available"
            : "Review only";
        var admin = feature.RequiresAdmin ? "Requires admin" : "No admin needed";
        var restore = feature.Reversible ? "Restorable design" : "Not reversible";
        return new TextBlock
        {
            Text = $"{feature.Risk} risk | {state} | {admin} | {restore} | {feature.Origin}",
            TextWrapping = TextWrapping.Wrap,
            FontSize = 12,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = GetBrush("OwnlyTextBrush")
        };
    }

    private static string GetReviewNote(OwnlyFeature feature)
    {
        if (feature.Availability.Equals("Available", StringComparison.OrdinalIgnoreCase))
        {
            return "This module is connected to a safe Ownly action or page. Changes still require a button press and are logged for restore when supported.";
        }

        return "This module is wired into Ownly as a review-only option. It shows the idea and risk clearly, but it will not change Windows until a restore-aware action is built.";
    }

    private static Brush GetBrush(string resourceKey) =>
        (Brush)Application.Current.Resources[resourceKey];
}
