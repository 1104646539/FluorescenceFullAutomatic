using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluorescenceFullAutomatic.Services;
using System.Windows;
using FluorescenceFullAutomatic.Utils;
using CommunityToolkit.Mvvm.Messaging;
using FluorescenceFullAutomatic.Config;
using System.Collections.ObjectModel;
using Microsoft.Win32;
using System.Drawing.Printing;

namespace FluorescenceFullAutomatic.ViewModels
{
    public partial class TestSettingsViewModel:ObservableObject
    {
         public ObservableCollection<string> PrinterList { get; } = new ObservableCollection<string>();

         [ObservableProperty]
         private string selectedPrinter;

         [ObservableProperty]
         private string singleReportTemplatePath;

         [ObservableProperty]
         private string doubleReportTemplatePath;

         [ObservableProperty]
         private string testNum;

         [ObservableProperty]
         private double samplingVolumn;

         [ObservableProperty]
         private int cleanoutDuration;
         
         [ObservableProperty]
         private int reactionDuration;

         [ObservableProperty]
         private bool isScanBarcode;

         [ObservableProperty]
         private bool isAutoPrintTicket;

         [ObservableProperty]
         private bool isAutoPrintA4Report;

         private readonly IConfigService configRepository;
        private readonly IDialogService dialogRepository;
        private readonly IToolService toolRepository;
        public TestSettingsViewModel(IToolService toolRepository,IConfigService configRepository,IDialogService dialogRepository)
         {
            this.toolRepository = toolRepository;
             this.configRepository = configRepository;
             this.dialogRepository = dialogRepository;
             LoadSettings();
             WeakReferenceMessenger.Default.Register<EventMsg<string>>(this, (r, m) =>
             {
                 if (m.What == EventWhat.WHAT_CLICK_SETTINGS)
                 {
                     LoadSettings();
                 }
             });
        }

         private void LoadSettings()
         {
             TestNum = configRepository.TestNum().ToString();
             SamplingVolumn = configRepository.SamplingVolume();
             CleanoutDuration = configRepository.CleanoutDuration();
             ReactionDuration = configRepository.ReactionDuration();
             IsScanBarcode = configRepository.IsScanBarcode();
             IsAutoPrintTicket = configRepository.IsAutoPrintTicket();
             IsAutoPrintA4Report = configRepository.IsAutoPrintA4Report();

             // 加载打印机列表
             PrinterList.Clear();
             foreach (string printer in toolRepository.GetPrinters())
             {
                 PrinterList.Add(printer);
             }
             

             // 设置当前选中的打印机
             if (!string.IsNullOrEmpty(configRepository.GetPrinterName()))
             {
                 SelectedPrinter = PrinterList.FirstOrDefault(p => p == configRepository.GetPrinterName());
             }

             // 加载模板路径
             SingleReportTemplatePath = configRepository.GetReportTemplatePath();
             DoubleReportTemplatePath = configRepository.GetReportDoubleTemplatePath();
         }

     
         [RelayCommand]
         private void SelectSingleTemplate()
         {
            ShowSelectReportDialog(true);
         }

        private void ShowSelectReportDialog(bool isSingle)
        {
            var dialog = new OpenFileDialog
             {
                 Filter = "xlsx文档|*.xlsx|所有文件|*.*",
                 Title = isSingle ? "选择单联报告模板" : "选择双联报告模板",
                 InitialDirectory = !string.IsNullOrEmpty(isSingle ? SingleReportTemplatePath : DoubleReportTemplatePath) 
                 ? System.IO.Path.GetDirectoryName(isSingle ? SingleReportTemplatePath : DoubleReportTemplatePath) 
                 : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
             };

             if (dialog.ShowDialog() == true)
             {
                if (isSingle) {
                    SingleReportTemplatePath = dialog.FileName;
                } else {
                    DoubleReportTemplatePath = dialog.FileName;
                }
             }
        }

        [RelayCommand]
         private void SelectDoubleTemplate()
         {
            ShowSelectReportDialog(false);
            
         }

         [RelayCommand]
         private void SaveSettings()
         {
            if (SystemGlobal.MachineStatus.IsRunning()) {
                dialogRepository.ShowHiltDialog("提示", "当前仪器正在运行，请先等待检测完毕！", "确定", (m, d) => { });
                return;
            }
             // 检测编号校验
             if (!int.TryParse(TestNum, out int testNumValue) || testNumValue <= 0)
             {
                dialogRepository.ShowHiltDialog("提示", "检测编号必须是大于0的整数！", "确定", (m,d)=>{ });
                 return;
             }
             
             // 取样量校验
             if (SamplingVolumn <= 1 || SamplingVolumn >= 300)
             {
                dialogRepository.ShowHiltDialog("提示", "取样量必须大于1且小于300！", "确定", (m, d) => { });
                 return;
             }
             
             // 清洗时长校验
             if (CleanoutDuration <= 10 || CleanoutDuration > 10000)
            {
                dialogRepository.ShowHiltDialog("提示", "清洗时长必须大于10且不超过10000！", "确定", (m, d) => { });
                 return;
             }
             
             // 反应时长校验
             if (ReactionDuration <= 0 || ReactionDuration > 3600)
            {
                dialogRepository.ShowHiltDialog("提示", "反应时长必须大于0且不超过3600！", "确定", (m, d) => { });
                 return;
             }

            // SingleReportTemplatePath 和 DoubleReportTemplatePath 校验
            if (string.IsNullOrEmpty(SingleReportTemplatePath) || string.IsNullOrEmpty(DoubleReportTemplatePath))
            {
                dialogRepository.ShowHiltDialog("提示", "请选择单联和双联报告模板！", "确定", (m, d) => { });
                 return;
            }
            
             
             
             // 保存设置
             configRepository.SetTestNum(testNumValue);
             configRepository.SetSamplingVolume((int)SamplingVolumn);
             configRepository.SetCleanoutDuration(CleanoutDuration);
             configRepository.SetReactionDuration(ReactionDuration);
             configRepository.SetIsScanBarcode(IsScanBarcode);
             configRepository.SetIsAutoPrintTicket(IsAutoPrintTicket);
             configRepository.SetIsAutoPrintA4Report(IsAutoPrintA4Report);
            configRepository.SetReportTemplatePath(SingleReportTemplatePath);
            configRepository.SetDoubleReportTemplatePath(DoubleReportTemplatePath);
            configRepository.SetPrinterName(SelectedPrinter);
           
            dialogRepository.ShowHiltDialog("提示", "设置已保存！", "确定", (m, d) => { });
            WeakReferenceMessenger.Default.Send(new EventMsg<string>("") { What = EventWhat.WHAT_CHANGE_TEST_SETTINGS});
            return;
        }

    
    }

    public class PrinterInfo
    {
        public string Name { get; set; }
    }
}
