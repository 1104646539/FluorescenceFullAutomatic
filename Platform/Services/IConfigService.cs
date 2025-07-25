﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluorescenceFullAutomatic.Platform.Core.Config;
using FluorescenceFullAutomatic.Platform.Sql;

namespace FluorescenceFullAutomatic.Platform.Services
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

        string BarcodePortName();
        public int BarcodePortBaudRate();

        string TicketPortName();
        public int TicketPortBaudRate();
        public string MainPortName();
        public int MainPortBaudRate();

        int ReactionDuration();
        void SetReactionDuration(int duration);

        int TestNum();
        void SetTestNum(int num);

        int SamplingVolume();
        void SetSamplingVolume(int volumn);

        string GetPrinterName();
        void SetPrinterName(string printerName);

        string GetReportTemplatePath();
        string GetReportDoubleTemplatePath();
        void SetReportTemplatePath(string path);
        void SetDoubleReportTemplatePath(string path);

        void SetDebugModeChnage();
        bool GetDebugMode();

        bool ClearReactionArea();

        void SetClearReactionArea(bool clear);

        // 新增：添加和移除DebugMode变化监听器
        void AddDebugModeChangedListener(Action<bool> listener);
        void RemoveDebugModeChangedListener(Action<bool> listener);
    }
    public class ConfigService : IConfigService
    {
        private readonly List<Action<bool>> _debugModeChangedListeners = new List<Action<bool>>();
        public ConfigService()
        {
        }

        public int BarcodePortBaudRate()
        {
            return GlobalConfig.Instance.BarcodePortBaudRate;
        }

        public string BarcodePortName()
        {
            return GlobalConfig.Instance.BarcodePortName;
        }

        public int CleanoutDuration()
        {
            return GlobalConfig.Instance.CleanoutDuration;
        }

        public bool GetDebugMode()
        {
            return GlobalConfig.Instance.IsDebug;
        }

        public string GetPrinterName()
        {
            return GlobalConfig.Instance.PrinterName;
        }

        public string GetReportDoubleTemplatePath()
        {
            return GlobalConfig.Instance.ReportDoubleTemplatePath;
        }

        public string GetReportTemplatePath()
        {
            return GlobalConfig.Instance.ReportTemplatePath;
        }

        public bool IsAutoPrintA4Report()
        {
            return GlobalConfig.Instance.AutoPrintA4Report;
        }

        public bool IsAutoPrintTicket()
        {
            return GlobalConfig.Instance.AutoPrintTicket;
        }

        public bool IsScanBarcode()
        {
            return GlobalConfig.Instance.ScanBarcode;
        }

        public int MainPortBaudRate()
        {
            return GlobalConfig.Instance.MainPortBaudRate;
        }

        public string MainPortName()
        {
            return GlobalConfig.Instance.MainPortName;
        }

        public int ReactionDuration()
        {
            return GlobalConfig.Instance.ReactionDuration;
        }

        public int SamplingVolume()
        {
            return GlobalConfig.Instance.SamplingVolume;
        }

        public void SetCleanoutDuration(int duration)
        {
            GlobalConfig.Instance.CleanoutDuration = duration;
        }

        public void SetDebugModeChnage()
        {
            GlobalConfig.Instance.IsDebug = !GlobalConfig.Instance.IsDebug;
            // 触发所有监听器
            foreach (var listener in _debugModeChangedListeners.ToList())
            {
                try
                {
                    listener?.Invoke(GlobalConfig.Instance.IsDebug);
                }
                catch (Exception ex)
                {
                    // 可以记录日志
                }
            }
        }

        public void AddDebugModeChangedListener(Action<bool> listener)
        {
            if (listener != null && !_debugModeChangedListeners.Contains(listener))
            {
                _debugModeChangedListeners.Add(listener);
            }
        }
        public void RemoveDebugModeChangedListener(Action<bool> listener)
        {
            if (listener != null)
            {
                _debugModeChangedListeners.Remove(listener);
            }
        }

        public void SetDoubleReportTemplatePath(string path)
        {
            GlobalConfig.Instance.ReportDoubleTemplatePath = path;
        }

        public void SetIsAutoPrintA4Report(bool value)
        {
            GlobalConfig.Instance.AutoPrintA4Report = value;
        }

        public void SetIsAutoPrintTicket(bool value)
        {
            GlobalConfig.Instance.AutoPrintTicket = value;
        }

        public void SetIsScanBarcode(bool value)
        {
            GlobalConfig.Instance.ScanBarcode = value;
        }

        public void SetPrinterName(string printerName)
        {
            GlobalConfig.Instance.PrinterName = printerName;
        }

        public void SetReactionDuration(int duration)
        {
            GlobalConfig.Instance.ReactionDuration = duration;
        }

        public void SetReportTemplatePath(string path)
        {
            GlobalConfig.Instance.ReportTemplatePath = path;
        }

        public void SetSamplingVolume(int volumn)
        {
            GlobalConfig.Instance.SamplingVolume = volumn;
        }

        public void SetTestNum(int num)
        {
            GlobalConfig.Instance.TestNum = num;
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

        public int TicketPortBaudRate()
        {
            return GlobalConfig.Instance.TicketPortBaudRate;
        }

        public string TicketPortName()
        {
            return GlobalConfig.Instance.TicketPortName;
        }

        public bool ClearReactionArea()
        {
            return GlobalConfig.Instance.ClearReactionArea == 0;
        }

        public void SetClearReactionArea(bool clear)
        {
            GlobalConfig.Instance.ClearReactionArea = clear ? 0 : 1;
        }
    }
}
