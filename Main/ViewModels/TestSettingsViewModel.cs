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

         private readonly IConfigService _configService;

         public TestSettingsViewModel(IConfigService configService)
         {
             _configService = configService;
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
             TestNum = _configService.TestNum().ToString();
             SamplingVolumn = _configService.SamplingVolumn();
             CleanoutDuration = _configService.CleanoutDuration();
             ReactionDuration = _configService.ReactionDuration();
             IsScanBarcode = _configService.IsScanBarcode();
             IsAutoPrintTicket = _configService.IsAutoPrintTicket();
             IsAutoPrintA4Report = _configService.IsAutoPrintA4Report();

             // 加载打印机列表
             PrinterList.Clear();
             foreach (string printer in _configService.GetPrinterInfos())
             {
                 PrinterList.Add(printer);
             }
             

             // 设置当前选中的打印机
             if (!string.IsNullOrEmpty(_configService.GetPrinterName()))
             {
                 SelectedPrinter = PrinterList.FirstOrDefault(p => p == _configService.GetPrinterName());
             }

             // 加载模板路径
             SingleReportTemplatePath = _configService.GetReportTemplatePath();
             DoubleReportTemplatePath = _configService.GetReportDoubleTemplatePath();
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
                GlobalUtil.ShowHiltDialog("提示", "当前仪器正在运行，请先等待检测完毕！", "确定", (m, d) => { });
                return;
            }
             // 检测编号校验
             if (!int.TryParse(TestNum, out int testNumValue) || testNumValue <= 0)
             {
                GlobalUtil.ShowHiltDialog("提示", "检测编号必须是大于0的整数！", "确定", (m,d)=>{ });
                 return;
             }
             
             // 取样量校验
             if (SamplingVolumn <= 1 || SamplingVolumn >= 300)
             {
                GlobalUtil.ShowHiltDialog("提示", "取样量必须大于1且小于300！", "确定", (m, d) => { });
                 return;
             }
             
             // 清洗时长校验
             if (CleanoutDuration <= 10 || CleanoutDuration > 10000)
            {
                GlobalUtil.ShowHiltDialog("提示", "清洗时长必须大于10且不超过10000！", "确定", (m, d) => { });
                 return;
             }
             
             // 反应时长校验
             if (ReactionDuration <= 0 || ReactionDuration > 3600)
            {
                GlobalUtil.ShowHiltDialog("提示", "反应时长必须大于0且不超过3600！", "确定", (m, d) => { });
                 return;
             }

            // SingleReportTemplatePath 和 DoubleReportTemplatePath 校验
            if (string.IsNullOrEmpty(SingleReportTemplatePath) || string.IsNullOrEmpty(DoubleReportTemplatePath))
            {
                GlobalUtil.ShowHiltDialog("提示", "请选择单联和双联报告模板！", "确定", (m, d) => { });
                 return;
            }
            
             
             
             // 保存设置
             _configService.SetTestNum(testNumValue);
             _configService.SetSamplingVolumn((int)SamplingVolumn);
             _configService.SetCleanoutDuration(CleanoutDuration);
             _configService.SetReactionDuration(ReactionDuration);
             _configService.SetIsScanBarcode(IsScanBarcode);
             _configService.SetIsAutoPrintTicket(IsAutoPrintTicket);
             _configService.SetIsAutoPrintA4Report(IsAutoPrintA4Report);
            _configService.SetReportTemplatePath(SingleReportTemplatePath);
            _configService.SetDoubleReportTemplatePath(DoubleReportTemplatePath);
            _configService.SetPrinterName(SelectedPrinter);
           
            GlobalUtil.ShowHiltDialog("提示", "设置已保存！", "确定", (m, d) => { });
            WeakReferenceMessenger.Default.Send(new EventMsg<string>("") { What = EventWhat.WHAT_CHANGE_TEST_SETTINGS});
            return;
        }

    
    }

    public class PrinterInfo
    {
        public string Name { get; set; }
    }
}
