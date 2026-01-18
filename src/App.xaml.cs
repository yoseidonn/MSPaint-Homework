using System;
using System.Windows;
using WpfApplication = System.Windows.Application;
using WpfMessageBox = System.Windows.MessageBox;

namespace MSPaint
{
    public partial class App : WpfApplication
    {
        protected override void OnStartup(System.Windows.StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // Handle unhandled exceptions
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            
            // Create and show main window
            var mainWindow = new Views.MainWindow();
            mainWindow.Show();
            
            // Show canvas setup dialog on startup
            if (mainWindow.DataContext is ViewModels.MainViewModel viewModel)
            {
                viewModel.FileNew();
            }
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            string errorMessage = $"An unhandled exception occurred:\n\n{e.Exception.Message}\n\nStack Trace:\n{e.Exception.StackTrace}";
            WpfMessageBox.Show(errorMessage, "Application Error", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true; // Prevent app from crashing
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                string errorMessage = $"A fatal exception occurred:\n\n{ex.Message}\n\nStack Trace:\n{ex.StackTrace}";
                WpfMessageBox.Show(errorMessage, "Fatal Application Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
