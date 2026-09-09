using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Ownly.Views.Pages;
using System;

namespace Ownly
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);
            StyleCaptionButtons();

            ContentFrame.Navigate(typeof(DashboardPage), null, new SuppressNavigationTransitionInfo());

            Activated += OnFirstActivated;
        }

        private void OnFirstActivated(object sender, WindowActivatedEventArgs e)
        {
            Activated -= OnFirstActivated;

            var visual = Microsoft.UI.Xaml.Hosting.ElementCompositionPreview.GetElementVisual(RootGrid);
            var compositor = visual.Compositor;
            Microsoft.UI.Xaml.Hosting.ElementCompositionPreview.SetIsTranslationEnabled(RootGrid, true);
            visual.Opacity = 0f;
            visual.Properties.InsertVector3("Translation", new System.Numerics.Vector3(0, 10, 0));

            var fade = compositor.CreateScalarKeyFrameAnimation();
            fade.InsertKeyFrame(1f, 1f);
            fade.Duration = TimeSpan.FromMilliseconds(360);
            visual.StartAnimation("Opacity", fade);

            var move = compositor.CreateVector3KeyFrameAnimation();
            move.InsertKeyFrame(1f, new System.Numerics.Vector3(0, 0, 0));
            move.Duration = TimeSpan.FromMilliseconds(420);
            visual.StartAnimation("Translation", move);
        }

        private void StyleCaptionButtons()
        {
            try
            {
                var bar = AppWindow.TitleBar;
                bar.ButtonBackgroundColor = Microsoft.UI.Colors.Transparent;
                bar.ButtonInactiveBackgroundColor = Microsoft.UI.Colors.Transparent;
                bar.ButtonForegroundColor = Microsoft.UI.Colors.White;
                bar.ButtonInactiveForegroundColor = Microsoft.UI.ColorHelper.FromArgb(255, 0x8B, 0x8B, 0x8F);
                bar.ButtonHoverBackgroundColor = Microsoft.UI.ColorHelper.FromArgb(255, 0x1E, 0x1E, 0x20);
                bar.ButtonHoverForegroundColor = Microsoft.UI.Colors.White;
                bar.ButtonPressedBackgroundColor = Microsoft.UI.ColorHelper.FromArgb(255, 0x2A, 0x2A, 0x2D);
                bar.ButtonPressedForegroundColor = Microsoft.UI.Colors.White;
            }
            catch
            {
                // Older title bar API surface; the window still works.
            }
        }

        private void AppNavigation_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.IsSettingsSelected)
            {
                NavigateTo(typeof(SettingsPage), "settings");
                return;
            }

            if (args.SelectedItemContainer?.Tag is string tag)
            {
                NavigateTo(tag == "overview" ? typeof(DashboardPage) : typeof(SectionPage), tag);
            }
        }

        private void NavigateTo(Type pageType, string tag)
        {
            var info = tag == "overview"
                ? (NavigationTransitionInfo)new EntranceNavigationTransitionInfo()
                : new SlideNavigationTransitionInfo { Effect = SlideNavigationTransitionEffect.FromRight };
            ContentFrame.Navigate(pageType, tag, info);
        }
    }
}
