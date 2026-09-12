using System;
using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Ownly.Core.App;

namespace Ownly.Views.Pages;

public sealed partial class SettingsPage : Page
{
    private string? _downloadUrl;

    public SettingsPage()
    {
        InitializeComponent();
        UpdateVersionText.Text = $"You're on Ownly {UpdateService.CurrentVersion}.";
    }

    private async void CheckUpdate_Click(object sender, RoutedEventArgs e)
    {
        CheckUpdateButton.IsEnabled = false;
        CheckUpdateButton.Content = "CHECKING…";
        DownloadUpdateButton.Visibility = Visibility.Collapsed;
        UpdateStatusText.Text = "";

        var result = await UpdateService.CheckAsync();
        UpdateStatusText.Text = result.Message;

        if (result.HasUpdate)
        {
            _downloadUrl = result.DownloadUrl;
            DownloadUpdateButton.Visibility = Visibility.Visible;
        }

        CheckUpdateButton.Content = "CHECK FOR UPDATES";
        CheckUpdateButton.IsEnabled = true;
    }

    private void DownloadUpdate_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo(_downloadUrl ?? UpdateService.DownloadUrl) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            UpdateStatusText.Text = "Ownly could not open the download: " + ex.Message;
        }
    }
}
