using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Ownly.Core.App;

namespace Ownly.Views;

public sealed partial class DisclaimerWindow : Window
{
    private readonly AcceptanceService _acceptance = new();
    private bool _scrolledToEnd;
    private bool _canEvaluate;

    /// <summary>Raised once the user has accepted. The host opens the main window in response.</summary>
    public event EventHandler? Accepted;

    public DisclaimerWindow()
    {
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        try
        {
            var bar = AppWindow.TitleBar;
            bar.ButtonBackgroundColor = Microsoft.UI.Colors.Transparent;
            bar.ButtonInactiveBackgroundColor = Microsoft.UI.Colors.Transparent;
            bar.ButtonForegroundColor = Microsoft.UI.Colors.White;
            bar.ButtonHoverBackgroundColor = Microsoft.UI.ColorHelper.FromArgb(255, 0x1E, 0x1E, 0x20);
            bar.ButtonHoverForegroundColor = Microsoft.UI.Colors.White;
        }
        catch
        {
            // non-fatal
        }

        TitleText.Text = LegalText.Title;
        IntroText.Text = LegalText.Intro;
        BodyText.Text = LegalText.Body;
        AgreeCheckLabel.Text = LegalText.AgreeCheckbox;

        Activated += OnFirstActivated;
        LegalScroller.SizeChanged += (_, _) => EvaluateScrollGate();
    }

    private void OnFirstActivated(object sender, WindowActivatedEventArgs e)
    {
        Activated -= OnFirstActivated;
        try
        {
            AppWindow.Resize(new Windows.Graphics.SizeInt32(900, 840));
            if (AppWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter presenter)
            {
                presenter.IsMaximizable = false;
            }
        }
        catch
        {
            // Non-fatal: the window still works at its default size.
        }

        // Let layout settle before the scroll gate can unlock, so a mid-layout measurement
        // never enables the checkbox before the user has actually scrolled.
        DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
        {
            _canEvaluate = true;
            EvaluateScrollGate();
        });
    }

    private void LegalScroller_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e) => EvaluateScrollGate();

    private void EvaluateScrollGate()
    {
        if (_scrolledToEnd || !_canEvaluate)
        {
            return;
        }

        var scrollable = LegalScroller.ScrollableHeight;
        var atBottom = scrollable > 0 && (scrollable - LegalScroller.VerticalOffset) <= 28;
        var trulyFits = scrollable <= 2 && LegalScroller.ViewportHeight > 150;

        if (atBottom || trulyFits)
        {
            _scrolledToEnd = true;
            AgreeCheck.IsEnabled = true;
            ScrollHintText.Text = "YOU HAVE REACHED THE END — CHECK THE BOX TO CONTINUE";
            ScrollHintText.Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["OwnlyMutedTextBrush"];
        }
    }

    private void AgreeCheck_Toggled(object sender, RoutedEventArgs e)
    {
        AgreeButton.IsEnabled = _scrolledToEnd && AgreeCheck.IsChecked == true;
    }

    private void AgreeButton_Click(object sender, RoutedEventArgs e)
    {
        _acceptance.RecordAcceptance();
        Accepted?.Invoke(this, EventArgs.Empty);
        Close();
    }

    private void DeclineButton_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Exit();
    }
}
