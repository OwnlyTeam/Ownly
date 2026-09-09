using Microsoft.UI.Xaml.Controls;
using Ownly.Core.System;

namespace Ownly.Views.Pages;

public sealed partial class DashboardPage : Page
{
    public DashboardPage()
    {
        InitializeComponent();
        LoadSystemSnapshot();
    }

    private void LoadSystemSnapshot()
    {
        var snapshot = new SystemInfoService().ReadSnapshot();
        DeviceNameText.Text = snapshot.DeviceName;
        WindowsVersionText.Text = snapshot.WindowsVersion;
        ProcessorText.Text = snapshot.Processor;
        MemoryText.Text = snapshot.Memory;
        StorageText.Text = snapshot.Storage;
        StorageDetailText.Text = snapshot.StorageDetail;
    }
}
