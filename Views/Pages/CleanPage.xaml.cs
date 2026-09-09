using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Ownly.Core.Scanning;
using Ownly.Core.Safety;
using System.Threading.Tasks;

namespace Ownly.Views.Pages;

public sealed partial class CleanPage : Page
{
    public CleanPage()
    {
        InitializeComponent();
    }

    private async void ScanButton_Click(object sender, RoutedEventArgs e)
    {
        ScanButton.IsEnabled = false;
        ScanButton.Content = "SCANNING...";
        StatusText.Text = "Looking around your PC.";
        SummaryText.Text = "Reading safe, local information...";

        var report = await Task.Run(() => new ReadOnlyScanner().Scan());
        ShowReport(report);
        ScanButton.IsEnabled = true;
        ScanButton.Content = "SCAN AGAIN";
    }

    private void SuggestionsButton_Click(object sender, RoutedEventArgs e)
    {
        var result = new SafetyEngine().ApplyDisableWindowsSuggestions();
        SuggestionsButton.Content = result.Success ? "APPLIED" : "TRY AGAIN";
        SuggestionsButton.IsEnabled = !result.Success;
        SuggestionsStatus.Text = result.Success ? "APPLIED · RESTORABLE" : "OPTIONAL · REVERSIBLE";
    }

    private void BackgroundButton_Click(object sender, RoutedEventArgs e)
    {
        var result = new SafetyEngine().ApplyLimitBackgroundApps();
        BackgroundButton.Content = result.Success ? "APPLIED" : "TRY AGAIN";
        BackgroundButton.IsEnabled = !result.Success;
        BackgroundStatus.Text = result.Success ? "APPLIED · RESTORABLE" : "OPTIONAL · MODERATE";
    }

    private void TempButton_Click(object sender, RoutedEventArgs e)
    {
        TempButton.IsEnabled = false;
        TempButton.Content = "CLEANING...";
        TempStatus.Text = "MOVING TO BACKUP...";

        var result = new SafetyEngine().QuarantineTemporaryFiles();
        TempButton.Content = result.Success ? "APPLIED" : "TRY AGAIN";
        TempButton.IsEnabled = !result.Success;
        TempStatus.Text = result.Success ? "APPLIED · RESTORABLE" : "OPTIONAL · RESTORABLE";
        SummaryText.Text = result.Message;
    }

    private void ShowReport(ScanReport report)
    {
        ResultsPanel.Children.Clear();
        ResultCountText.Text = $"{report.Recommendations.Count} FOUND";
        StatusText.Text = report.Recommendations.Count == 1 && report.Recommendations[0].Category == "READY"
            ? "Your PC looks clear for now."
            : $"Ownly found {report.Recommendations.Count} recommendation{(report.Recommendations.Count == 1 ? "" : "s")}.";
        SummaryText.Text = $"{report.BloatwareCandidates.Count} optional app{(report.BloatwareCandidates.Count == 1 ? "" : "s")} · {report.TemporaryBytes / 1024d / 1024d / 1024d:0.0} GB temp files · {report.StartupItems} startup item{(report.StartupItems == 1 ? "" : "s")}";
        TempStatus.Text = report.TemporaryBytes > 0 ? $"{report.TemporaryBytes / 1024d / 1024d:0} MB · RESTORABLE" : "ALREADY CLEAR · RESTORABLE";
        TempButton.Content = report.TemporaryBytes > 0 ? "CLEAN TEMP FILES" : "ALREADY CLEAR";
        TempButton.IsEnabled = report.TemporaryBytes > 0;

        foreach (var recommendation in report.Recommendations)
        {
            var card = new Border
            {
                Style = (Style)Application.Current.Resources["OwnlyCardStyle"],
                Padding = new Thickness(22)
            };
            var content = new StackPanel { Spacing = 7 };
            content.Children.Add(new TextBlock
            {
                Text = recommendation.Title,
                FontSize = 17,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["OwnlyTextBrush"]
            });
            content.Children.Add(new TextBlock
            {
                Text = recommendation.Description,
                Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["OwnlyMutedTextBrush"]
            });
            content.Children.Add(new TextBlock
            {
                Text = recommendation.Detail,
                Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["OwnlyTextBrush"],
                Margin = new Thickness(0, 5, 0, 0)
            });
            card.Child = content;
            ResultsPanel.Children.Add(card);
        }

        if (report.BloatwareCandidates.Count > 0)
        {
            ResultsPanel.Children.Add(new TextBlock
            {
                Text = "OPTIONAL APP REVIEW",
                FontSize = 12,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                CharacterSpacing = 110,
                Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["OwnlyMutedTextBrush"],
                Margin = new Thickness(0, 18, 0, 2)
            });

            foreach (var candidate in report.BloatwareCandidates)
            {
                ResultsPanel.Children.Add(CreateCandidateCard(candidate));
            }
        }
    }

    private static Border CreateCandidateCard(BloatwareCandidate candidate)
    {
        var card = new Border
        {
            Style = (Style)Application.Current.Resources["OwnlyCardStyle"],
            Padding = new Thickness(22)
        };
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var content = new StackPanel { Spacing = 7 };
        content.Children.Add(new TextBlock
        {
            Text = candidate.DisplayName,
            FontSize = 17,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["OwnlyTextBrush"]
        });
        content.Children.Add(new TextBlock
        {
            Text = ExplainCandidate(candidate.DisplayName),
            TextWrapping = TextWrapping.Wrap,
            Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["OwnlyMutedTextBrush"]
        });
        content.Children.Add(new TextBlock
        {
            Text = $"{candidate.Source} · {candidate.Publisher}",
            FontSize = 12,
            Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["OwnlyTextBrush"]
        });
        if (candidate.IsProtected)
        {
            content.Children.Add(new TextBlock
            {
                Text = candidate.ProtectionReason,
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
                Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["OwnlyMutedTextBrush"]
            });
        }
        Grid.SetColumn(content, 0);
        grid.Children.Add(content);
        var status = new TextBlock
        {
            Text = candidate.IsProtected ? "PROTECTED" : "REVIEW ONLY",
            FontSize = 10,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            CharacterSpacing = 70,
            Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["OwnlyMutedTextBrush"],
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(status, 1);
        grid.Children.Add(status);
        card.Child = grid;
        return card;
    }

    private static string ExplainCandidate(string candidate)
    {
        if (candidate.Contains("Xbox", System.StringComparison.OrdinalIgnoreCase))
        {
            return "Microsoft gaming services and companion apps. Keep them if you use Xbox features or PC Game Pass.";
        }
        if (candidate.Contains("Clipchamp", System.StringComparison.OrdinalIgnoreCase))
        {
            return "Microsoft’s video editor. Optional for users who do not edit videos with Clipchamp.";
        }
        if (candidate.Contains("McAfee", System.StringComparison.OrdinalIgnoreCase) || candidate.Contains("Norton", System.StringComparison.OrdinalIgnoreCase))
        {
            return "Third-party security software. Review carefully before removing; Windows Security may already cover your needs.";
        }
        if (candidate.Contains("Candy", System.StringComparison.OrdinalIgnoreCase) || candidate.Contains("Solitaire", System.StringComparison.OrdinalIgnoreCase))
        {
            return "Optional entertainment software commonly included with consumer Windows installs.";
        }
        if (candidate.Contains("Spotify", System.StringComparison.OrdinalIgnoreCase) || candidate.Contains("TikTok", System.StringComparison.OrdinalIgnoreCase) || candidate.Contains("Facebook", System.StringComparison.OrdinalIgnoreCase))
        {
            return "A consumer app that may be optional depending on how you use this PC.";
        }
        return "An optional application candidate. Ownly will show its publisher, purpose, and dependencies before offering removal.";
    }
}
