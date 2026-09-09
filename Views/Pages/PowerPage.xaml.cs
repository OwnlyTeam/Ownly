using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Ownly.Core.Optimize;
using Ownly.Core.Safety;

namespace Ownly.Views.Pages;

public sealed partial class PowerPage : Page
{
    public PowerPage()
    {
        InitializeComponent();
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        ModesPanel.Children.Clear();
        var service = new PowerModeService();
        var active = service.ReadActive();
        ActiveText.Text = string.IsNullOrWhiteSpace(active.Guid)
            ? "Active power mode could not be read."
            : $"ACTIVE NOW · {active.Name}";

        foreach (var mode in PowerModeService.Modes)
        {
            ModesPanel.Children.Add(CreateModeCard(mode, active.Guid));
        }
    }

    private Border CreateModeCard(PowerModeDefinition mode, string activeGuid)
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
        var icon = new FontIcon { Glyph = "\uE7E8", FontSize = 22, Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["OwnlyTextBrush"], Margin = new Thickness(0, 0, 18, 0) };
        Grid.SetColumn(icon, 0);
        grid.Children.Add(icon);
        var text = new StackPanel { Spacing = 6 };
        text.Children.Add(new TextBlock { Text = mode.Name, FontSize = 17, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["OwnlyTextBrush"] });
        text.Children.Add(new TextBlock { Text = mode.Description, FontSize = 14, TextWrapping = TextWrapping.Wrap, Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["OwnlyMutedTextBrush"] });
        Grid.SetColumn(text, 1);
        grid.Children.Add(text);
        var button = new Button
        {
            Content = string.Equals(activeGuid, mode.Guid, System.StringComparison.OrdinalIgnoreCase) ? "ACTIVE" : "APPLY",
            Tag = mode,
            IsEnabled = !string.Equals(activeGuid, mode.Guid, System.StringComparison.OrdinalIgnoreCase),
            Padding = new Thickness(14, 8, 14, 8),
            VerticalAlignment = VerticalAlignment.Center
        };
        button.Click += ModeButton_Click;
        Grid.SetColumn(button, 2);
        grid.Children.Add(button);
        card.Child = grid;
        return card;
    }

    private void ModeButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not PowerModeDefinition mode)
        {
            return;
        }

        var result = new SafetyEngine().ApplyPowerMode(mode);
        button.Content = result.Success ? "ACTIVE" : "TRY AGAIN";
        button.IsEnabled = !result.Success;
        FeedbackText.Text = result.Message;
        if (result.Success)
        {
            ActiveText.Text = $"ACTIVE NOW · {mode.Name}";
        }
    }
}
