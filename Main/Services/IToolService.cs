using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluorescenceFullAutomatic.Model;
using FluorescenceFullAutomatic.Utils;

namespace FluorescenceFullAutomatic.Services
{
    public interface IToolService
    {
        List<string> GetPrinters();
        string CalcTC(double t, double c);
        int CalcCon(string t, string c, string tc, Project project);
        TestResult CalcTestResult(TestResult testResult);
        string GetString(string key);
    }

    public class ToolRepository : IToolService
    {
        public string GetString(string key)
        {
            return GlobalUtil.GetString(key);
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
            if (con < 0)
            {
                con = 0;
            }

            return con;
        }


        public string CalcTC(double t, double c)
        {
            if (c <= 0 || t <= 0)
            {
                return "0";
            }
            return (t / c).ToString("0.000");
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
            if (con > project.ConMax)
            {
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

        public List<string> GetPrinters()
        {
            return PrinterSettings.InstalledPrinters.Cast<string>().ToList();

        }
    }
}
