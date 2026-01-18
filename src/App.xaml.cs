using System;
using System.Windows;

namespace MSPaint
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // Handle unhandled exceptions
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            string errorMessage = $"An unhandled exception occurred:\n\n{e.Exception.Message}\n\nStack Trace:\n{e.Exception.StackTrace}";
            MessageBox.Show(errorMessage, "Application Error", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true; // Prevent app from crashing
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                string errorMessage = $"A fatal exception occurred:\n\n{ex.Message}\n\nStack Trace:\n{ex.StackTrace}";
                MessageBox.Show(errorMessage, "Fatal Application Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
