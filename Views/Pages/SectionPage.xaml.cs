using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Ownly.Core.Features;
using Ownly.Core.Safety;
using Ownly.Core.Scanning;
using Ownly.Core.System;

namespace Ownly.Views.Pages;

public sealed partial class SectionPage : Page
{
    private string _section = "overview";

    public SectionPage()
    {
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        _section = (e.Parameter as string ?? "overview").ToLowerInvariant();
        Render();
    }

    private void Render()
    {
        var content = SectionCatalog.Get(_section);
        EyebrowText.Text = content.Eyebrow;
        TitleText.Text = content.Title;
        DescriptionText.Text = content.Description;
        ContentStack.Children.Clear();
        FeedbackText.Text = string.Empty;

        if (_section == "changes")
        {
            RenderChanges();
            AnimateIn();
            return;
        }

        if (_section == "activity")
        {
            RenderActivity();
            AnimateIn();
            return;
        }

        if (content.ShowSystemSnapshot)
        {
            ContentStack.Children.Add(BuildSnapshotCard());
        }

        if (content.ShowScanner)
        {
            ContentStack.Children.Add(BuildScannerCard());
        }

        foreach (var group in content.Groups)
        {
            ContentStack.Children.Add(BuildHeading(group.Heading, group.Caption));
            foreach (var item in group.Items)
            {
                ContentStack.Children.Add(BuildItemCard(item));
            }
        }

        RenderTweaks();
        AnimateIn();
    }

    private void RenderTweaks()
    {
        var tweaks = Ownly.Core.Tweaks.TweakCatalog.ForSection(_section);
        if (tweaks.Count == 0)
        {
            return;
        }

        var runner = new Ownly.Core.Tweaks.TweakRunner();
        foreach (var group in tweaks.GroupBy(t => t.Group))
        {
            ContentStack.Children.Add(BuildHeading("More options · " + group.Key, null));
            foreach (var tweak in group)
            {
                ContentStack.Children.Add(BuildTweakCard(tweak, runner));
            }
        }
    }

    // ---------- shared visual helpers ----------

    private static Brush Brush(string key) => (Brush)Application.Current.Resources[key];

    private static Border Card() => new()
    {
        Style = (Style)Application.Current.Resources["OwnlyCardStyle"],
        Padding = new Thickness(20)
    };

    private static FrameworkElement BuildHeading(string heading, string? caption)
    {
        var panel = new StackPanel { Spacing = 4, Margin = new Thickness(2, 14, 0, 2) };
        panel.Children.Add(new TextBlock
        {
            Text = heading.ToUpperInvariant(),
            FontSize = 12,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            CharacterSpacing = 110,
            Foreground = Brush("OwnlyMutedTextBrush")
        });
        if (!string.IsNullOrWhiteSpace(caption))
        {
            panel.Children.Add(new TextBlock
            {
                Text = caption,
                FontSize = 13,
                TextWrapping = TextWrapping.Wrap,
                Foreground = Brush("OwnlyFaintTextBrush")
            });
        }
        return panel;
    }

    private static FontIcon Icon(string glyph) => new()
    {
        Glyph = glyph,
        FontSize = 20,
        Foreground = Brush("OwnlyAccentBrush"),
        Margin = new Thickness(0, 2, 18, 0),
        VerticalAlignment = VerticalAlignment.Top
    };

    private static string SectionGlyph(string section) => section switch
    {
        "clean" => "",
        "customize" => "",
        "optimize" => "",
        "privacy" => "",
        "tools" => "",
        _ => ""
    };

    private static TextBlock RiskChip(string risk) => new()
    {
        Text = risk.ToUpperInvariant() + " RISK",
        FontSize = 10,
        FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
        CharacterSpacing = 60,
        Foreground = Brush(risk.Equals("Low", StringComparison.OrdinalIgnoreCase) ? "OwnlyFaintTextBrush" : "OwnlyWarningBrush"),
        VerticalAlignment = VerticalAlignment.Center,
        Margin = new Thickness(0, 8, 0, 0)
    };

    // ---------- item cards ----------

    private FrameworkElement BuildItemCard(SectionItem item)
    {
        var card = Card();
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var icon = Icon(string.IsNullOrEmpty(item.Glyph) ? SectionGlyph(_section) : item.Glyph);
        Grid.SetColumn(icon, 0);
        grid.Children.Add(icon);

        var text = new StackPanel { Spacing = 5, VerticalAlignment = VerticalAlignment.Center };
        text.Children.Add(new TextBlock
        {
            Text = item.Title,
            FontSize = 16,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = Brush("OwnlyTextBrush")
        });
        text.Children.Add(new TextBlock
        {
            Text = item.Description,
            FontSize = 13,
            TextWrapping = TextWrapping.Wrap,
            Foreground = Brush("OwnlyMutedTextBrush")
        });
        if (item.Kind == SectionItemKind.Toggle || item.Risk.Equals("Moderate", StringComparison.OrdinalIgnoreCase))
        {
            text.Children.Add(RiskChip(item.Risk));
        }
        Grid.SetColumn(text, 1);
        grid.Children.Add(text);

        FrameworkElement control = item.Kind switch
        {
            SectionItemKind.Toggle => BuildToggle(item),
            SectionItemKind.Navigate => BuildNavButton(item),
            SectionItemKind.Action => BuildActionButton(item),
            _ => new TextBlock { Text = string.Empty }
        };
        control.VerticalAlignment = VerticalAlignment.Center;
        control.Margin = new Thickness(16, 0, 0, 0);
        Grid.SetColumn(control, 2);
        grid.Children.Add(control);

        card.Child = grid;
        return card;
    }

    private ToggleSwitch BuildToggle(SectionItem item)
    {
        var toggle = new ToggleSwitch
        {
            OnContent = "ON",
            OffContent = "OFF",
            IsOn = SectionActions.IsApplied(item.Id)
        };
        toggle.Toggled += (sender, _) =>
        {
            var t = (ToggleSwitch)sender;
            var applied = SectionActions.IsApplied(item.Id);
            if (t.IsOn == applied)
            {
                return; // state already matches; nothing to do
            }

            var result = t.IsOn ? SectionActions.Apply(item.Id) : SectionActions.Revert(item.Id);
            FeedbackText.Text = result.Message;
            if (!result.Success)
            {
                t.IsOn = applied; // roll the switch back to the true state
            }
        };
        return toggle;
    }

    private Button BuildNavButton(SectionItem item)
    {
        var button = new Button { Content = "OPEN" };
        button.Click += (_, _) =>
        {
            if (item.NavigateTarget == "power")
            {
                Frame.Navigate(typeof(PowerPage));
            }
            else if (item.NavigateTarget == "startup")
            {
                Frame.Navigate(typeof(StartupPage));
            }
            else if (item.NavigateTarget == "uninstall")
            {
                Frame.Navigate(typeof(UninstallPage));
            }
        };
        return button;
    }

    private Button BuildActionButton(SectionItem item)
    {
        var button = new Button { Content = item.ActionLabel ?? "RUN" };
        button.Click += async (_, _) =>
        {
            button.IsEnabled = false;
            if (item.Id == "tools.data-folder")
            {
                OpenDataFolder();
                button.IsEnabled = true;
                return;
            }

            if (item.Id == "clean.temp")
            {
                button.Content = "WORKING…";
                var result = await Task.Run(() => new SafetyEngine().QuarantineTemporaryFiles());
                FeedbackText.Text = result.Message;
                button.Content = result.Success ? "DONE" : "TRY AGAIN";
                button.IsEnabled = !result.Success;
                return;
            }

            button.IsEnabled = true;
        };
        return button;
    }

    private void OpenDataFolder()
    {
        try
        {
            var dir = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Ownly");
            System.IO.Directory.CreateDirectory(dir);
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(dir) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            FeedbackText.Text = "Ownly could not open its data folder: " + ex.Message;
        }
    }

    // ---------- system snapshot ----------

    private FrameworkElement BuildSnapshotCard()
    {
        var snapshot = new SystemInfoService().ReadSnapshot();
        var card = new Border
        {
            Style = (Style)Application.Current.Resources["OwnlyRaisedCardStyle"],
            Padding = new Thickness(22)
        };
        var wrap = new StackPanel { Spacing = 14 };
        wrap.Children.Add(new TextBlock
        {
            Text = "THIS DEVICE",
            FontSize = 11,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            CharacterSpacing = 100,
            Foreground = Brush("OwnlyMutedTextBrush")
        });
        var row = new Grid { ColumnSpacing = 26 };
        for (var i = 0; i < 4; i++)
        {
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        }
        AddSnapshotCell(row, 0, "PROCESSOR", snapshot.Processor);
        AddSnapshotCell(row, 1, "MEMORY", snapshot.Memory);
        AddSnapshotCell(row, 2, "WINDOWS", snapshot.WindowsVersion);
        AddSnapshotCell(row, 3, "SYSTEM DRIVE", snapshot.Storage);
        wrap.Children.Add(row);
        card.Child = wrap;
        return card;
    }

    private static void AddSnapshotCell(Grid row, int column, string label, string value)
    {
        var cell = new StackPanel { Spacing = 4 };
        cell.Children.Add(new TextBlock { Text = label, FontSize = 10, CharacterSpacing = 80, Foreground = Brush("OwnlyFaintTextBrush") });
        cell.Children.Add(new TextBlock
        {
            Text = value,
            FontSize = 14,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            Foreground = Brush("OwnlyTextBrush")
        });
        Grid.SetColumn(cell, column);
        row.Children.Add(cell);
    }

    // ---------- scanner ----------

    private FrameworkElement BuildScannerCard()
    {
        var card = new Border
        {
            Style = (Style)Application.Current.Resources["OwnlyRaisedCardStyle"],
            Padding = new Thickness(22)
        };
        var grid = new Grid { ColumnSpacing = 16 };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var text = new StackPanel { Spacing = 6, VerticalAlignment = VerticalAlignment.Center };
        var status = new TextBlock
        {
            Text = "Read-only scan",
            FontSize = 16,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = Brush("OwnlyTextBrush")
        };
        var detail = new TextBlock
        {
            Text = "Look at temp file size, personal startup items, and optional apps. Nothing is changed.",
            FontSize = 13,
            TextWrapping = TextWrapping.Wrap,
            Foreground = Brush("OwnlyMutedTextBrush")
        };
        text.Children.Add(status);
        text.Children.Add(detail);
        Grid.SetColumn(text, 0);
        grid.Children.Add(text);

        var button = new Button { Content = "SCAN THIS PC", Style = (Style)Application.Current.Resources["OwnlyPrimaryButtonStyle"], VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(button, 1);
        grid.Children.Add(button);

        var results = new StackPanel { Spacing = 12, Margin = new Thickness(0, 14, 0, 0) };

        button.Click += async (_, _) =>
        {
            button.IsEnabled = false;
            button.Content = "SCANNING…";
            results.Children.Clear();
            var report = await Task.Run(() => new ReadOnlyScanner().Scan());
            button.IsEnabled = true;
            button.Content = "SCAN AGAIN";
            status.Text = $"{report.Recommendations.Count} thing{(report.Recommendations.Count == 1 ? "" : "s")} to look at";
            detail.Text = $"{report.BloatwareCandidates.Count} optional app(s) · "
                          + $"{report.TemporaryBytes / 1024d / 1024d / 1024d:0.0} GB temp files · "
                          + $"{report.StartupItems} startup item(s)";

            foreach (var rec in report.Recommendations)
            {
                results.Children.Add(SimpleCard(rec.Title, rec.Description + "\n" + rec.Detail));
            }
            foreach (var candidate in report.BloatwareCandidates.Take(20))
            {
                results.Children.Add(SimpleCard(
                    candidate.DisplayName,
                    $"{candidate.Source} · {candidate.Publisher}"
                    + (candidate.IsProtected ? "\nProtected — Ownly will not offer to remove this." : "\nReview only — Ownly does not remove apps in this build.")));
            }
        };

        var outer = new StackPanel();
        outer.Children.Add(grid);
        outer.Children.Add(results);
        card.Child = outer;
        return card;
    }

    private static Border SimpleCard(string title, string body)
    {
        var c = Card();
        var s = new StackPanel { Spacing = 5 };
        s.Children.Add(new TextBlock { Text = title, FontSize = 15, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Foreground = Brush("OwnlyTextBrush") });
        s.Children.Add(new TextBlock { Text = body, FontSize = 13, TextWrapping = TextWrapping.Wrap, Foreground = Brush("OwnlyMutedTextBrush") });
        c.Child = s;
        return c;
    }

    // ---------- tweak cards ----------

    private FrameworkElement BuildTweakCard(Ownly.Core.Tweaks.Tweak tweak, Ownly.Core.Tweaks.TweakRunner runner)
    {
        var card = Card();
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var icon = Icon(SectionGlyph(_section));
        Grid.SetColumn(icon, 0);
        grid.Children.Add(icon);

        var text = new StackPanel { Spacing = 5, VerticalAlignment = VerticalAlignment.Center };
        text.Children.Add(new TextBlock { Text = tweak.Title, FontSize = 16, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Foreground = Brush("OwnlyTextBrush") });
        text.Children.Add(new TextBlock { Text = tweak.Description, FontSize = 13, TextWrapping = TextWrapping.Wrap, Foreground = Brush("OwnlyMutedTextBrush") });

        var chips = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, Margin = new Thickness(0, 6, 0, 0) };
        chips.Children.Add(Chip(tweak.Risk.ToUpperInvariant() + " RISK",
            tweak.Risk.Equals("Low", StringComparison.OrdinalIgnoreCase) ? "OwnlyFaintTextBrush"
            : tweak.Risk.Equals("High", StringComparison.OrdinalIgnoreCase) ? "OwnlyDangerBrush" : "OwnlyWarningBrush"));
        if (tweak.NeedsAdmin)
        {
            chips.Children.Add(Chip("NEEDS ADMIN", "OwnlyMutedTextBrush"));
        }
        text.Children.Add(chips);
        if (!string.IsNullOrWhiteSpace(tweak.Note))
        {
            text.Children.Add(new TextBlock { Text = tweak.Note, FontSize = 12, TextWrapping = TextWrapping.Wrap, Foreground = Brush("OwnlyFaintTextBrush"), Margin = new Thickness(0, 4, 0, 0) });
        }
        Grid.SetColumn(text, 1);
        grid.Children.Add(text);

        FrameworkElement control;
        if (tweak.Kind == Ownly.Core.Tweaks.TweakKind.Registry)
        {
            var toggle = new ToggleSwitch { OnContent = "ON", OffContent = "OFF", IsOn = runner.IsApplied(tweak) };
            toggle.Toggled += async (s, _) =>
            {
                var t = (ToggleSwitch)s;
                var applied = runner.IsApplied(tweak);
                if (t.IsOn == applied)
                {
                    return;
                }
                t.IsEnabled = false;
                var wantOn = t.IsOn;
                var result = await Task.Run(() => wantOn ? runner.Apply(tweak) : runner.RevertByTweak(tweak));
                FeedbackText.Text = result.Message;
                t.IsOn = runner.IsApplied(tweak);
                t.IsEnabled = true;
            };
            control = toggle;
        }
        else
        {
            var button = new Button { Content = ActionVerb(tweak) };
            button.Click += async (_, _) =>
            {
                button.IsEnabled = false;
                var original = button.Content;
                button.Content = "WORKING…";
                var result = await Task.Run(() => runner.Apply(tweak));
                FeedbackText.Text = result.Message;
                button.Content = original;
                button.IsEnabled = true;
            };
            control = button;
        }
        control.VerticalAlignment = VerticalAlignment.Center;
        control.Margin = new Thickness(16, 0, 0, 0);
        Grid.SetColumn(control, 2);
        grid.Children.Add(control);

        card.Child = grid;
        return card;
    }

    private static string ActionVerb(Ownly.Core.Tweaks.Tweak tweak) => tweak.Id switch
    {
        "tools.flush-dns" => "FLUSH",
        "tools.restart-explorer" => "RESTART",
        "tools.empty-recycle-bin" => "EMPTY",
        "tools.restore-point" => "CREATE",
        "tools.sfc" or "tools.dism" => "RUN",
        "tools.clear-update-cache" => "CLEAR",
        _ => "RUN"
    };

    private static Border Chip(string label, string brushKey) => new()
    {
        Background = Brush("OwnlySurfaceRaisedBrush"),
        CornerRadius = new CornerRadius(4),
        Padding = new Thickness(7, 3, 7, 3),
        Child = new TextBlock
        {
            Text = label,
            FontSize = 9,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            CharacterSpacing = 60,
            Foreground = Brush(brushKey)
        }
    };

    // ---------- activity ----------

    private void RenderActivity()
    {
        var entries = new Ownly.Core.Tweaks.ActivityLog().Read().Reverse().ToList();
        if (entries.Count == 0)
        {
            ContentStack.Children.Add(SimpleCard("Nothing yet", "This is a running record of everything Ownly does — what it changed, whether it needed administrator rights, and any output. It fills in as you use the app."));
            return;
        }

        var clear = new Button { Content = "CLEAR LOG", HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 0, 0, 6) };
        clear.Click += (_, _) => { new Ownly.Core.Tweaks.ActivityLog().Clear(); Render(); };
        ContentStack.Children.Add(clear);

        foreach (var e in entries)
        {
            var card = Card();
            var s = new StackPanel { Spacing = 6 };
            var head = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
            head.Children.Add(new TextBlock { Text = e.Success ? "OK" : "FAILED", FontSize = 10, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, CharacterSpacing = 60, Foreground = Brush(e.Success ? "OwnlyTextBrush" : "OwnlyDangerBrush"), VerticalAlignment = VerticalAlignment.Center });
            head.Children.Add(new TextBlock { Text = e.Title, FontSize = 15, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Foreground = Brush("OwnlyTextBrush") });
            s.Children.Add(head);
            s.Children.Add(new TextBlock { Text = $"{e.At:g} · {e.Method}" + (e.Elevated ? " · elevated" : ""), FontSize = 11, Foreground = Brush("OwnlyFaintTextBrush") });
            if (!string.IsNullOrWhiteSpace(e.Output))
            {
                s.Children.Add(new Border
                {
                    Background = Brush("OwnlyBackgroundBrush"),
                    BorderBrush = Brush("OwnlyBorderBrush"),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(12, 8, 12, 8),
                    Margin = new Thickness(0, 4, 0, 0),
                    Child = new TextBlock
                    {
                        Text = e.Output.Length > 600 ? e.Output[..600] + " …" : e.Output,
                        FontFamily = new FontFamily("Consolas"),
                        FontSize = 11,
                        TextWrapping = TextWrapping.Wrap,
                        Foreground = Brush("OwnlyMutedTextBrush"),
                        IsTextSelectionEnabled = true
                    }
                });
            }
            card.Child = s;
            ContentStack.Children.Add(card);
        }
    }

    // ---------- entrance animation ----------

    private void AnimateIn()
    {
        var i = 0;
        foreach (var child in ContentStack.Children)
        {
            if (child is not FrameworkElement el)
            {
                continue;
            }

            var delay = TimeSpan.FromMilliseconds(45 * Math.Min(i, 8));
            Microsoft.UI.Xaml.Hosting.ElementCompositionPreview.SetIsTranslationEnabled(el, true);
            var visual = Microsoft.UI.Xaml.Hosting.ElementCompositionPreview.GetElementVisual(el);
            var compositor = visual.Compositor;

            visual.Opacity = 0f;
            visual.Properties.InsertVector3("Translation", new System.Numerics.Vector3(0, 16, 0));

            var fade = compositor.CreateScalarKeyFrameAnimation();
            fade.InsertKeyFrame(1f, 1f);
            fade.Duration = TimeSpan.FromMilliseconds(260);
            fade.DelayTime = delay;
            visual.StartAnimation("Opacity", fade);

            var move = compositor.CreateVector3KeyFrameAnimation();
            move.InsertKeyFrame(1f, new System.Numerics.Vector3(0, 0, 0));
            move.Duration = TimeSpan.FromMilliseconds(320);
            move.DelayTime = delay;
            visual.StartAnimation("Translation", move);

            i++;
        }
    }

    // ---------- changes ----------

    private void RenderChanges()
    {
        var changes = new ChangeLogService().Read().OrderByDescending(c => c.AppliedAt).ToList();
        if (changes.Count == 0)
        {
            ContentStack.Children.Add(SimpleCard("No changes yet", "Ownly has not modified this PC. Anything you apply will be listed here with a restore option."));
            return;
        }

        foreach (var change in changes)
        {
            var card = Card();
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var text = new StackPanel { Spacing = 5, VerticalAlignment = VerticalAlignment.Center };
            text.Children.Add(new TextBlock { Text = change.FeatureName, FontSize = 16, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Foreground = Brush("OwnlyTextBrush") });
            text.Children.Add(new TextBlock { Text = change.Summary, FontSize = 13, TextWrapping = TextWrapping.Wrap, Foreground = Brush("OwnlyMutedTextBrush") });
            text.Children.Add(new TextBlock { Text = $"{change.AppliedAt:g} · {change.Category} · {change.Risk} risk", FontSize = 11, Foreground = Brush("OwnlyFaintTextBrush") });
            Grid.SetColumn(text, 0);
            grid.Children.Add(text);

            if (change.Reversible && !change.IsRestored)
            {
                var restore = new Button { Content = "RESTORE", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(16, 0, 0, 0) };
                restore.Click += (_, _) =>
                {
                    var result = change.FeatureId.StartsWith("tweak:", StringComparison.Ordinal)
                        ? new Ownly.Core.Tweaks.TweakRunner().Revert(change)
                        : new SafetyEngine().Restore(change);
                    FeedbackText.Text = result.Message;
                    if (result.Success)
                    {
                        Render();
                    }
                };
                Grid.SetColumn(restore, 1);
                grid.Children.Add(restore);
            }
            else
            {
                var badge = new TextBlock
                {
                    Text = change.IsRestored ? "RESTORED" : "APPLIED",
                    FontSize = 10,
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    CharacterSpacing = 70,
                    Foreground = Brush("OwnlyFaintTextBrush"),
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(16, 0, 0, 0)
                };
                Grid.SetColumn(badge, 1);
                grid.Children.Add(badge);
            }

            card.Child = grid;
            ContentStack.Children.Add(card);
        }
    }
}
