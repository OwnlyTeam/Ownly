using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Ownly
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        private Window? _window;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                Log("AppDomain.UnhandledException", e.ExceptionObject as Exception);
            try
            {
                InitializeComponent();
                this.UnhandledException += (s, e) =>
                {
                    Log("Application.UnhandledException", e.Exception);
                    e.Handled = true;
                };
            }
            catch (Exception ex)
            {
                Log("App() ctor", ex);
                throw;
            }
        }

        internal static void Log(string where, Exception? ex)
        {
            try
            {
                var dir = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Ownly");
                System.IO.Directory.CreateDirectory(dir);
                System.IO.File.AppendAllText(
                    System.IO.Path.Combine(dir, "diagnostics.log"),
                    $"[{DateTime.Now:o}] {where}" + (ex is null ? "\n" : $"\n{ex}\n") + "\n");
                if (ex is not null)
                {
                    System.IO.File.AppendAllText(
                        System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Ownly-crash.log"),
                        $"[{DateTime.Now:o}] {where}\n{ex}\n\n");
                }
            }
            catch
            {
                // best effort only
            }
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            try
            {
                if (new Ownly.Core.App.AcceptanceService().HasAcceptedCurrent())
                {
                    ShowMainWindow();
                    return;
                }

                var disclaimer = new Ownly.Views.DisclaimerWindow();
                disclaimer.Accepted += (_, _) => ShowMainWindow();
                _window = disclaimer;
                disclaimer.Activate();
            }
            catch (Exception ex)
            {
                Log("OnLaunched", ex);
                throw;
            }
        }

        private void ShowMainWindow()
        {
            _window = new MainWindow();
            _window.Activate();
        }
    }
}
