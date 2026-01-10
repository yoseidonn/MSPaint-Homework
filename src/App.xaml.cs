using System;
using System.Windows;
using WpfApplication = System.Windows.Application;
using WpfMessageBox = System.Windows.MessageBox;

namespace MSPaint
{
    public partial class App : WpfApplication
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // Handle unhandled exceptions
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            
            // Show canvas setup window first
            var setupWindow = new Pages.CanvasSetupWindow();
            if (setupWindow.ShowDialog() == true)
            {
                // Create and show the main window
                var mainWindow = new MainWindow();
                
                // Initialize canvas based on setup result
                if (setupWindow.LoadedGrid != null)
                {
                    // Load from file
                    var page = mainWindow.GetDrawingPage();
                    if (page != null)
                    {
                        page.InitializeCanvas(setupWindow.LoadedGrid).Wait();
                    }
                }
                else if (setupWindow.Result != null)
                {
                    // Create new canvas with settings
                    var page = mainWindow.GetDrawingPage();
                    if (page != null)
                    {
                        page.InitializeCanvas(setupWindow.Result).Wait();
                    }
                }
                
                mainWindow.Show();
            }
            else
            {
                // User cancelled, exit application
                Shutdown();
            }
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            string errorMessage = $"An unhandled exception occurred:\n\n{e.Exception}\n\nStack Trace:\n{e.Exception.StackTrace}";
            WpfMessageBox.Show(errorMessage, "Application Error", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true; // Prevent app from crashing
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                string errorMessage = $"A fatal exception occurred:\n\n{ex}\n\nStack Trace:\n{ex.StackTrace}";
                WpfMessageBox.Show(errorMessage, "Fatal Application Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}