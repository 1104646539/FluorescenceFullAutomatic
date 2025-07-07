using FluorescenceFullAutomatic.Model;
using FluorescenceFullAutomatic.Repositorys;
using FluorescenceFullAutomatic.Services;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows.Documents;

namespace FluorescenceFullAutomatic.ViewModels
{
    public class ServiceLocator
    {
        private IServiceProvider _serviceProvider;
        public MainViewModel MainViewModel => _serviceProvider.GetService<MainViewModel>();
        public HomeViewModel HomeViewModel => _serviceProvider.GetService<HomeViewModel>();
        public DebugViewModel DebugViewModel => _serviceProvider.GetService<DebugViewModel>();
        public DataManagerViewModel DataManagerViewModel => _serviceProvider.GetService<DataManagerViewModel>();
        public ProjectListViewModel ProjectListViewModel => _serviceProvider.GetService<ProjectListViewModel>();
        public ProjectDetailsViewModel ProjectDetailsViewModel => _serviceProvider.GetService<ProjectDetailsViewModel>();
        public SettingsViewModel SettingsViewModel => _serviceProvider.GetService<SettingsViewModel>();
        public ApplyTestViewModel ApplyTestViewModel => _serviceProvider.GetService<ApplyTestViewModel>();
        public QCViewModel QCViewModel => _serviceProvider.GetService<QCViewModel>();
        public TestSettingsViewModel TestSettingsViewModel => _serviceProvider.GetService<TestSettingsViewModel>();
        public UploadSettingsViewModel UploadSettingsViewModel => _serviceProvider.GetService<UploadSettingsViewModel>();
        public RunningLogViewModel RunningLogViewModel => _serviceProvider.GetService<RunningLogViewModel>();
        public VersionViewModel VersionViewModel => _serviceProvider.GetService<VersionViewModel>();

        public DialogViewModel DialogViewModel => _serviceProvider.GetService<DialogViewModel>();

        public ServiceLocator()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<IHomeService, HomeService>();
            serviceCollection.AddSingleton<IConfigService, ConfigService>();
            serviceCollection.AddSingleton<ISerialPortService, SerialPortService>();
            serviceCollection.AddSingleton<IDialogCoordinator, DialogCoordinator>();
            serviceCollection.AddSingleton<IDataManagerService, DataManagerService>();
            serviceCollection.AddSingleton<IProjectService, ProjectService>();
            serviceCollection.AddSingleton<ISettingsService, SettingsService>();
            serviceCollection.AddSingleton<IApplyTestService, ApplyTestService>();
            serviceCollection.AddSingleton<IQCService, QCService>();
            serviceCollection.AddSingleton<IUploadService,UploadService>();
            serviceCollection.AddSingleton<IRunningLogService,RunningLogService>();
            serviceCollection.AddSingleton<ILisService, LisService>();
            serviceCollection.AddSingleton<IExcelExportService, ExcelExportService>();

            //repository
            serviceCollection.AddSingleton<IApplyTestRepository, ApplyTestRepository>();
            serviceCollection.AddSingleton<IConfigRepository, ConfigRepository>();
            serviceCollection.AddSingleton<IDialogRepository, DialogRepository>();
            serviceCollection.AddSingleton<IExportExcelRepository, ExportExcelRepository>();
            serviceCollection.AddSingleton<ILisRepository, LisRepository>();
            serviceCollection.AddSingleton<IPatientRepository, PatientRepository>();
            serviceCollection.AddSingleton<IPointRepository, PointRepository>();
            serviceCollection.AddSingleton<IPrintRepository, PrintRepository>();
            serviceCollection.AddSingleton<IProjectRepository, ProjectRepository>();
            serviceCollection.AddSingleton<ITestResultRepository, TestResultRepository>();
            serviceCollection.AddSingleton<IToolRepository, ToolRepository>();
            serviceCollection.AddSingleton<IUploadConfigRepository, UploadConfigRepository>();
            serviceCollection.AddSingleton<IDispatcherService, DispatcherService>();
            serviceCollection.AddSingleton<IReactionAreaQueueRepository, ReactionAreaQueueRepository>();

            serviceCollection.AddSingleton<MainViewModel>();
            serviceCollection.AddSingleton<HomeViewModel>();
            serviceCollection.AddSingleton<DebugViewModel>();
            serviceCollection.AddSingleton<DataManagerViewModel>();
            serviceCollection.AddSingleton<ProjectListViewModel>();
            //serviceCollection.AddSingleton<ProjectDetailsViewModel>();
            serviceCollection.AddSingleton<SettingsViewModel>();
            serviceCollection.AddSingleton<ApplyTestViewModel>();
            serviceCollection.AddSingleton<QCViewModel>();
            serviceCollection.AddSingleton<TestSettingsViewModel>();
            serviceCollection.AddSingleton<UploadSettingsViewModel>();
            serviceCollection.AddSingleton<RunningLogViewModel>();
            serviceCollection.AddSingleton<VersionViewModel>();
            //serviceCollection.AddSingleton<DialogViewModel>();

            _serviceProvider = serviceCollection.BuildServiceProvider();
        }
    }
}
