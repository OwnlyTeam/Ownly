using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Ownly.Core.Uninstall;

namespace Ownly.Views.Pages;

public sealed partial class UninstallPage : Page
{
    private IReadOnlyList<InstalledApp> _all = Array.Empty<InstalledApp>();

    public UninstallPage()
    {
        InitializeComponent();
    }

    private void Back_Click(object sender, RoutedEventArgs e)
    {
        if (Frame.CanGoBack)
        {
            Frame.GoBack();
        }
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e) => await LoadAsync();

    private async void Refresh_Click(object sender, RoutedEventArgs e) => await LoadAsync();

    private async Task LoadAsync()
    {
        RefreshButton.IsEnabled = false;
        StatusText.Text = "Reading installed programs...";
        AppsPanel.Children.Clear();
        AppsPanel.Children.Add(new ProgressRing { IsActive = true, Width = 28, Height = 28 });
        FeedbackText.Text = "";

        _all = await Task.Run(() => new InstalledAppsService().Scan());
        Render(SearchBox.Text);
        RefreshButton.IsEnabled = true;
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => Render(SearchBox.Text);

    private void Render(string? filter)
    {
        var items = string.IsNullOrWhiteSpace(filter)
            ? _all
            : _all.Where(a => a.DisplayName.Contains(filter, StringComparison.OrdinalIgnoreCase)
                            || a.Publisher.Contains(filter, StringComparison.OrdinalIgnoreCase)).ToList();

        StatusText.Text = _all.Count == 0
            ? "No installed programs were found in the registry."
            : $"{items.Count} of {_all.Count} program{(_all.Count == 1 ? "" : "s")} shown.";

        AppsPanel.Children.Clear();
        if (items.Count == 0)
        {
            AppsPanel.Children.Add(new TextBlock
            {
                Text = "Nothing matches that search.",
                FontSize = 15,
                Foreground = Brush("OwnlyMutedTextBrush")
            });
            return;
        }

        foreach (var app in items)
        {
            AppsPanel.Children.Add(CreateCard(app));
        }
    }

    private static Brush Brush(string key) => (Brush)Application.Current.Resources[key];

    private Border CreateCard(InstalledApp app)
    {
        var card = new Border { Style = (Style)Application.Current.Resources["OwnlyCardStyle"], Padding = new Thickness(20) };
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var text = new StackPanel { Spacing = 4, VerticalAlignment = VerticalAlignment.Center };
        text.Children.Add(new TextBlock { Text = app.DisplayName, FontSize = 16, FontWeight = FontWeights.SemiBold, Foreground = Brush("OwnlyTextBrush") });
        var metaParts = new List<string> { app.Publisher };
        if (!string.IsNullOrWhiteSpace(app.Version)) metaParts.Add("v" + app.Version);
        if (app.EstimatedSizeKb > 0) metaParts.Add($"{app.EstimatedSizeKb / 1024d:0.#} MB");
        text.Children.Add(new TextBlock { Text = string.Join(" · ", metaParts), FontSize = 12, Foreground = Brush("OwnlyMutedTextBrush") });
        if (app.IsProtected)
        {
            text.Children.Add(new TextBlock { Text = "PROTECTED — " + app.ProtectionReason, FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = Brush("OwnlyFaintTextBrush"), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 4, 0, 0) });
        }
        else if (!app.CanUninstall)
        {
            text.Children.Add(new TextBlock { Text = "No uninstall command found for this program.", FontSize = 11, Foreground = Brush("OwnlyFaintTextBrush"), Margin = new Thickness(0, 4, 0, 0) });
        }
        Grid.SetColumn(text, 0);
        grid.Children.Add(text);

        var button = new Button { Content = "UNINSTALL", IsEnabled = app.CanUninstall, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(16, 0, 0, 0) };
        button.Click += async (_, _) => await UninstallClicked(app, button, card);
        Grid.SetColumn(button, 1);
        grid.Children.Add(button);

        card.Child = grid;
        return card;
    }

    private async Task UninstallClicked(InstalledApp app, Button button, Border card)
    {
        var confirm = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = "Uninstall " + app.DisplayName + "?",
            Content = "Ownly will start this program's own uninstaller. This is not reversible from Ownly — some uninstallers show their own window; finish there if one appears.",
            PrimaryButtonText = "UNINSTALL",
            CloseButtonText = "CANCEL",
            DefaultButton = ContentDialogButton.Close
        };
        if (await confirm.ShowAsync() != ContentDialogResult.Primary)
        {
            return;
        }

        button.IsEnabled = false;
        button.Content = "WORKING…";
        var service = new InstalledAppsService();
        var result = await Task.Run(() => service.Uninstall(app));
        FeedbackText.Text = result.Message;

        if (!result.Success)
        {
            button.Content = "TRY AGAIN";
            button.IsEnabled = true;
            return;
        }

        button.Content = "DONE";
        AppendLeftoverPanel(card, app, service);
    }

    private void AppendLeftoverPanel(Border card, InstalledApp app, InstalledAppsService service)
    {
        var stillRegistered = InstalledAppsService.StillRegistered(app);
        var folderExists = InstalledAppsService.InstallFolderStillExists(app);
        if (!stillRegistered && !folderExists)
        {
            return;
        }

        var panel = new StackPanel { Spacing = 8, Margin = new Thickness(0, 14, 0, 0) };
        panel.Children.Add(new TextBlock
        {
            Text = "Left behind:",
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            Foreground = Brush("OwnlyMutedTextBrush")
        });

        if (stillRegistered)
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
            row.Children.Add(new TextBlock { Text = "Registry entry still present", FontSize = 13, Foreground = Brush("OwnlyTextBrush"), VerticalAlignment = VerticalAlignment.Center });
            var removeBtn = new Button { Content = "REMOVE ENTRY" };
            removeBtn.Click += (_, _) =>
            {
                var r = service.RemoveRegistryTrace(app);
                FeedbackText.Text = r.Message;
                if (r.Success)
                {
                    removeBtn.IsEnabled = false;
                    removeBtn.Content = "REMOVED";
                }
            };
            row.Children.Add(removeBtn);
            panel.Children.Add(row);
        }

        if (folderExists)
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
            row.Children.Add(new TextBlock { Text = "Install folder still present: " + app.InstallLocation, FontSize = 13, Foreground = Brush("OwnlyTextBrush"), TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center });
            var openBtn = new Button { Content = "OPEN FOLDER" };
            openBtn.Click += (_, _) => service.OpenInstallFolder(app);
            row.Children.Add(openBtn);
            panel.Children.Add(row);
        }

        if (card.Child is Grid header)
        {
            card.Child = null;
            var wrapper = new StackPanel();
            wrapper.Children.Add(header);
            wrapper.Children.Add(panel);
            card.Child = wrapper;
        }
    }
}
