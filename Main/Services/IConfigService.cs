using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Web.ApplicationServices;
using System.Windows;
using FluorescenceFullAutomatic.Config;
using FluorescenceFullAutomatic.Model;
using FluorescenceFullAutomatic.Sql;
using FluorescenceFullAutomatic.Utils;
using FluorescenceFullAutomatic.ViewModels;

namespace FluorescenceFullAutomatic.Services
{
    public interface IConfigService
    {
        int TestNumIncrement();
        
        int CleanoutDuration();
        void SetCleanoutDuration(int duration);
        
        bool IsScanBarcode();
        void SetIsScanBarcode(bool value);
        
        bool IsAutoPrintTicket();
        void SetIsAutoPrintTicket(bool value);
        
        bool IsAutoPrintA4Report();
        void SetIsAutoPrintA4Report(bool value);
        
        Project GetProject(string cardQRCode);
        TestResult CalcTestResult(TestResult testResult);

        string GetString(string key);

        string BarcodePortName();
        public int BarcodePortBaudRate();
        
        string TicketPortName();
        public int TicketPortBaudRate();
        public string MainPortName();
        public int MainPortBaudRate();

        int ReactionDuration();
        void SetReactionDuration(int duration);
        string CalcTC(double t, double c);
        int CalcCon(string t, string c, string tc, Project project);

        int TestNum();
        void SetTestNum(int num);

        int SamplingVolumn();
        void SetSamplingVolumn(int volumn);

        string GetPrinterName();
        void SetPrinterName(string printerName);
        
        string GetReportTemplatePath();
        string GetReportDoubleTemplatePath();
        void SetReportTemplatePath(string path);
        void SetDoubleReportTemplatePath(string path);

        void SetDebugModeChnage();
        bool GetDebugMode();

        List<string> GetPrinterInfos();
    }

    public class ConfigService : IConfigService
    {
        private readonly SerialPortHelper _serialPortUtil;

        public ConfigService()
        {
            _serialPortUtil = SerialPortHelper.Instance;
        }

        public TestResult CalcTestResult(TestResult testResult)
        {
            int t = 0;
            int c = 0;
            int.TryParse(testResult.T, out t);
            int.TryParse(testResult.C, out c);
            testResult.Tc = CalcTC(t, c);
            Project project = testResult.Project;

            int con = CalcCon(testResult.T, testResult.C, testResult.Tc, project);
            if (con > project.ConMax) { 
                con = project.ConMax;
            }
            testResult.Con = con.ToString();
            testResult.Result = CalcResult(
                testResult.T,
                testResult.C,
                testResult.Con,
                project.ProjectLjz
            );
            //双联卡
            if (project.TestType == 1)
            {
                int t2 = 0;
                int c2 = 0;
                int.TryParse(testResult.T2, out t2);
                int.TryParse(testResult.C2, out c2);
                testResult.Tc2 = CalcTC(t2, c2);

                int con2 = CalcCon(testResult.T2, testResult.C2, testResult.Tc2, project);
                if (con2 > project.ConMax2)
                {
                    con2 = project.ConMax2;
                }
                testResult.Con2 = con2.ToString();
                testResult.Result2 = CalcResult(
                    testResult.T2,
                    testResult.C2,
                    testResult.Con2,
                    project.ProjectLjz2
                );
            }

            testResult.TestVerdict = CalcTestVeridict(
                testResult.Result,
                testResult.Result2,
                project.TestType == 1
            );
            testResult.TestTime = DateTime.Now;

            return testResult;
        }

        private string CalcTestVeridict(string result, string result2, bool isDouble)
        {
            if (isDouble)
            {
                if (
                    result == GlobalUtil.GetString(Keys.ResultNegative)
                    && result2 == GlobalUtil.GetString(Keys.ResultNegative)
                )
                {
                    return GlobalUtil.GetString(Keys.ResultNegative);
                }
                else if (
                    result == GlobalUtil.GetString(Keys.ResultPositive)
                    && result2 == GlobalUtil.GetString(Keys.ResultPositive)
                )
                {
                    return GlobalUtil.GetString(Keys.ResultPositive);
                }
                else
                {
                    return GlobalUtil.GetString(Keys.ResultInvalid);
                }
            }
            else
            {
                return result;
            }
        }

        private string CalcResult(string t, string c, string con, int projectLjz)
        {
            if (int.Parse(c) <= 10)
            {
                return GlobalUtil.GetString(Keys.ResultInvalid);
            }
            return double.Parse(con) >= projectLjz
                ? GlobalUtil.GetString(Keys.ResultPositive)
                : GlobalUtil.GetString(Keys.ResultNegative);
        }

        /// <summary>
        /// 根据tc值计算浓度
        /// </summary>
        /// <param name="tc"></param>
        /// <param name="projectLjz"></param>
        /// <returns></returns>
        public int CalcCon(string t, string c, string tc, Project project)
        {
            //project.A1 = -1.818473100987009;
            //project.A2 = 1.2532061054101633;
            //project.X0 = 374.24683086385727;
            //project.P = 564.4612105104441;
            //project.A1 = -1.818473100987009;
            //project.A2 = 564.4612105104441;
            //project.X0 = 374.24683086385727;
            //project.P = 1.2532061054101633;
            if (project == null) return 0;
            double a1 = project.A1;
            double a2 = project.A2;
            double X0 = project.X0;
            double p = project.P;
            double dr = double.Parse(tc);
            double cValue = double.Parse(c);
            if (cValue <= 10)
            {
                return 0;
            }
           
            int con = (int)(X0 * Math.Pow(((a2 - a1) / (a2 - dr)) - 1, (1 / p)));
            if (con < 0) {
                con = 0;
            }

            return con;
        }

        /// <summary>
        /// 根据检测卡二维码获取项目
        /// </summary>
        /// <param name="cardQRCode"></param>
        /// <returns></returns>
        Project IConfigService.GetProject(string cardQRCode)
        {
            // 解析二维码
            if (string.IsNullOrEmpty(cardQRCode))
            {
                return null;
            }
           
            Project project = SqlHelper.getInstance().GetProjectForQRCode(cardQRCode);
            
            return project;
        }


        public int TestNum()
        {
            return GlobalConfig.Instance.TestNum;
        }

        public int TestNumIncrement()
        {
            int testNum = TestNum();
            int temp = testNum++;
            GlobalConfig.Instance.TestNum = testNum;
            return temp;
        }
        public string CalcTC(double t, double c)
        {
            if (c <= 0 || t <= 0)
            {
                return "0";
            }
            return (t / c).ToString("0.000");
        }
        public bool IsScanBarcode()
        {
            return GlobalConfig.Instance.ScanBarcode;
        }

        public bool IsAutoPrintTicket()
        {
            return GlobalConfig.Instance.AutoPrintTicket;
        }

        public bool IsAutoPrintA4Report()
        {
            return GlobalConfig.Instance.AutoPrintA4Report;
        }

        public int CleanoutDuration()
        {
            return GlobalConfig.Instance.CleanoutDuration;
        }

        public int SamplingVolumn()
        {
            return GlobalConfig.Instance.SamplingVolume;
        }

        public string BarcodePortName()
        {
            return GlobalConfig.Instance.BarcodePortName;
        }

        public int BarcodePortBaudRate()
        {
            return GlobalConfig.Instance.BarcodePortBaudRate;
        }

        public string MainPortName()
        {
            return GlobalConfig.Instance.MainPortName;
        }

        public int MainPortBaudRate()
        {
            return GlobalConfig.Instance.MainPortBaudRate;
        }

        public int ReactionDuration()
        {
            return GlobalConfig.Instance.ReactionDuration;
        }

        public void SetReactionDuration(int duration)
        {
            GlobalConfig.Instance.ReactionDuration = duration;
        }

        public string GetString(string key)
        {
            return GlobalUtil.GetString(key);
        }

        public void SetTestNum(int num)
        {
            GlobalConfig.Instance.TestNum = num;
        }

        public void SetCleanoutDuration(int duration)
        {
            GlobalConfig.Instance.CleanoutDuration = duration;
        }

        public void SetIsScanBarcode(bool value)
        {
            GlobalConfig.Instance.ScanBarcode = value;
        }

        public void SetIsAutoPrintTicket(bool value)
        {
            GlobalConfig.Instance.AutoPrintTicket = value;
        }

        public void SetIsAutoPrintA4Report(bool value)
        {
            GlobalConfig.Instance.AutoPrintA4Report = value;
        }

        public void SetSamplingVolumn(int volumn)
        {
            GlobalConfig.Instance.SamplingVolume = volumn;
        }

        public string GetPrinterName()
        {
           return GlobalConfig.Instance.PrinterName;
        }

        public void SetPrinterName(string printerName)
        {
            GlobalConfig.Instance.PrinterName = printerName;
        }

        public void SetReportTemplatePath(string path)
        {
            GlobalConfig.Instance.ReportTemplatePath = path;
        }

        public void SetDoubleReportTemplatePath(string path)
        {
            GlobalConfig.Instance.ReportDoubleTemplatePath = path;
        }

        public string GetReportTemplatePath()
        {
            return GlobalConfig.Instance.ReportTemplatePath;
        }

        public string GetReportDoubleTemplatePath()
        {
            return GlobalConfig.Instance.ReportDoubleTemplatePath;
        }

        public List<string> GetPrinterInfos()
        {
            return PrinterSettings.InstalledPrinters.Cast<string>().ToList();
        }

        public string TicketPortName()
        {
            return GlobalConfig.Instance.TicketPortName;
        }

        public int TicketPortBaudRate()
        {
            return GlobalConfig.Instance.TicketPortBaudRate;
        }

        public void SetDebugModeChnage()
        {
            GlobalConfig.Instance.IsDebug = !GlobalConfig.Instance.IsDebug;
        }

        public bool GetDebugMode()
        {
            return GlobalConfig.Instance.IsDebug;
        }
    }
}
