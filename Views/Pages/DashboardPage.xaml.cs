using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
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

    private void SectionCard_Tapped(object sender, TappedRoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: string tag })
        {
            MainWindow.Instance?.NavigateToSection(tag);
        }
    }

    private void SectionCard_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        if (sender is Border border)
        {
            border.Background = (Brush)Application.Current.Resources["OwnlySurfaceHoverBrush"];
        }
    }

    private void SectionCard_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (sender is Border border)
        {
            border.Background = (Brush)Application.Current.Resources["OwnlySurfaceBrush"];
        }
    }
}
