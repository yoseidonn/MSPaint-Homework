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
            
            // Set shutdown mode to explicit (don't close when last window closes)
            this.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            
            // Handle unhandled exceptions FIRST, before anything else
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            
            // Also handle TaskScheduler unobserved exceptions
            System.Threading.Tasks.TaskScheduler.UnobservedTaskException += (s, args) =>
            {
                WpfMessageBox.Show(
                    $"Unobserved task exception: {args.Exception.Message}",
                    "Task Exception",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                args.SetObserved();
            };
            
            try
            {
                // Show canvas setup window first
                var setupWindow = new Pages.CanvasSetupWindow();
                bool? dialogResult = setupWindow.ShowDialog();
                
                if (dialogResult == true)
                {
                    // Create and show the main window first
                    MainWindow? mainWindow = null;
                    try
                    {
                        mainWindow = new MainWindow();
                        mainWindow.Show();
                        
                        // Use async method to initialize canvas after a short delay
                        // This ensures the window is fully rendered
                        // Fire and forget - we don't need to wait for it
                        System.Threading.Tasks.Task.Run(async () =>
                        {
                            await System.Threading.Tasks.Task.Delay(200);
                            await this.Dispatcher.InvokeAsync(async () =>
                            {
                                await InitializeCanvasAfterDelay(mainWindow, setupWindow);
                            });
                        });
                    }
                    catch (Exception ex)
                    {
                        WpfMessageBox.Show(
                            $"Error creating main window: {ex.Message}\n\n{ex.StackTrace}",
                            "Window Creation Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                        Shutdown();
                        return;
                    }
                }
                else
                {
                    // User cancelled, exit application
                    Shutdown();
                }
            }
            catch (Exception ex)
            {
                WpfMessageBox.Show(
                    $"Fatal startup error: {ex.Message}\n\n{ex.StackTrace}",
                    "Startup Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Shutdown();
            }
        }

        private async System.Threading.Tasks.Task InitializeCanvasAfterDelay(MainWindow mainWindow, Pages.CanvasSetupWindow setupWindow)
        {
            try
            {
                // Wait a bit for window to be fully rendered
                await System.Threading.Tasks.Task.Delay(200);
                
                await mainWindow.InitializeCanvasAsync(setupWindow);
            }
            catch (Exception ex)
            {
                WpfMessageBox.Show(
                    $"Error initializing canvas: {ex.Message}\n\n{ex.StackTrace}",
                    "Initialization Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
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