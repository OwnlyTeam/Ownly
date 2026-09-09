using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Ownly.Core.Safety;
using Ownly.Core.Scanning;
using System.Threading.Tasks;

namespace Ownly.Views.Pages;

public sealed partial class StartupPage : Page
{
    public StartupPage()
    {
        InitializeComponent();
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        StartupItemsPanel.Children.Clear();
        var entries = (await Task.Run(() => new ReadOnlyScanner().Scan())).StartupEntries;
        StatusText.Text = entries.Count == 0
            ? "No personal Startup entries were found."
            : $"{entries.Count} personal Startup entr{(entries.Count == 1 ? "y" : "ies")} found.";

        if (entries.Count == 0)
        {
            StartupItemsPanel.Children.Add(new TextBlock
            {
                Text = "Nothing needs review in your personal Startup folder.",
                FontSize = 16,
                Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["OwnlyMutedTextBrush"]
            });
            return;
        }

        foreach (var entry in entries)
        {
            StartupItemsPanel.Children.Add(CreateEntryCard(entry));
        }
    }

    private Border CreateEntryCard(StartupEntry entry)
    {
        var card = new Border
        {
            Style = (Style)Application.Current.Resources["OwnlyCardStyle"],
            Padding = new Thickness(22)
        };
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var icon = new FontIcon { Glyph = "\uE81C", FontSize = 22, Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["OwnlyTextBrush"], Margin = new Thickness(0, 0, 18, 0) };
        Grid.SetColumn(icon, 0);
        grid.Children.Add(icon);
        var content = new StackPanel { Spacing = 6 };
        content.Children.Add(new TextBlock { Text = entry.Name, FontSize = 17, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["OwnlyTextBrush"] });
        content.Children.Add(new TextBlock { Text = entry.FullPath, FontSize = 12, TextTrimming = TextTrimming.CharacterEllipsis, Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["OwnlyMutedTextBrush"] });
        Grid.SetColumn(content, 1);
        grid.Children.Add(content);
        var disableButton = new Button { Content = "DISABLE STARTUP", Tag = entry, Padding = new Thickness(14, 8, 14, 8), HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center };
        disableButton.Click += DisableButton_Click;
        Grid.SetColumn(disableButton, 2);
        grid.Children.Add(disableButton);
        card.Child = grid;
        return card;
    }

    private void DisableButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not StartupEntry entry)
        {
            return;
        }

        var result = new SafetyEngine().DisableStartupItem(entry.FullPath, entry.Name);
        button.Content = result.Success ? "DISABLED" : "TRY AGAIN";
        button.IsEnabled = !result.Success;
        FeedbackText.Text = result.Message;
    }
}
