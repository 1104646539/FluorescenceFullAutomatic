using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using FluorescenceFullAutomatic.Sql;
using FluorescenceFullAutomatic.ViewModels;
using FluorescenceFullAutomatic.Views;
using Serilog;

namespace FluorescenceFullAutomatic
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // this.DispatcherUnhandledException += App_DispatcherUnhandledException;
            base.OnStartup(e);
            AppDomain.CurrentDomain.UnhandledException += HandleUnhandledException;

        }

        private void HandleUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception;
            if (exception != null)
            {
                Log.Fatal(exception, "发生了未处理的异常");
            }
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            Log.Error($"发生了错误 {e.Exception}");
            //e.Handled = true;
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            InitLog();
            InitDB();
            
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
        }

        private void InitDB()
        {
            SqlHelper.getInstance();
        }

        private void InitLog()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .WriteTo.File("logs/myapp.txt", rollingInterval: RollingInterval.Day)
                .Enrich.FromLogContext()
                .CreateLogger();
        }
    }
}
