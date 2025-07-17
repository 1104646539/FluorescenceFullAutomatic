using FluorescenceFullAutomatic.Platform.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestMain.Repositorys
{
    public class FakeConfigRepository : IConfigService
    {
        private int _barcodePortBaudRate;
        private string _barcodePortName;
        private int _cleanoutDuration;
        private bool _debugMode;
        private string _printerName;
        private string _reportDoubleTemplatePath;
        private string _reportTemplatePath;
        private bool _isAutoPrintA4Report;
        private bool _isAutoPrintTicket;
        private bool _isScanBarcode;
        private int _mainPortBaudRate;
        private string _mainPortName;
        private int _reactionDuration;
        private int _samplingVolume;
        private int _testNum;
        private int _ticketPortBaudRate;
        private string _ticketPortName;

        public void AddDebugModeChangedListener(Action<bool> listener)
        {
            
        }

        public int BarcodePortBaudRate()
        {
            return _barcodePortBaudRate;
        }

        public string BarcodePortName()
        {
            return _barcodePortName;
        }

        public int CleanoutDuration()
        {
            return _cleanoutDuration;
        }

        public bool GetDebugMode()
        {
            return _debugMode;
        }

        public string GetPrinterName()
        {
            return _printerName;
        }

        public string GetReportDoubleTemplatePath()
        {
            return _reportDoubleTemplatePath;
        }

        public string GetReportTemplatePath()
        {
            return _reportTemplatePath;
        }

        public bool IsAutoPrintA4Report()
        {
            return _isAutoPrintA4Report;
        }

        public bool IsAutoPrintTicket()
        {
            return _isAutoPrintTicket;
        }

        public bool IsScanBarcode()
        {
            return _isScanBarcode;
        }

        public int MainPortBaudRate()
        {
            return _mainPortBaudRate;
        }

        public string MainPortName()
        {
            return _mainPortName;
        }

        public int ReactionDuration()
        {
            return _reactionDuration;
        }

        public void RemoveDebugModeChangedListener(Action<bool> listener)
        {
            
        }

        public int SamplingVolume()
        {
            return _samplingVolume;
        }

        public void SetCleanoutDuration(int duration)
        {
            _cleanoutDuration = duration;
        }

        public void SetDebugModeChnage()
        {
            _debugMode = !_debugMode;
        }

        public void SetDoubleReportTemplatePath(string path)
        {
            _reportDoubleTemplatePath = path;
        }

        public void SetIsAutoPrintA4Report(bool value)
        {
            _isAutoPrintA4Report = value;
        }

        public void SetIsAutoPrintTicket(bool value)
        {
            _isAutoPrintTicket = value;
        }

        public void SetIsScanBarcode(bool value)
        {
            _isScanBarcode = value;
        }

        public void SetPrinterName(string printerName)
        {
            _printerName = printerName;
        }

        public void SetReactionDuration(int duration)
        {
            _reactionDuration = duration;
        }

        public void SetReportTemplatePath(string path)
        {
            _reportTemplatePath = path;
        }

        public void SetSamplingVolume(int volumn)
        {
            _samplingVolume = volumn;
        }

        public void SetTestNum(int num)
        {
            _testNum = num;
        }

        public int TestNum()
        {
            return _testNum;
        }

        public int TestNumIncrement()
        {
            return ++_testNum;
        }

        public int TicketPortBaudRate()
        {
            return _ticketPortBaudRate;
        }

        public string TicketPortName()
        {
            return _ticketPortName;
        }
    }
}
