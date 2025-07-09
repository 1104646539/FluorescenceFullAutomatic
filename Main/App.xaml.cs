using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Threading;
using FluorescenceFullAutomatic.Repositorys;
using FluorescenceFullAutomatic.Services;
using FluorescenceFullAutomatic.Sql;
using FluorescenceFullAutomatic.ViewModels;
using FluorescenceFullAutomatic.Views;
using FluorescenceFullAutomatic.Views.Ctr;
using MahApps.Metro.Controls.Dialogs;
using Prism.Ioc;
using Prism.Unity;
using Serilog;

namespace FluorescenceFullAutomatic
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : PrismApplication
    {
        protected override Window CreateShell()
        {
            AppDomain.CurrentDomain.UnhandledException += HandleUnhandledException;
            return Container.Resolve<MainWindow>();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry) {
            containerRegistry.RegisterSingleton<IHomeService, HomeService>();
            containerRegistry.RegisterSingleton<IConfigService, ConfigService>();
            containerRegistry.RegisterSingleton<ISerialPortService, SerialPortService>();
            containerRegistry.RegisterSingleton<IDialogCoordinator, DialogCoordinator>();
            containerRegistry.RegisterSingleton<IDataManagerService, DataManagerService>();
            containerRegistry.RegisterSingleton<IProjectService, ProjectService>();
            containerRegistry.RegisterSingleton<ISettingsService, SettingsService>();
            containerRegistry.RegisterSingleton<IApplyTestService, ApplyTestService>();
            containerRegistry.RegisterSingleton<IQCService, QCService>();
            containerRegistry.RegisterSingleton<IUploadService, UploadService>();
            containerRegistry.RegisterSingleton<IRunningLogService, RunningLogService>();
            containerRegistry.RegisterSingleton<ILisService, LisService>();
            containerRegistry.RegisterSingleton<IExcelExportService, ExcelExportService>();

            //repository
            containerRegistry.RegisterSingleton<IApplyTestRepository, ApplyTestRepository>();
            containerRegistry.RegisterSingleton<IConfigRepository, ConfigRepository>();
            containerRegistry.RegisterSingleton<IDialogRepository, DialogRepository>();
            containerRegistry.RegisterSingleton<IExportExcelRepository, ExportExcelRepository>();
            containerRegistry.RegisterSingleton<ILisRepository, LisRepository>();
            containerRegistry.RegisterSingleton<IPatientRepository, PatientRepository>();
            containerRegistry.RegisterSingleton<IPointRepository, PointRepository>();
            containerRegistry.RegisterSingleton<IPrintRepository, PrintRepository>();
            containerRegistry.RegisterSingleton<IProjectRepository, ProjectRepository>();
            containerRegistry.RegisterSingleton<ITestResultRepository, TestResultRepository>();
            containerRegistry.RegisterSingleton<IToolRepository, ToolRepository>();
            containerRegistry.RegisterSingleton<IUploadConfigRepository, UploadConfigRepository>();
            containerRegistry.RegisterSingleton<IDispatcherService, DispatcherService>();
            containerRegistry.RegisterSingleton<IReactionAreaQueueRepository, ReactionAreaQueueRepository>();

            
            //serviceCollection.AddSingleton<MainViewModel>();
            //serviceCollection.AddSingleton<HomeViewModel>();
            //serviceCollection.AddSingleton<DebugViewModel>();
            //serviceCollection.AddSingleton<DataManagerViewModel>();
            //serviceCollection.AddSingleton<ProjectListViewModel>();
            ////serviceCollection.AddSingleton<ProjectDetailsViewModel>();
            //serviceCollection.AddSingleton<SettingsViewModel>();
            //serviceCollection.AddSingleton<ApplyTestViewModel>();
            //serviceCollection.AddSingleton<QCViewModel>();
            //serviceCollection.AddSingleton<TestSettingsViewModel>();
            //serviceCollection.AddSingleton<UploadSettingsViewModel>();
            //serviceCollection.AddSingleton<RunningLogViewModel>();
            //serviceCollection.AddSingleton<VersionViewModel>();
        }

        //protected override void OnStartup(StartupEventArgs e)
        //{
        //    // this.DispatcherUnhandledException += App_DispatcherUnhandledException;
        //    base.OnStartup(e);
            
        //}

        private void HandleUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception;
            if (exception != null)
            {
                Log.Fatal(exception, "发生了未处理的异常");
            }
        }

        private void App_DispatcherUnhandledException(
            object sender,
            DispatcherUnhandledExceptionEventArgs e
        )
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
