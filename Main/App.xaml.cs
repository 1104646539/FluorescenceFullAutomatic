using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using FluorescenceFullAutomatic.HomeModule.Services;
using FluorescenceFullAutomatic.HomeModule.Views;
using FluorescenceFullAutomatic.Platform;
using FluorescenceFullAutomatic.Platform.Services;
using FluorescenceFullAutomatic.Platform.Sql;
using FluorescenceFullAutomatic.ViewModels;
using FluorescenceFullAutomatic.Views;
using MahApps.Metro.Controls.Dialogs;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Unity;
using Serilog;
using HomeModule_Module = HomeModule.HomeModule;
using UploadModule_Module = UploadModule.UploadModule;
namespace FluorescenceFullAutomatic
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : PrismApplication
    {
        // 定义互斥锁，用于确保应用程序只有一个实例
        private static Mutex _mutex = null;
        private const string MutexName = "FluorescenceFullAutomaticSingleInstance";
        
        // Windows API 声明，用于查找和激活窗口
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);
        
        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        
        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);
        
        private const int SW_RESTORE = 9;
        
        protected override Window CreateShell()
        {
            // 检查应用程序是否已经在运行
            if (!EnsureSingleInstance())
            {
                // 如果已经有一个实例在运行，则退出当前实例
                Shutdown();
                return null;
            }
            
            AppDomain.CurrentDomain.UnhandledException += HandleUnhandledException;
            Init();
            //Workbook workbook = null;
            //workbook.PrintDocument.Print();
            return Container.Resolve<MainWindow>();
        }
        
        /// <summary>
        /// 确保应用程序只有一个实例在运行
        /// </summary>
        /// <returns>如果是第一个实例则返回true，否则返回false</returns>
        private bool EnsureSingleInstance()
        {
            bool createdNew;
            
            try
            {
                // 尝试创建一个命名互斥锁
                _mutex = new Mutex(true, MutexName, out createdNew);
                
                if (!createdNew)
                {
                    // 如果互斥锁已存在，说明已经有一个实例在运行
                    // 尝试查找并激活已运行的实例
                    ActivateExistingInstance();
                    //MessageBox.Show("应用程序已经在运行中。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return false;
                }
                
                // 在应用程序退出时释放互斥锁
                Application.Current.Exit += (s, e) =>
                {
                    if (_mutex != null)
                    {
                        _mutex.ReleaseMutex();
                        _mutex.Close();
                    }
                };
                
                return true;
            }
            catch (Exception ex)
            {
                // 如果创建互斥锁时发生异常，记录日志并允许应用程序继续运行
                Log.Error($"创建互斥锁时发生异常: {ex.Message}");
                return true;
            }
        }
        
        /// <summary>
        /// 查找并激活已经运行的应用程序实例
        /// </summary>
        private void ActivateExistingInstance()
        {
            try
            {
                // 获取当前进程名称
                string currentProcessName = Process.GetCurrentProcess().ProcessName;
                
                // 查找具有相同名称的所有进程
                Process[] processes = Process.GetProcessesByName(currentProcessName);
                
                foreach (Process process in processes)
                {
                    // 跳过当前进程
                    if (process.Id != Process.GetCurrentProcess().Id)
                    {
                        // 获取主窗口句柄
                        IntPtr mainWindowHandle = process.MainWindowHandle;
                        
                        if (mainWindowHandle != IntPtr.Zero)
                        {
                            // 如果窗口是最小化的，则恢复它
                            if (IsIconic(mainWindowHandle))
                            {
                                ShowWindow(mainWindowHandle, SW_RESTORE);
                            }
                            
                            // 将窗口置于前台
                            SetForegroundWindow(mainWindowHandle);
                            
                            Log.Information("已将现有应用程序实例激活并置于前台");
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"激活现有实例时发生错误: {ex.Message}");
            }
        }

        protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
        {
            moduleCatalog.AddModule<PlatformModule>();
            moduleCatalog.AddModule<UploadModule_Module>();
            moduleCatalog.AddModule<HomeModule_Module>();
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

            //containerRegistry.RegisterSingleton<IDialogCoordinator, DialogCoordinator>();
            //containerRegistry.RegisterSingleton<IDataManagerService, DataManagerService>();
            //containerRegistry.RegisterSingleton<ISettingsService, SettingsService>();
            //containerRegistry.RegisterSingleton<IApplyTestService, ApplyTestService>();
            //containerRegistry.RegisterSingleton<ILisService, LisService>();
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
