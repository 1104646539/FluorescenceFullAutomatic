using FluorescenceFullAutomatic.Platform.Services;
using Prism.Ioc;
using Prism.Modularity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluorescenceFullAutomatic.Platform
{
    public class PlatformModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            //Console.WriteLine("PlatformModule RegisterTypes called");
            //containerRegistry.RegisterSingleton<ILogService, LogService>();
            //containerRegistry.RegisterSingleton<IConfigService, ConfigService>();
            //containerRegistry.RegisterSingleton<IDialogService, DialogService>();
            //containerRegistry.RegisterSingleton<IToolService, ToolService>();
            //containerRegistry.RegisterSingleton<IDispatcherService, DispatcherService>();

            //containerRegistry.RegisterSingleton<IPatientService, PatientService>();
            //containerRegistry.RegisterSingleton<IPointService, PointService>();
            //containerRegistry.RegisterSingleton<IPrintService, PrintService>();
            //containerRegistry.RegisterSingleton<IProjectService, ProjectService>();
            //containerRegistry.RegisterSingleton<IReactionAreaQueueService, ReactionAreaQueueService>();
            //containerRegistry.RegisterSingleton<ISerialPortService, SerialPortService>();
            //containerRegistry.RegisterSingleton<ITestResultService, TestResultService>();
            //containerRegistry.RegisterSingleton<IExportExcelService, ExportExcelService>();



            //containerRegistry.RegisterSingleton<IApplyTestService, ApplyTestService>();
            //containerRegistry.RegisterSingleton<IDataManagerService, DataManagerService>();
            //containerRegistry.RegisterSingleton<ISettingsService, SettingsService>();
            //containerRegistry.RegisterSingleton<IHomeService, HomeService>();
            //containerRegistry.RegisterSingleton<ILisService, LisService>();
        }
    }
}
