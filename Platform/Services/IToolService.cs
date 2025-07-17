using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using FluorescenceFullAutomatic.Platform.Model;
using FluorescenceFullAutomatic.Platform.Utils;

namespace FluorescenceFullAutomatic.Platform.Services
{
    public interface IToolService
    {
        List<string> GetPrinters();
        string CalcTC(double t, double c);
        int CalcCon(string t, string c, string tc, Project project);
        TestResult CalcTestResult(TestResult testResult);
        string GetString(string key);
        
        /// <summary>
        /// 隐藏任务栏
        /// </summary>
        void HideTaskBar();
        
        /// <summary>
        /// 显示任务栏
        /// </summary>
        void ShowTaskBar();
        
        /// <summary>
        /// 添加任务栏状态变化监听器
        /// </summary>
        /// <param name="listener">监听器回调，0为隐藏，1为显示</param>
        void AddTaskBarStateChangedListener(Action<int> listener);
        
        /// <summary>
        /// 移除任务栏状态变化监听器
        /// </summary>
        /// <param name="listener">要移除的监听器回调</param>
        void RemoveTaskBarStateChangedListener(Action<int> listener);
    }

    public class ToolService : IToolService
    {
        private readonly List<Action<int>> _taskBarStateChangedListeners = new List<Action<int>>();
        
        // Windows API 常量和声明
        private const int SW_HIDE = 0;
        private const int SW_SHOW = 5;
        
        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        
        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        
        // 任务栏的类名
        private const string TaskbarClassName = "Shell_TrayWnd";
        
        // 应用程序栏消息常量
        private const int ABM_SETSTATE = 0x0000000A;
        private const int ABS_AUTOHIDE = 0x0000001;
        private const int ABS_ALWAYSONTOP = 0x0000002;
        
        [DllImport("shell32.dll")]
        private static extern IntPtr SHAppBarMessage(int dwMessage, ref APPBARDATA pData);
        
        [StructLayout(LayoutKind.Sequential)]
        private struct APPBARDATA
        {
            public int cbSize;
            public IntPtr hWnd;
            public int uCallbackMessage;
            public int uEdge;
            public RECT rc;
            public IntPtr lParam;
        }
        
        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }
        
        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);
        
        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
        
        
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
        
        /// <summary>
        /// 隐藏任务栏并释放其占用的屏幕空间
        /// </summary>
        public void HideTaskBar()
        {
            try
            {
                // 查找任务栏窗口
                IntPtr taskbarHandle = FindWindow(TaskbarClassName, null);
                
                if (taskbarHandle != IntPtr.Zero)
                {
                    // 首先隐藏任务栏
                    ShowWindow(taskbarHandle, SW_HIDE);
                    
                    // 设置任务栏为自动隐藏模式，这会释放它占用的空间
                    APPBARDATA abd = new APPBARDATA();
                    abd.cbSize = Marshal.SizeOf(typeof(APPBARDATA));
                    abd.hWnd = taskbarHandle;
                    abd.lParam = (IntPtr)ABS_AUTOHIDE;
                    SHAppBarMessage(ABM_SETSTATE, ref abd);
                    
                    // 通知任务栏状态变化
                    NotifyTaskBarStateChanged(0);
                    
                    // 记录日志
                    Serilog.Log.Information("任务栏已隐藏并释放屏幕空间");
                }
            }
            catch (Exception ex)
            {
                // 记录异常，但不抛出
                Serilog.Log.Error($"隐藏任务栏时发生错误: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 显示任务栏并恢复其占用的屏幕空间
        /// </summary>
        public void ShowTaskBar()
        {
            try
            {
                // 查找任务栏窗口
                IntPtr taskbarHandle = FindWindow(TaskbarClassName, null);
                
                if (taskbarHandle != IntPtr.Zero)
                {
                    // 设置任务栏为正常模式（不自动隐藏）
                    APPBARDATA abd = new APPBARDATA();
                    abd.cbSize = Marshal.SizeOf(typeof(APPBARDATA));
                    abd.hWnd = taskbarHandle;
                    abd.lParam = (IntPtr)ABS_ALWAYSONTOP;
                    SHAppBarMessage(ABM_SETSTATE, ref abd);
                    
                    // 显示任务栏
                    ShowWindow(taskbarHandle, SW_SHOW);
                    
                    // 通知任务栏状态变化
                    NotifyTaskBarStateChanged(1);
                    
                    // 记录日志
                    Serilog.Log.Information("任务栏已显示并恢复屏幕空间");
                }
            }
            catch (Exception ex)
            {
                // 记录异常，但不抛出
                Serilog.Log.Error($"显示任务栏时发生错误: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 添加任务栏状态变化监听器
        /// </summary>
        /// <param name="listener">监听器回调，0为隐藏，1为显示</param>
        public void AddTaskBarStateChangedListener(Action<int> listener)
        {
            if (listener != null && !_taskBarStateChangedListeners.Contains(listener))
            {
                _taskBarStateChangedListeners.Add(listener);
            }
        }
        
        /// <summary>
        /// 移除任务栏状态变化监听器
        /// </summary>
        /// <param name="listener">要移除的监听器回调</param>
        public void RemoveTaskBarStateChangedListener(Action<int> listener)
        {
            if (listener != null)
            {
                _taskBarStateChangedListeners.Remove(listener);
            }
        }
        
        /// <summary>
        /// 通知所有监听器任务栏状态变化
        /// </summary>
        /// <param name="state">任务栏状态，0为隐藏，1为显示</param>
        private void NotifyTaskBarStateChanged(int state)
        {
            foreach (var listener in _taskBarStateChangedListeners.ToList())
            {
                try
                {
                    listener?.Invoke(state);
                }
                catch (Exception ex)
                {
                    // 记录异常，但不中断其他监听器的通知
                    Serilog.Log.Error($"通知任务栏状态变化时发生错误: {ex.Message}");
                }
            }
        }
    }
}
