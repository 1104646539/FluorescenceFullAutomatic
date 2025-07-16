using System;
using System.Windows;
using System.Windows.Threading;
using FluorescenceFullAutomatic.HomeModule.Services;
using FluorescenceFullAutomatic.HomeModule.Views;
using FluorescenceFullAutomatic.Platform;
using FluorescenceFullAutomatic.Platform.Services;
using FluorescenceFullAutomatic.Platform.Sql;
using FluorescenceFullAutomatic.Views;
using MahApps.Metro.Controls.Dialogs;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Unity;
using Serilog;
using Spire.Xls;

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
            Init();
            //Workbook workbook = null;
            //workbook.PrintDocument.Print();
            return Container.Resolve<MainWindow>();
        }

        protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
        {
            base.ConfigureModuleCatalog(moduleCatalog);
            moduleCatalog.AddModule<PlatformModule>();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry) {
            //services
            containerRegistry.RegisterSingleton<ILogService, LogService>();

            containerRegistry.RegisterSingleton<IHomeService, HomeService>();
            containerRegistry.RegisterSingleton<ISerialPortService, SerialPortService>();
            containerRegistry.RegisterSingleton<IDialogCoordinator, DialogCoordinator>();
            containerRegistry.RegisterSingleton<IDataManagerService, DataManagerService>();
            containerRegistry.RegisterSingleton<ISettingsService, SettingsService>();

            containerRegistry.RegisterSingleton<IApplyTestService, ApplyTestService>();
            containerRegistry.RegisterSingleton<IConfigService, ConfigService>();
            containerRegistry.RegisterSingleton<IDialogService, DialogService>();
            containerRegistry.RegisterSingleton<IExportExcelService, ExportExcelService>();
            containerRegistry.RegisterSingleton<ILisService, LisService>();
            containerRegistry.RegisterSingleton<IPatientService, PatientService>();
            containerRegistry.RegisterSingleton<IPointService, PointService>();
            containerRegistry.RegisterSingleton<IPrintService, PrintService>();
            containerRegistry.RegisterSingleton<IProjectService, ProjectService>();
            containerRegistry.RegisterSingleton<ITestResultService, TestResultService>();
            containerRegistry.RegisterSingleton<IToolService, ToolService>();
            containerRegistry.RegisterSingleton<IDispatcherService, DispatcherService>();
            containerRegistry.RegisterSingleton<IReactionAreaQueueService, ReactionAreaQueueService>();

            //navigation region
            containerRegistry.RegisterForNavigation(typeof(HomeView), typeof(HomeView).Name);
            containerRegistry.RegisterForNavigation(typeof(DataManagerView), typeof(DataManagerView).Name);
            containerRegistry.RegisterForNavigation(typeof(ApplyTestView), typeof(ApplyTestView).Name);
            containerRegistry.RegisterForNavigation(typeof(QCView), typeof(QCView).Name);
            containerRegistry.RegisterForNavigation(typeof(SettingsView), typeof(SettingsView).Name);

        }


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

   
        private void Init() {
            InitLog();
            InitDB();
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
