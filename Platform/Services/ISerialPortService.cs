using FluorescenceFullAutomatic.Platform.Core.Config;
using FluorescenceFullAutomatic.Platform.Model;
using FluorescenceFullAutomatic.Platform.Utils;
using System;
using System.Windows.Navigation;

namespace FluorescenceFullAutomatic.Platform.Services
{
    public interface ISerialPortService
    {
        //通讯串口
        void AddSerialPortConnectReceived(Action<string> action);
        void RemoveSerialPortConnectReceived(Action<string> action);

        void AddSerialPortExceptionReceived(Action<string> action);
        void RemoveSerialPortExceptionReceived(Action<string> action);
        void Connect(string portName, int baudRate);
        void Disconnect();

        //条码串口
        void AddBarcodeConnectReceived(Action<string> action);
        void RemoveBarcodeConnectReceived(Action<string> action);

        void AddBarcodeExceptionReceived(Action<string> action);
        void RemoveBarcodeExceptionReceived(Action<string> action);

        void ConnectBarcode(string portName, int baudRate);
        void DisconnectBarcode();

        //热敏打印机串口
        void AddTicketConnectReceived(Action<string> action);
        void RemoveTicketConnectReceived(Action<string> action);

        void AddTicketExceptionReceived(Action<string> action);
        void RemoveTicketExceptionReceived(Action<string> action);

        void ConnectTicket(string portName, int baudRate);
        void DisconnectTicket();




        // 自检相关
        void GetSelfInspectionState(bool clearReactionArea);
        
        // 仪器相关
        void GetMachineState();
        
        // 清洗液相关
        void GetCleanoutFluid();
        
        // 样本架相关
        void GetSampleShelf();
        void MoveSampleShelf(int pos);
        
        // 样本操作相关
        void MoveSample( int pos);
        void Sampling(string type, int volume);
        void CleanoutSamplingProbe(int duration);
        void AddingSample(int volume, string type);
        void Drainage();
        
        // 卡片操作相关
        void PushCard();
        void MoveReactionArea(int x, int y);
        void Test(int x, int y, string cardType, string testType, string scanStart, 
                 string scanEnd, string peakWidth, string peakDistance);
        
        // 温度相关
        void GetReactionTemp(string temp);
        
        // 反应区操作
        void ClearReactionArea();
        
        // 电机控制
        void Motor(string motor, string direction, string value);
        
        // 系统相关
        void ResetParams();
        void Update();
        
        // 样本处理相关
        void Squeezing(string type);
        void Pierced(string type);

        void GetVersion();
        void Shutdown();
        public bool IsOpen();
        // 事件注册
        void AddReceiveData(IReceiveData receiveData);
        void RemoveReceiveData(IReceiveData receiveData);

        void AddConnectStateListener(Action<string> result);

        void AddOriginReceiveDataListener(Action<string> result);
        void RemoveOriginReceiveDataListener(Action<string> result);

        void AddOriginSendDataListener(Action<string> result);
        void RemoveOriginSendDataListener(Action<string> result);
          

        void ScanBarcode();
        void StopScanBarcode();
        void AddScanSuccessListener(Action<string> onScanSuccess);
        void AddScanFailedListener(Action<string> onScanFailed);

        string GetMainPortName();

    }

    public class SerialPortService : ISerialPortService
    {
        private readonly SerialPortHelper _serialPortHelper;

        private readonly  ReactionAreaQueue _reactionAreaQueue;

        private readonly BarcodeHelper _barcodeHelper;
        private readonly TicketReportHelper _ticketReportHelper;
        public void Enqueue(ReactionAreaItem item)
        {
           _reactionAreaQueue.Enqueue(item);
        }

     
        public void OnAddDequeue(Func<ReactionAreaItem, bool> onDequeue)
        {
            _reactionAreaQueue._dequeueCallback += onDequeue;

        }


        public void OnRemoveDequeue(Func<ReactionAreaItem, bool> onDequeue)
        {
           _reactionAreaQueue._dequeueCallback -= onDequeue;

        }
   
        public SerialPortService()
        {
            _serialPortHelper = SerialPortHelper.Instance;
            _barcodeHelper = BarcodeHelper.Instance;
            _ticketReportHelper = TicketReportHelper.Instance;
            //_reactionAreaQueue.SetEnqueueDuration(10);
        }

        public void Connect(string portName, int baudRate)
        {
            _serialPortHelper.Connect(portName, baudRate);
        }

        public void Disconnect()
        {
            _serialPortHelper.Disconnect();
        }

        public void GetSelfInspectionState(bool clearReactionArea)
        {
            _serialPortHelper.GetSelfInspectionState(clearReactionArea);
        }

        public void GetMachineState()
        {
            _serialPortHelper.GetMachineState();
        }

        public void GetCleanoutFluid()
        {
            _serialPortHelper.GetCleanoutFluid();
        }

        public void GetSampleShelf()
        {
            _serialPortHelper.SampleShelf();
        }

        public void MoveSampleShelf(int pos)
        {
            _serialPortHelper.MoveSampleShelf(pos);
        }

        public void MoveSample( int pos)
        {
            _serialPortHelper.MoveSample(pos);
        }

        public void Sampling(string type, int volume)
        {
            _serialPortHelper.Sampling(type, volume);
        }

        public void CleanoutSamplingProbe(int duration)
        {
            _serialPortHelper.CleanoutSamplingProbe(duration);
        }

        public void AddingSample(int volume, string type)
        {
            _serialPortHelper.AddingSample(volume, type);
        }

        public void Drainage()
        {
            _serialPortHelper.Drainage();
        }

        public void PushCard()
        {
            _serialPortHelper.PushCard();
        }

        public void MoveReactionArea(int x, int y)
        {
            _serialPortHelper.MoveReactionArea(y, x);
        }

        public void Test(int x, int y, string cardType, string testType, string scanStart, 
                        string scanEnd, string peakWidth, string peakDistance)
        {
            _serialPortHelper.Test(y, x, cardType, testType, scanStart, scanEnd, peakWidth, peakDistance);
        }

        public void GetReactionTemp(string temp)
        {
            _serialPortHelper.GetReactionTemp(temp);
        }

        public void ClearReactionArea()
        {
            _serialPortHelper.ClearReactionArea();
        }

        public void Motor(string motor, string direction, string value)
        {
            _serialPortHelper.Motor(motor, direction, value);
        }

        public void ResetParams()
        {
            _serialPortHelper.ResetParams();
        }

        public void Update()
        {
            _serialPortHelper.Update();
        }

        public void Squeezing(string type)
        {
            _serialPortHelper.Squeezing(type);
        }

        public void Pierced(string type)
        {
            _serialPortHelper.Pierced(type);
        }

        public void AddReceiveData(IReceiveData receiveData)
        {
            _serialPortHelper.AddReceiveData(receiveData);
        }

        public void RemoveReceiveData(IReceiveData receiveData)
        {
            _serialPortHelper.RemoveReceiveData(receiveData);
        }

        public bool IsOpen()
        {
            return _serialPortHelper.IsOpen();
        }

        public void AddConnectStateListener(Action<string> result)
        {
            _serialPortHelper.SerialPortConnectReceived += result;
        }

       

        public void ScanBarcode()
        {
            _barcodeHelper.StartScan();
        }

        public void StopScanBarcode()
        {
            _barcodeHelper.StopScan();
        }

        public void AddScanSuccessListener(Action<string> onScanSuccess)
        {
            _barcodeHelper.ScanSuccess += onScanSuccess;
        }

        public void AddScanFailedListener(Action<string> onScanFailed)
        {
            _barcodeHelper.ScanFailed += onScanFailed;
        }

        public void AddOriginReceiveDataListener(Action<string> result)
        {
            _serialPortHelper.SerialPortOriginDataReceived += result;
        }

        public void RemoveOriginReceiveDataListener(Action<string> result)
        {
            _serialPortHelper.SerialPortOriginDataReceived -= result;

        }

        public void AddOriginSendDataListener(Action<string> result)
        {
            _serialPortHelper.SerialPortOriginDataSend += result;
        }

      
        public void RemoveOriginSendDataListener(Action<string> result)
        {
            _serialPortHelper.SerialPortOriginDataSend -= result;
        }

        public string GetMainPortName()
        {
            return GlobalConfig.Instance.MainPortName;
        }

        public void GetVersion()
        {
            _serialPortHelper.GetVersion();
        }

        public void Shutdown()
        {
            _serialPortHelper.Shutdown();
        }
        
        public void AddSerialPortConnectReceived(Action<string> action)
        {
            _serialPortHelper.SerialPortConnectReceived += action;
        }


        public void RemoveSerialPortConnectReceived(Action<string> action)
        {
            _serialPortHelper.SerialPortConnectReceived -= action;
        }

        public void AddSerialPortExceptionReceived(Action<string> action)
        {
            _serialPortHelper.SerialPortExceptionReceived += action;
        }

        public void RemoveSerialPortExceptionReceived(Action<string> action)
        {
            _serialPortHelper.SerialPortExceptionReceived -= action;
        }

        public void AddBarcodeConnectReceived(Action<string> action)
        {
            _barcodeHelper.SerialPortConnectReceived += action;
        }

        public void RemoveBarcodeConnectReceived(Action<string> action)
        {
            _barcodeHelper.SerialPortConnectReceived -= action;
        }

        public void AddBarcodeExceptionReceived(Action<string> action)
        {
            _barcodeHelper.SerialPortExceptionReceived += action;
        }

        public void RemoveBarcodeExceptionReceived(Action<string> action)
        {
            _barcodeHelper.SerialPortExceptionReceived -= action;
        }

        public void ConnectBarcode(string portName, int baudRate)
        {
            _barcodeHelper.Connect(portName, baudRate);
        }

        public void DisconnectBarcode()
        {
            _barcodeHelper.Disconnect();
        }

        public void AddTicketConnectReceived(Action<string> action)
        {
            _ticketReportHelper.SerialPortConnectReceived += action;
        }

        public void RemoveTicketConnectReceived(Action<string> action)
        {
            _ticketReportHelper.SerialPortConnectReceived -= action;
        }

        public void AddTicketExceptionReceived(Action<string> action)
        {
            _ticketReportHelper.SerialPortExceptionReceived += action;
        }

        public void RemoveTicketExceptionReceived(Action<string> action)
        {
            _ticketReportHelper.SerialPortExceptionReceived -= action;
        }

        public void ConnectTicket(string portName, int baudRate)
        {
            _ticketReportHelper.Connect(portName, baudRate);
        }

        public void DisconnectTicket()
        {
            _ticketReportHelper.Disconnect();
        }
    }
}