using FluorescenceFullAutomatic.Core.Config;
using FluorescenceFullAutomatic.Core.Model;
using FluorescenceFullAutomatic.Platform.Core.Config;
using FluorescenceFullAutomatic.Platform.Model;
using FluorescenceFullAutomatic.Platform.Utils;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Navigation;

namespace FluorescenceFullAutomatic.Platform.Services
{
    public interface ISerialPortCommandFacade
    {
        Task<BaseResponseModel<List<string>>> GetSelfInspectionStateAsync(
            bool retainReactionArea,
            CancellationToken cancellationToken = default,
            int timeoutMs = 300000
        );

        Task<BaseResponseModel<MachineStatusModel>> GetMachineStateAsync(
            CancellationToken cancellationToken = default,
            int timeoutMs = 30000
        );

        Task<BaseResponseModel<MoveSampleShelfModel>> MoveSampleShelfAsync(
            int pos,
            CancellationToken cancellationToken = default,
            int timeoutMs = 30000
        );

        Task<BaseResponseModel<MoveSampleModel>> MoveSampleAsync(
            int pos,
            CancellationToken cancellationToken = default,
            int timeoutMs = 30000
        );

        Task<BaseResponseModel<SamplingModel>> SamplingAsync(
            string type,
            int volume,
            CancellationToken cancellationToken = default,
            int timeoutMs = 30000
        );

        Task<BaseResponseModel<CleanoutSamplingProbeModel>> CleanoutSamplingProbeAsync(
            int duration,
            CancellationToken cancellationToken = default,
            int timeoutMs = 30000
        );

        Task<BaseResponseModel<AddingSampleModel>> AddingSampleAsync(
            int volume,
            string type,
            CancellationToken cancellationToken = default,
            int timeoutMs = 30000
        );

        Task<BaseResponseModel<DrainageModel>> DrainageAsync(
            CancellationToken cancellationToken = default,
            int timeoutMs = 30000
        );

        Task<BaseResponseModel<PushCardModel>> PushCardAsync(
            CancellationToken cancellationToken = default,
            int timeoutMs = 30000
        );

        Task<BaseResponseModel<MoveReactionAreaModel>> MoveReactionAreaAsync(
            int x,
            int y,
            CancellationToken cancellationToken = default,
            int timeoutMs = 30000
        );

        Task<BaseResponseModel<TestModel>> TestAsync(
            int x,
            int y,
            string cardType,
            string testType,
            string scanStart,
            string scanEnd,
            string peakWidth,
            string peakDistance,
            CancellationToken cancellationToken = default,
            int timeoutMs = 30000
        );

        Task<BaseResponseModel<ReactionTempModel>> GetReactionTempAsync(
            string temp,
            CancellationToken cancellationToken = default,
            int timeoutMs = 30000
        );

        Task<BaseResponseModel<ClearReactionAreaModel>> ClearReactionAreaAsync(
            CancellationToken cancellationToken = default,
            int timeoutMs = 30000
        );

        Task<BaseResponseModel<MotorModel>> MotorAsync(
            string motor,
            string direction,
            string value,
            CancellationToken cancellationToken = default,
            int timeoutMs = 30000
        );

        Task<BaseResponseModel<ResetParamsModel>> ResetParamsAsync(
            CancellationToken cancellationToken = default,
            int timeoutMs = 30000
        );

        Task<BaseResponseModel<UpdateModel>> UpdateAsync(
            CancellationToken cancellationToken = default,
            int timeoutMs = 30000
        );

        Task<BaseResponseModel<SqueezingModel>> SqueezingAsync(
            string type,
            CancellationToken cancellationToken = default,
            int timeoutMs = 30000
        );

        Task<BaseResponseModel<PiercedModel>> PiercedAsync(
            string type,
            CancellationToken cancellationToken = default,
            int timeoutMs = 30000
        );

        Task<BaseResponseModel<VersionModel>> GetVersionAsync(
            CancellationToken cancellationToken = default,
            int timeoutMs = 30000
        );

        Task<BaseResponseModel<ShutdownModel>> ShutdownAsync(
            CancellationToken cancellationToken = default,
            int timeoutMs = 30000
        );
    }

    public interface ISerialPortService
    {
        //ͨѶ����
        void AddSerialPortConnectReceived(Action<string> action);
        void RemoveSerialPortConnectReceived(Action<string> action);

        void AddSerialPortExceptionReceived(Action<string> action);
        void RemoveSerialPortExceptionReceived(Action<string> action);
        void Connect(string portName, int baudRate);
        void Disconnect();

        //���봮��
        void AddBarcodeConnectReceived(Action<string> action);
        void RemoveBarcodeConnectReceived(Action<string> action);

        void AddBarcodeExceptionReceived(Action<string> action);
        void RemoveBarcodeExceptionReceived(Action<string> action);

        void ConnectBarcode(string portName, int baudRate);
        void DisconnectBarcode();

        //������ӡ������
        void AddTicketConnectReceived(Action<string> action);
        void RemoveTicketConnectReceived(Action<string> action);

        void AddTicketExceptionReceived(Action<string> action);
        void RemoveTicketExceptionReceived(Action<string> action);

        void ConnectTicket(string portName, int baudRate);
        void DisconnectTicket();




        // �Լ����?
        void GetSelfInspectionState(bool retainReactionArea);
        
        // �������?
        void GetMachineState();
        
        // ��ϴҺ���?
        void GetCleanoutFluid();
        
        // ���������?
        void GetSampleShelf();
        void MoveSampleShelf(int pos);
        
        // �����������?
        void MoveSample( int pos);
        void Sampling(string type, int volume);
        void CleanoutSamplingProbe(int duration);
        void AddingSample(int volume, string type);
        void Drainage();
        
        // ��Ƭ�������?
        void PushCard();
        void MoveReactionArea(int x, int y);
        void Test(int x, int y, string cardType, string testType, string scanStart, 
                 string scanEnd, string peakWidth, string peakDistance);
        
        // �¶����?
        void GetReactionTemp(string temp);
        
        // ��Ӧ������
        void ClearReactionArea();
        
        // �������?
        void Motor(string motor, string direction, string value);
        
        // ϵͳ���?
        void ResetParams();
        void Update();
        
        // �����������?
        void Squeezing(string type);
        void Pierced(string type);

        void GetVersion();
        void Shutdown();
        public bool IsOpen();
        // �¼�ע��
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

        void RemoveScanSuccessListener(Action<string> onScanSuccess);
        void RemoveScanFailedListener(Action<string> onScanFailed);

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

        public void GetSelfInspectionState(bool retainReactionArea)
        {
            _serialPortHelper.GetSelfInspectionState(retainReactionArea);
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

        public void RemoveScanSuccessListener(Action<string> onScanSuccess)
        {
            _barcodeHelper.ScanSuccess -= onScanSuccess;
        }

        public void RemoveScanFailedListener(Action<string> onScanFailed)
        {
            _barcodeHelper.ScanFailed -= onScanFailed;
        }
    }

    public sealed class SerialCommandException : Exception
    {
        public string Code { get; }
        public string DeviceError { get; }

        public SerialCommandException(string code, string deviceError)
            : base($"串口命令执行失败。命令码={code} 错误={deviceError}")
        {
            Code = code;
            DeviceError = deviceError;
        }
    }

    public sealed class SerialPortCommandFacade : ISerialPortCommandFacade, IReceiveData
    {
        private interface IPendingRequest
        {
            bool IsCompleted { get; }
            void TrySetResult(object result);
            void TrySetException(Exception exception);
        }

        private sealed class PendingRequest<T> : IPendingRequest
        {
            public TaskCompletionSource<T> Tcs { get; }

            public PendingRequest()
            {
                Tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
            }

            public bool IsCompleted => Tcs.Task.IsCompleted;

            public void TrySetResult(object result)
            {
                if (result is T typed)
                {
                    Tcs.TrySetResult(typed);
                }
            }

            public void TrySetException(Exception exception)
            {
                Tcs.TrySetException(exception);
            }
        }

        private readonly object _gate = new object();
        private readonly Dictionary<string, IPendingRequest> _pendingByCmd =
            new Dictionary<string, IPendingRequest>();

        private readonly ISerialPortService _serialPortService;

        public SerialPortCommandFacade(ISerialPortService serialPortService)
        {
            _serialPortService = serialPortService;
            _serialPortService.AddReceiveData(this);
        }

        public Task<BaseResponseModel<List<string>>> GetSelfInspectionStateAsync(
            bool retainReactionArea,
            CancellationToken cancellationToken = default,
            int timeoutMs = 30000
        )
        {
            return EnqueueAsync<BaseResponseModel<List<string>>>(
                SerialGlobal.CMD_GetSelfInspectionState,
                () => _serialPortService.GetSelfInspectionState(retainReactionArea),
                cancellationToken,
                timeoutMs
            );
        }

        public Task<BaseResponseModel<MachineStatusModel>> GetMachineStateAsync(
            CancellationToken cancellationToken = default,
            int timeoutMs = 5000
        )
        {
            return EnqueueAsync<BaseResponseModel<MachineStatusModel>>(
                SerialGlobal.CMD_GetMachineState,
                () => _serialPortService.GetMachineState(),
                cancellationToken,
                timeoutMs
            );
        }

        public Task<BaseResponseModel<MoveSampleShelfModel>> MoveSampleShelfAsync(
            int pos,
            CancellationToken cancellationToken = default,
            int timeoutMs = 15000
        )
        {
            return EnqueueAsync<BaseResponseModel<MoveSampleShelfModel>>(
                SerialGlobal.CMD_MoveSampleShelf,
                () => _serialPortService.MoveSampleShelf(pos),
                cancellationToken,
                timeoutMs
            );
        }

        public Task<BaseResponseModel<MoveSampleModel>> MoveSampleAsync(
            int pos,
            CancellationToken cancellationToken = default,
            int timeoutMs = 15000
        )
        {
            return EnqueueAsync<BaseResponseModel<MoveSampleModel>>(
                SerialGlobal.CMD_MoveSample,
                () => _serialPortService.MoveSample(pos),
                cancellationToken,
                timeoutMs
            );
        }

        public Task<BaseResponseModel<SamplingModel>> SamplingAsync(
            string type,
            int volume,
            CancellationToken cancellationToken = default,
            int timeoutMs = 15000
        )
        {
            return EnqueueAsync<BaseResponseModel<SamplingModel>>(
                SerialGlobal.CMD_Sampling,
                () => _serialPortService.Sampling(type, volume),
                cancellationToken,
                timeoutMs
            );
        }

        public Task<BaseResponseModel<CleanoutSamplingProbeModel>> CleanoutSamplingProbeAsync(
            int duration,
            CancellationToken cancellationToken = default,
            int timeoutMs = 15000
        )
        {
            return EnqueueAsync<BaseResponseModel<CleanoutSamplingProbeModel>>(
                SerialGlobal.CMD_CleanoutSamplingProbe,
                () => _serialPortService.CleanoutSamplingProbe(duration),
                cancellationToken,
                timeoutMs
            );
        }

        public Task<BaseResponseModel<AddingSampleModel>> AddingSampleAsync(
            int volume,
            string type,
            CancellationToken cancellationToken = default,
            int timeoutMs = 15000
        )
        {
            return EnqueueAsync<BaseResponseModel<AddingSampleModel>>(
                SerialGlobal.CMD_AddingSample,
                () => _serialPortService.AddingSample(volume, type),
                cancellationToken,
                timeoutMs
            );
        }

        public Task<BaseResponseModel<DrainageModel>> DrainageAsync(
            CancellationToken cancellationToken = default,
            int timeoutMs = 15000
        )
        {
            return EnqueueAsync<BaseResponseModel<DrainageModel>>(
                SerialGlobal.CMD_Drainage,
                () => _serialPortService.Drainage(),
                cancellationToken,
                timeoutMs
            );
        }

        public Task<BaseResponseModel<PushCardModel>> PushCardAsync(
            CancellationToken cancellationToken = default,
            int timeoutMs = 15000
        )
        {
            return EnqueueAsync<BaseResponseModel<PushCardModel>>(
                SerialGlobal.CMD_PushCard,
                () => _serialPortService.PushCard(),
                cancellationToken,
                timeoutMs
            );
        }

        public Task<BaseResponseModel<MoveReactionAreaModel>> MoveReactionAreaAsync(
            int x,
            int y,
            CancellationToken cancellationToken = default,
            int timeoutMs = 20000
        )
        {
            return EnqueueAsync<BaseResponseModel<MoveReactionAreaModel>>(
                SerialGlobal.CMD_MoveReactionArea,
                () => _serialPortService.MoveReactionArea(x, y),
                cancellationToken,
                timeoutMs
            );
        }

        public Task<BaseResponseModel<TestModel>> TestAsync(
            int x,
            int y,
            string cardType,
            string testType,
            string scanStart,
            string scanEnd,
            string peakWidth,
            string peakDistance,
            CancellationToken cancellationToken = default,
            int timeoutMs = 30000
        )
        {
            return EnqueueAsync<BaseResponseModel<TestModel>>(
                SerialGlobal.CMD_Test,
                () =>
                    _serialPortService.Test(
                        x,
                        y,
                        cardType,
                        testType,
                        scanStart,
                        scanEnd,
                        peakWidth,
                        peakDistance
                    ),
                cancellationToken,
                timeoutMs
            );
        }

        public Task<BaseResponseModel<ReactionTempModel>> GetReactionTempAsync(
            string temp,
            CancellationToken cancellationToken = default,
            int timeoutMs = 5000
        )
        {
            return EnqueueAsync<BaseResponseModel<ReactionTempModel>>(
                SerialGlobal.CMD_GetReactionTemp,
                () => _serialPortService.GetReactionTemp(temp),
                cancellationToken,
                timeoutMs
            );
        }

        public Task<BaseResponseModel<ClearReactionAreaModel>> ClearReactionAreaAsync(
            CancellationToken cancellationToken = default,
            int timeoutMs = 15000
        )
        {
            return EnqueueAsync<BaseResponseModel<ClearReactionAreaModel>>(
                SerialGlobal.CMD_ClearReactionArea,
                () => _serialPortService.ClearReactionArea(),
                cancellationToken,
                timeoutMs
            );
        }

        public Task<BaseResponseModel<MotorModel>> MotorAsync(
            string motor,
            string direction,
            string value,
            CancellationToken cancellationToken = default,
            int timeoutMs = 15000
        )
        {
            return EnqueueAsync<BaseResponseModel<MotorModel>>(
                SerialGlobal.CMD_Motor,
                () => _serialPortService.Motor(motor, direction, value),
                cancellationToken,
                timeoutMs
            );
        }

        public Task<BaseResponseModel<ResetParamsModel>> ResetParamsAsync(
            CancellationToken cancellationToken = default,
            int timeoutMs = 15000
        )
        {
            return EnqueueAsync<BaseResponseModel<ResetParamsModel>>(
                SerialGlobal.CMD_ResetParams,
                () => _serialPortService.ResetParams(),
                cancellationToken,
                timeoutMs
            );
        }

        public Task<BaseResponseModel<UpdateModel>> UpdateAsync(
            CancellationToken cancellationToken = default,
            int timeoutMs = 30000
        )
        {
            return EnqueueAsync<BaseResponseModel<UpdateModel>>(
                SerialGlobal.CMD_Update,
                () => _serialPortService.Update(),
                cancellationToken,
                timeoutMs
            );
        }

        public Task<BaseResponseModel<SqueezingModel>> SqueezingAsync(
            string type,
            CancellationToken cancellationToken = default,
            int timeoutMs = 15000
        )
        {
            return EnqueueAsync<BaseResponseModel<SqueezingModel>>(
                SerialGlobal.CMD_Squeezing,
                () => _serialPortService.Squeezing(type),
                cancellationToken,
                timeoutMs
            );
        }

        public Task<BaseResponseModel<PiercedModel>> PiercedAsync(
            string type,
            CancellationToken cancellationToken = default,
            int timeoutMs = 15000
        )
        {
            return EnqueueAsync<BaseResponseModel<PiercedModel>>(
                SerialGlobal.CMD_Pierced,
                () => _serialPortService.Pierced(type),
                cancellationToken,
                timeoutMs
            );
        }

        public Task<BaseResponseModel<VersionModel>> GetVersionAsync(
            CancellationToken cancellationToken = default,
            int timeoutMs = 5000
        )
        {
            return EnqueueAsync<BaseResponseModel<VersionModel>>(
                SerialGlobal.CMD_Version,
                () => _serialPortService.GetVersion(),
                cancellationToken,
                timeoutMs
            );
        }

        public Task<BaseResponseModel<ShutdownModel>> ShutdownAsync(
            CancellationToken cancellationToken = default,
            int timeoutMs = 5000
        )
        {
            return EnqueueAsync<BaseResponseModel<ShutdownModel>>(
                SerialGlobal.CMD_Shutdown,
                () => _serialPortService.Shutdown(),
                cancellationToken,
                timeoutMs
            );
        }

        private Task<T> EnqueueAsync<T>(
            string cmd,
            Action send,
            CancellationToken cancellationToken,
            int timeoutMs
        )
        {
            var pending = new PendingRequest<T>();
            lock (_gate)
            {
                if (_pendingByCmd.TryGetValue(cmd, out var existing) && !existing.IsCompleted)
                {
                    throw new InvalidOperationException($"命令正在执行中，不允许并发。命令码={cmd}");
                }
                _pendingByCmd[cmd] = pending;
            }

            send();
            return WaitAsync(pending, cmd, cancellationToken, timeoutMs);
        }

        private async Task<T> WaitAsync<T>(
            PendingRequest<T> pending,
            string cmd,
            CancellationToken cancellationToken,
            int timeoutMs
        )
        {
            var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            try
            {
                var delayTask = Task.Delay(timeoutMs, timeoutCts.Token);

                var completed = await Task.WhenAny(pending.Tcs.Task, delayTask);
                if (completed == pending.Tcs.Task)
                {
                    timeoutCts.Cancel();
                    RemovePending(cmd, pending);
                    return await pending.Tcs.Task;
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    pending.Tcs.TrySetCanceled(cancellationToken);
                    RemovePending(cmd, pending);
                    throw new OperationCanceledException(cancellationToken);
                }

                var ex = new TimeoutException($"串口命令超时。命令码={cmd} 超时={timeoutMs}ms");
                pending.Tcs.TrySetException(ex);
                RemovePending(cmd, pending);
                throw ex;
            }
            finally
            {
                timeoutCts.Dispose();
            }
        }

        private void RemovePending(string cmd, IPendingRequest pending)
        {
            lock (_gate)
            {
                if (_pendingByCmd.TryGetValue(cmd, out var existing) && ReferenceEquals(existing, pending))
                {
                    _pendingByCmd.Remove(cmd);
                }
            }
        }

        private void CompleteNext(string cmd, object result)
        {
            IPendingRequest pending = null;
            lock (_gate)
            {
                if (_pendingByCmd.TryGetValue(cmd, out pending))
                {
                    _pendingByCmd.Remove(cmd);
                }
            }
            pending?.TrySetResult(result);
        }

        private void FailNext(string cmd, Exception exception)
        {
            IPendingRequest pending = null;
            lock (_gate)
            {
                if (_pendingByCmd.TryGetValue(cmd, out pending))
                {
                    _pendingByCmd.Remove(cmd);
                }
            }
            pending?.TrySetException(exception);
        }

        public void ReceiveGetSelfMachineStatusModel(BaseResponseModel<List<string>> model)
        {
            CompleteNext(SerialGlobal.CMD_GetSelfInspectionState, model);
        }

        public void ReceiveMachineStatusModel(BaseResponseModel<MachineStatusModel> model)
        {
            CompleteNext(SerialGlobal.CMD_GetMachineState, model);
        }

        public void ReceiveMoveSampleShelfModel(BaseResponseModel<MoveSampleShelfModel> model)
        {
            CompleteNext(SerialGlobal.CMD_MoveSampleShelf, model);
        }

        public void ReceiveMoveSampleModel(BaseResponseModel<MoveSampleModel> model)
        {
            CompleteNext(SerialGlobal.CMD_MoveSample, model);
        }

        public void ReceiveSamplingModel(BaseResponseModel<SamplingModel> model)
        {
            CompleteNext(SerialGlobal.CMD_Sampling, model);
        }

        public void ReceiveCleanoutSamplingProbeModel(BaseResponseModel<CleanoutSamplingProbeModel> model)
        {
            CompleteNext(SerialGlobal.CMD_CleanoutSamplingProbe, model);
        }

        public void ReceiveAddingSampleModel(BaseResponseModel<AddingSampleModel> model)
        {
            CompleteNext(SerialGlobal.CMD_AddingSample, model);
        }

        public void ReceiveDrainageModel(BaseResponseModel<DrainageModel> model)
        {
            CompleteNext(SerialGlobal.CMD_Drainage, model);
        }

        public void ReceivePushCardModel(BaseResponseModel<PushCardModel> model)
        {
            CompleteNext(SerialGlobal.CMD_PushCard, model);
        }

        public void ReceiveMoveReactionAreaModel(BaseResponseModel<MoveReactionAreaModel> model)
        {
            CompleteNext(SerialGlobal.CMD_MoveReactionArea, model);
        }

        public void ReceiveTestModel(BaseResponseModel<TestModel> model)
        {
            CompleteNext(SerialGlobal.CMD_Test, model);
        }

        public void ReceiveReactionTempModel(BaseResponseModel<ReactionTempModel> model)
        {
            CompleteNext(SerialGlobal.CMD_GetReactionTemp, model);
        }

        public void ReceiveClearReactionAreaModel(BaseResponseModel<ClearReactionAreaModel> model)
        {
            CompleteNext(SerialGlobal.CMD_ClearReactionArea, model);
        }

        public void ReceiveMotorModel(BaseResponseModel<MotorModel> model)
        {
            CompleteNext(SerialGlobal.CMD_Motor, model);
        }

        public void ReceiveResetParamsModel(BaseResponseModel<ResetParamsModel> model)
        {
            CompleteNext(SerialGlobal.CMD_ResetParams, model);
        }

        public void ReceiveUpdateModel(BaseResponseModel<UpdateModel> model)
        {
            CompleteNext(SerialGlobal.CMD_Update, model);
        }

        public void ReceiveSqueezingModel(BaseResponseModel<SqueezingModel> model)
        {
            CompleteNext(SerialGlobal.CMD_Squeezing, model);
        }

        public void ReceivePiercedModel(BaseResponseModel<PiercedModel> model)
        {
            CompleteNext(SerialGlobal.CMD_Pierced, model);
        }

        public void ReceiveVersionModel(BaseResponseModel<VersionModel> model)
        {
            CompleteNext(SerialGlobal.CMD_Version, model);
        }

        public void ReceiveShutdownModel(BaseResponseModel<ShutdownModel> model)
        {
            CompleteNext(SerialGlobal.CMD_Shutdown, model);
        }

        public void ReceiveStateError(BaseResponseModel<dynamic> model)
        {
            if (model == null || string.IsNullOrEmpty(model.Code))
            {
                return;
            }
            FailNext(model.Code, new SerialCommandException(model.Code, model.Error ?? ""));
        }
    }
}
