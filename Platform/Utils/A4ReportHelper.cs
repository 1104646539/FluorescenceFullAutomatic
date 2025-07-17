using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluorescenceFullAutomatic.Core.Config;
using FluorescenceFullAutomatic.Core.Model;
using FluorescenceFullAutomatic.Platform.Core.Config;
using FluorescenceFullAutomatic.Platform.Ex;
using FluorescenceFullAutomatic.Platform.Model;
using FluorescenceFullAutomatic.Platform.Sql;
using Serilog;
using Spire.Xls;
using Spire.Xls.Core;

namespace FluorescenceFullAutomatic.Platform.Utils
{
    public class A4ReportHelper
    {
        private static A4ReportHelper instance = null;

        public static A4ReportHelper getInstance()
        {
            if (instance == null)
            {
                instance = new A4ReportHelper();
            }
            return instance;
        }

        /// <summary>
        /// 生成报告
        /// </summary>
        /// <param name="tr"></param>
        private static Workbook createWookbook(TestResult tr)
        {
            Workbook workbook = null;
            if (tr == null || tr.Project == null)
            {
                return null;
            }

            try
            {
                int[] points = null;
                points = tr.Point?.Points;
                if (points == null)
                {
                    points = new int[1];
                }
                //是否是双联卡
                bool isDoubleCard = tr.Project.ProjectType == Project.Project_Type_Double;
                //point = um.point.point;
                //string point = "290,290,290,291,291,291,291,291,291,291,291,291,291,292,292,292,292,292,293,293,293,293,293,294,294,294,294,294,294,295,295,295,295,296,296,296,297,297,297,297,297,298,298,298,298,299,299,299,299,300,300,301,301,301,302,302,302,303,303,304,304,305,305,305,306,307,307,308,308,309,309,310,311,311,312,312,313,314,315,316,317,318,319,320,322,323,324,326,327,330,331,333,336,338,341,344,348,352,357,362,369,378,389,402,418,437,460,485,514,545,580,616,655,695,737,780,825,871,918,968,1019,1072,1127,1185,1244,1305,1368,1431,1495,1560,1622,1684,1744,1801,1855,1906,1954,1998,2038,2075,2111,2142,2172,2201,2228,2254,2280,2305,2329,2352,2377,2399,2421,2442,2462,2479,2494,2507,2516,2522,2523,2520,2510,2495,2474,2447,2413,2374,2331,2282,2229,2172,2112,2048,1982,1913,1842,1768,1694,1617,1539,1460,1380,1300,1219,1139,1061,985,913,845,782,723,671,624,582,546,514,488,465,446,430,416,405,395,387,380,374,369,364,360,356,353,349,347,344,342,340,338,336,334,332,331,329,328,326,325,324,323,321,320,319,318,318,317,316,315,315,314,313,313,312,312,311,311,311,310,310,310,309,309,309,309,309,308,308,308,308,308,308,308,308,309,309,309,309,309,310,310,311,312,312,312,313,314,314,315,316,317,318,319,320,321,322,324,325,326,328,330,331,333,336,338,340,343,345,348,352,355,359,363,368,373,379,387,395,405,417,432,448,469,492,518,548,582,621,663,710,761,819,882,952,1030,1115,1211,1317,1436,1569,1716,1881,2061,2254,2458,2670,2883,3091,3291,3480,3654,3809,3949,4070,4089,4089,4088,4088,4088,4089,4088,4088,4088,4088,4088,4089,4088,4088,4088,4088,4089,4088,4088,4088,4088,4088,4088,4088,4088,4088,4088,4088,4088,4089,4089,4088,4088,4088,4089,4088,4088,4088,4088,4089,4088,4074,4009,3906,3768,3601,3408,3194,2964,2723,2478,2236,2002,1782,1578,1396,1236,1096,978,878,793,722,664,615,574,540,511,486,465,448,432,418,406,396,387,379,372,365,359,354,349,345,341,337,334,331,329,326,324,322,320,318,317,315,314,313,312,310,309,308,307,306,306,305,304,304,303,302,302,301,301,301,300,300,299,299,299,299,298,298,298,298,298,298,298,298,298,298,298,298,298,298,299,298,299,299,299,299,299,300,300,300,301,301,301,301,302,302,302,302,302,303,303,303,303,303,304,304,304,304,304,305,305,305,305,306,306,306,306,306,307,307,307,307,307,307,307,308,307,307,308,308,308,308,308,308,308,308,308,308,308,308,308,308,308,308,308,308,308,308,308,308,308,308,308,308,309,308,309";


                string file = GlobalConfig.Instance.ReportTemplatePath;
                if (isDoubleCard)
                {
                    file = GlobalConfig.Instance.ReportDoubleTemplatePath;
                }
                workbook = new Workbook();
                workbook.Version = ExcelVersion.Version2016;
                workbook.LoadFromFile(file);
                Worksheet sheet = workbook.Worksheets[0];

                string patientName = "";
                string patientGender = "";
                string patientAge = "";
                string doctor = "";
                string barcode = "";
                string testTime = "";
                string hospitalName = "";

                int con = 0;
                int ljz = 100;
                string unit = "";
                int con2 = 0;
                int ljz2 = 40;
                string unit2 = "";
                string result = "";
                string testNum = "";

                string projectName = "";
                string projectName2 = "";
                string projectCode = "";

                if (tr.Patient != null)
                {
                    patientName = GlobalUtil.ToStringOrNull(tr.Patient.PatientName);
                    patientGender = GlobalUtil.ToStringOrNull(tr.Patient.PatientGender);
                    patientAge = GlobalUtil.ToStringOrNull(tr.Patient.PatientAge);
                    doctor = GlobalUtil.ToStringOrNull(tr.Patient.TestDoctor);
                }
                if (tr.Project != null)
                {
                    if (!isDoubleCard)
                    {
                        projectName = GlobalUtil.ToStringOrNull(tr.Project.ProjectName);
                        projectCode = GlobalUtil.ToStringOrNull(tr.Project.ProjectCode);
                    }
                    else
                    {
                        string[] ps = GlobalUtil.ToStringOrNull(tr.Project.ProjectName).Split('/');
                        if (ps.Length == 2)
                        {
                            projectName = ps[0];
                            projectName2 = ps[1];
                        }
                    }
                }
                barcode = GlobalUtil.ToStringOrNull(tr.Barcode);
                testNum = GlobalUtil.ToStringOrNull(tr.TestNum);
                if (tr.TestTime != null)
                {
                    testTime = tr.TestTime.GetDateTimeString();
                }

                hospitalName = GlobalUtil.ToStringOrNull(GlobalConfig.Instance.HospitalName);
                //如果是无效就设为0
                try
                {
                    con = Convert.ToInt32(Convert.ToDouble(GlobalUtil.ToStringOrNull(tr.Con, "0")));
                }
                catch (Exception ex)
                {
                    con = 0;
                }
                try
                {
                    con2 = Convert.ToInt32(
                        Convert.ToDouble(GlobalUtil.ToStringOrNull(tr.Con2, "0"))
                    );
                }
                catch (Exception ex)
                {
                    con2 = 0;
                }
                if (tr.Project != null)
                {
                    ljz = Convert.ToInt32(
                        Convert.ToDouble(
                            GlobalUtil.ToStringOrNull(tr.Project.ProjectLjz + "", "100")
                        )
                    );
                    unit = GlobalUtil.ToStringOrNull(tr.Project.ProjectUnit);
                    ljz2 = Convert.ToInt32(
                        Convert.ToDouble(
                            GlobalUtil.ToStringOrNull(tr.Project.ProjectLjz2 + "", "40")
                        )
                    );
                    unit2 = GlobalUtil.ToStringOrNull(tr.Project.ProjectUnit2);
                }
                result = GlobalUtil.ToStringOrNull(tr.TestVerdict);

                sheet.Range["N1"].Text = patientName;
                sheet.Range["N2"].Text = patientGender;
                sheet.Range["N3"].Text = patientAge;
                sheet.Range["N4"].Text = doctor;
                sheet.Range["N5"].Text = testTime;
                sheet.Range["N6"].Text = hospitalName;
                sheet.Range["N7"].Text = projectName;

                sheet.Range["O1"].NumberValue = ljz;
                sheet.Range["O2"].NumberValue = con;
                sheet.Range["O3"].Text = unit;
                sheet.Range["O4"].Text = result;
                sheet.Range["O5"].Text = testNum;
                sheet.Range["O6"].Text = projectCode;
                sheet.Range["O7"].Text = barcode;

                if (isDoubleCard)
                {
                    sheet.Range["P4"].Text = projectName2;
                    sheet.Range["P1"].NumberValue = ljz2;
                    sheet.Range["P2"].NumberValue = con2;
                    sheet.Range["P3"].Text = unit2;
                }

                for (int i = 0; i < points.Length; i++)
                {
                    sheet.Range["M" + (i + 1)].NumberValue = points[i];
                }

                Log.Information("生成成功");
            }
            catch (Exception ex)
            {
                workbook = null;
                Log.Information("生成失败 " + ex.Message);
            }

            return workbook;
        }

        /// <summary>
        /// 自动上传，自动打印
        /// </summary>
        /// <param name="tr"></param>
        /// <param name="autoPrint"></param>
        /// <param name="autoUploadFtp"></param>
        /// <param name="printerName"></param>
        public static void AutoExecReport(
            TestResult tr,
            bool autoPrint,
            bool autoUploadFtp,
            string printerName
        )
        {
            try
            {
                if (!autoPrint && !autoUploadFtp)
                    return;
                Workbook wb = createWookbook(tr);
                if (wb == null)
                {
                    Log.Information("生成报告失败");
                    return;
                }
                string filePath = getFilePath();
                string fileName = Path.GetFileName(filePath);
                wb.Worksheets[0].SaveToPdf(filePath);
                if (autoPrint)
                {
                    PrintReport(wb, fileName, printerName);
                }
                if (autoUploadFtp) { }
            }
            catch (Exception e)
            {
                Log.Information($"生成报告失败={e.Message}");
            }
        }

        /// <summary>
        /// 打印报告
        /// </summary>
        /// <param name="workbook"></param>
        /// <param name="fileName"></param>
        private static void PrintReport(
            TestResult tr,
            string printerName,
            Action<string> successAction = null,
            Action<string> failedAction = null
        )
        {
            Task.Run(() =>
            {
                try
                {
                    Workbook wb = createWookbook(tr);
                    if (wb == null)
                    {
                        Log.Information("生成报告失败");
                        return;
                    }
                    string filePath = getFilePath();
                    string fileName = Path.GetFileName(filePath);
                    wb.Worksheets[0].SaveToPdf(filePath);
                    PrintReport(wb, fileName, printerName, successAction, failedAction);
                }
                catch (Exception e)
                {
                    Log.Information($"生成报告失败={e.Message}");
                    failedAction?.Invoke($"生成报告失败={e.Message}");
                }
            });
           
        }

        /// <summary>
        /// 打印报告
        /// </summary>
        /// <param name="workbook"></param>
        /// <param name="fileName"></param>
        public static void PrintReport(
            List<TestResult> trs,
            string printerName,
            Action<string> successAction = null,
            Action<string> failedAction = null
        )
        {
            try
            {
                foreach (var item in trs)
                {
                    if (item.ResultState != ResultState.TestFinish)
                    {
                        failedAction?.Invoke("请选择已检测完成的结果");
                        return;
                    }
                }

                int index = 0;
                int successCount = 0;
                int failedCount = 0;
                Action printNext = null;
                string failedMsg = "";
                Action<string> singleSuccess = (msg) =>
                {
                    index++;
                    successCount++;
                    if (index < trs.Count)
                    {
                        printNext();
                    }
                    else
                    {
                        successAction?.Invoke(
                            $"打印成功{successCount}条,打印失败{failedCount}条。\n{failedMsg}"
                        );
                    }
                };

                Action<string> singleFailure = (err) =>
                {
                    failedMsg += $"ID={trs[index].Id},错误信息={err}";
                    index++;
                    failedCount++;

                    if (index < trs.Count)
                    {
                        printNext();
                    }
                    else
                    {
                        successAction?.Invoke(
                            $"打印成功{successCount}条,打印失败{failedCount}条。\n{failedMsg}"
                        );
                    }
                };

                printNext = () =>
                {
                    PrintReport(trs[index], printerName, singleSuccess, singleFailure);
                };

                printNext();
            }
            catch (Exception e)
            {
                Log.Information($"生成报告失败={e.Message}");
                failedAction?.Invoke($"生成报告失败: {e.Message}");
            }
        }

        /// <summary>
        /// 打印报告
        /// </summary>
        /// <param name="workbook"></param>
        /// <param name="fileName"></param>
        private static void PrintReport(
            Workbook workbook,
            string fileName,
            string printerName,
            Action<string> successAction = null,
            Action<string> failedAction = null
        )
        {
            Task.Run(() =>
            {
                if (workbook != null)
                {
                    //workbook.PrintDocument.DocumentName = fileName;
                    if (!string.IsNullOrEmpty(printerName))
                    {
                        workbook.PrintDocument.PrinterSettings.PrinterName = printerName;
                    }
                    try
                    {
                        workbook.PrintDocument.Print();
                        Log.Information($"打印报告中");
                        successAction?.Invoke("");
                    }
                    catch (Exception ex)
                    {
                        Log.Information($"打印报告失败 {ex.Message}");
                        failedAction?.Invoke($"打印报告失败 {ex.Message}");
                    }
                }
                else
                {
                    failedAction?.Invoke("生成失败");
                }
            });
        }

        /// <summary>
        /// 生成文件路径
        /// </summary>
        /// <param name="um"></param>
        /// <returns></returns>
        public static string getFilePath()
        {
            return SystemGlobal.Cache_Path
                + @"/report/"
                + DateTime.Now.GetDateTimeString3()
                + ".pdf";
        }
    }
}
