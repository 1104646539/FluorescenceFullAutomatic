using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluorescenceFullAutomatic.Config;
using FluorescenceFullAutomatic.Model;
using NHapi.Base.Model;
using NHapi.Model.V231.Message;
using NHapi.Model.V231.Segment;
using Serilog;
using Main.Upload;
using static Main.Upload.Hl7Result;
using System.Threading;

namespace FluorescenceFullAutomatic.Upload
{
    /// <summary>
    /// 连接状态变化的委托
    /// </summary>
    /// <param name="isConnected">是否已连接</param>
    /// <param name="message">相关消息</param>
    public delegate void ConnectionStatusChangedHandler(bool isConnected, string message);
    
    /// <summary>
    /// 连接错误的委托
    /// </summary>
    /// <param name="errorMessage">错误消息</param>
    /// <param name="exception">异常对象</param>
    public delegate void ConnectionErrorHandler(string errorMessage, Exception exception);
    
    /// <summary>
    /// HL7通信助手类，提供HL7通信的便捷方法
    /// </summary>
    public class HL7Helper
    {
        // 使用完全懒加载模式，只有在第一次访问Instance时才初始化
        private static readonly Lazy<HL7Helper> _instance = new Lazy<HL7Helper>(
            () => new HL7Helper(), 
            System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);
        
        /// <summary>
        /// 单例实例
        /// </summary>
        public static HL7Helper Instance => _instance.Value;
        
        private IHL7CommunicationService _communicationService;
        private bool _isRunning = false;
        // 标记是否正在初始化，避免循环引用
        private bool _isInitializing = false;
        
        // 简单的重连配置
        private bool _autoReconnectEnabled = true;
        private int _reconnectDelay = 5000; // 默认5秒后重连
        
        // 添加CancellationTokenSource用于管理重连任务
        private CancellationTokenSource _reconnectionCTS;
        
        /// <summary>
        /// 连接成功事件
        /// </summary>
        public event ConnectionStatusChangedHandler ConnectionSucceeded;
        
        /// <summary>
        /// 连接断开事件
        /// </summary>
        public event ConnectionStatusChangedHandler ConnectionClosed;
        
        /// <summary>
        /// 连接失败事件
        /// </summary>
        public event ConnectionErrorHandler ConnectionFailed;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        private HL7Helper()
        {
           
        }
        
        /// <summary>
        /// 初始化通信服务
        /// </summary>
        public void InitializeService()
        {
            // 防止循环引用
            if (_isInitializing) return;
            if (!UploadConfig.Instance.OpenUpload) {
                if (IsConnected())
                {
                    Disconnect();
                }
                else {
                    OnConnectionClosed("上传未打开");
                }
                return;
            }
            _isInitializing = true;
            try
            {
                
                // 先断开现有连接
                if (_communicationService != null)
                {
                    try
                    {
                        _communicationService.Disconnect();
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "断开现有连接时发生错误");
                    }
                    finally
                    {
                        _communicationService = null;
                        // 这里不触发事件，而是直接更新状态
                        _isRunning = false;
                    }
                }
                
                // 根据配置选择通信方式
                bool isSerialPort = UploadConfig.Instance.SerialPort;
                if (isSerialPort)
                {
                    _communicationService = new SerialCommunicationService();
                }
                else
                {
                    _communicationService = new TcpCommunicationService();
                }
                
                // 尝试连接
                if (UploadConfig.Instance.OpenUpload)
                {
                    bool connected = false;
                    try
                    {
                        connected = _communicationService.Connect();
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "连接通信服务时发生错误");
                        connected = false;
                    }
                    
                    _isRunning = connected;
                    
                    // 连接完成后再触发事件
                    if (connected)
                    {
                        OnConnectionSucceeded("初始化时自动连接成功");
                    }
                    else
                    {
                        OnConnectionFailed("初始化时自动连接失败", null);
                    }
                }
            }
            catch (Exception ex)
            {
                _isRunning = false;
                Log.Error(ex, "初始化HL7通信服务失败");
                OnConnectionFailed("初始化HL7通信服务失败", ex);
            }
            finally
            {
                _isInitializing = false;
            }
        }
        CancellationToken reconnectionCancellationToken;
        /// <summary>
        /// 简单的自动重连方法
        /// </summary>
        /// <param name="message">重连原因</param>
        private void TryReconnect(string message)
        {
            // 取消之前的重连任务（如果有）
            CancelPendingReconnection();

            if (!UploadConfig.Instance.AutoReconnection || !UploadConfig.Instance.OpenUpload || UploadConfig.Instance.SerialPort)
                return;

            int delay = UploadConfig.Instance.AutoReconnectionIntervalTime;
            Log.Information($"准备在 {delay}ms 后尝试重新连接，原因: {message}");
            
         
            // 创建新的取消令牌源
            _reconnectionCTS = new CancellationTokenSource();
            reconnectionCancellationToken = _reconnectionCTS.Token;
            
            // 使用简单的延迟任务进行重连
            Task.Run(async () => 
            {
                try
                {
                    // 等待指定的延迟时间，如果取消则退出
                    await Task.Delay(delay, reconnectionCancellationToken);
                    
                    // 检查是否已经被取消
                    if (reconnectionCancellationToken.IsCancellationRequested)
                    {
                        Log.Information("重连任务已被取消");
                        return;
                    }
                    
                    Log.Information("执行重新连接...");
                    
                    // 直接调用初始化方法重新连接
                    InitializeService();
                }
                catch (OperationCanceledException)
                {
                    Log.Information("重连任务已被取消");
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "重新连接时发生错误");
                }
            });
        }
        
        /// <summary>
        /// 取消待定的重连任务
        /// </summary>
        private void CancelPendingReconnection()
        {
            try
            {
                if (_reconnectionCTS != null && !_reconnectionCTS.IsCancellationRequested)
                {
                    Log.Information("取消待定的重连任务");
                    _reconnectionCTS.Cancel();
                    _reconnectionCTS.Dispose();
                    _reconnectionCTS = null;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "取消重连任务时发生错误");
            }
        }
       
        /// <summary>
        /// 启动HL7服务
        /// </summary>
        /// <returns>是否成功启动</returns>
        public bool Start()
        {
            try
            {
                if (_isRunning)
                {
                    Log.Information("HL7服务已经在运行中");
                    return true;
                }
                
                // 重新初始化服务
                InitializeService();
                
                // 如果配置允许上传，尝试连接
                if (UploadConfig.Instance.OpenUpload && _communicationService != null)
                {
                    _isRunning = _communicationService.Connect();
                    if (_isRunning)
                    {
                        Log.Information("HL7服务启动成功");
                        OnConnectionSucceeded("HL7服务启动成功");
                    }
                    else
                    {
                        Log.Warning("HL7服务启动失败，无法连接到服务器");
                        OnConnectionFailed("HL7服务启动失败，无法连接到服务器", null);
                    }
                }
                else
                {
                    _isRunning = false;
                    Log.Information("HL7服务未启动，上传功能已禁用");
                }
                
                return _isRunning;
            }
            catch (Exception ex)
            {
                _isRunning = false;
                Log.Error(ex, "启动HL7服务时发生错误");
                OnConnectionFailed("启动HL7服务时发生错误", ex);
                return false;
            }
        }
        
        /// <summary>
        /// 停止HL7服务
        /// </summary>
        public void Stop()
        {
            try
            {
                if (!_isRunning)
                {
                    Log.Information("HL7服务未运行");
                    return;
                }
                
                // 断开连接
                if (_communicationService != null)
                {
                    _communicationService.Disconnect();
                }
                
                _isRunning = false;
                Log.Information("HL7服务已停止");
                OnConnectionClosed("HL7服务已停止");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "停止HL7服务时发生错误");
                OnConnectionFailed("停止HL7服务时发生错误", ex);
            }
        }
        
        /// <summary>
        /// 检查HL7服务是否正在运行
        /// </summary>
        /// <returns>服务是否正在运行</returns>
        public bool IsRunning()
        {
            return _isRunning && IsConnected();
        }
        
        /// <summary>
        /// 触发连接成功事件
        /// </summary>
        /// <param name="message">相关消息</param>
        private void OnConnectionSucceeded(string message)
        {
            try
            {
                // 连接成功时取消所有待定的重连任务
                CancelPendingReconnection();
                
                ConnectionSucceeded?.Invoke(true, message);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "触发连接成功事件时发生错误");
            }
        }
        
        /// <summary>
        /// 触发连接断开事件
        /// </summary>
        /// <param name="message">相关消息</param>
        private void OnConnectionClosed(string message)
        {
            try
            {
                ConnectionClosed?.Invoke(false, message);
                
                // 检查是否需要自动重连（非手动断开的情况）
                //if (!message.Contains("手动断开") && !message.Contains("服务已停止"))
                //{
                    TryReconnect(message);
                //}
            }
            catch (Exception ex)
            {
                Log.Error(ex, "触发连接断开事件时发生错误");
            }
        }
        
        /// <summary>
        /// 触发连接失败事件
        /// </summary>
        /// <param name="errorMessage">错误消息</param>
        /// <param name="exception">异常对象</param>
        private void OnConnectionFailed(string errorMessage, Exception exception)
        {
            try
            {
                ConnectionFailed?.Invoke(errorMessage, exception);
                
                // 连接失败时尝试重连
                TryReconnect(errorMessage);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "触发连接失败事件时发生错误");
            }
        }
        
        /// <summary>
        /// 查询样本信息
        /// </summary>
        /// <param name="queryType">查询类型</param>
        /// <param name="condition1">条件1</param>
        /// <param name="condition2">条件2</param>
        /// <returns>查询结果</returns>
        public async Task<Hl7Result.QueryResult> QueryApplyTestAsync(QueryType queryType , string condition1,  string condition2 = "")
        {
            try
            {
                if (!UploadConfig.Instance.OpenUpload)
                {
                    Log.Warning("上传功能未启用，无法查询样本信息");
                    return new Hl7Result.QueryResult(
                        Hl7Result.QueryResultType.NotConnected,
                        queryType,
                        condition1,
                        condition2,
                        "上传功能未启用",
                        null,
                        null, null
                    );
                }
                
                if (_communicationService == null)
                {
                    Log.Error("HL7通信服务未初始化");
                    return new Hl7Result.QueryResult(
                        Hl7Result.QueryResultType.NotConnected,
                        queryType,
                        condition1,
                        condition2,
                        "HL7通信服务未初始化",
                        null,
                        null, null
                    );
                }
                
                // 发送查询请求并直接返回结果
                return await _communicationService.QuerySampleInfoAsync(queryType, condition1, condition2);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"查询样本 {queryType} {condition1} {condition2} 信息时发生错误");
                return new Hl7Result.QueryResult(
                    Hl7Result.QueryResultType.NotConnected,
                    queryType,
                    condition1,
                    condition2,
                    $"发生错误: {ex.Message}",
                    null,
                    null, null
                );
            }
        }
        /// <summary>
        /// 批量上传测试结果
        /// </summary>
        /// <param name="testResults"></param>
        /// <param name="uploadResult"></param>
        public void UploadTestResult(List<TestResult> testResults,Action<List<UploadResult>> uploadResult)
        {
            List<UploadResult> results = new List<UploadResult>();
            if (testResults == null) {
                Log.Information("上传的测试结果列表为空");
                uploadResult?.Invoke(results);
                return;
            }
            Task.Run(async() => {
                for (int i = 0; i < testResults.Count; i++)
                {
                   UploadResult result = await UploadTestResultAsync(testResults[i]);
                    if (result != null && result.ResultType != UploadResultType.Success) {
                        results.Add(result);
                    }
                }
                App.Current.Dispatcher.Invoke(() =>
                {
                    uploadResult?.Invoke(results);
                });
            });
        }


        /// <summary>
        /// 上传检测结果
        /// </summary>
        /// <param name="testResult">测试结果</param>
        /// <returns>上传结果</returns>
        public async Task<Hl7Result.UploadResult> UploadTestResultAsync(TestResult testResult)
        {
            try
            {
                if (!UploadConfig.Instance.OpenUpload)
                {
                    Log.Warning("上传功能未启用，无法上传检测结果");
                    return new Hl7Result.UploadResult(
                        Hl7Result.UploadResultType.Failed,
                        testResult.Id,
                        "上传功能未启用",
                        null);
                }
                
                if (_communicationService == null)
                {
                    Log.Error("HL7通信服务未初始化");
                    return new Hl7Result.UploadResult(
                        Hl7Result.UploadResultType.NotConnected,
                        testResult.Id,
                        "HL7通信服务未初始化",
                        null);
                }
                
                // 发送上传请求并直接返回结果
                return await _communicationService.UploadTestResultAsync(testResult);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"上传样本 {testResult.Barcode} 结果时发生错误");
                return new Hl7Result.UploadResult(
                    Hl7Result.UploadResultType.Failed,
                    testResult.Id,
                    $"发生错误: {ex.Message}",
                    null);
            }
        }
        
        /// <summary>
        /// 关闭连接
        /// </summary>
        public void Disconnect()
        {
            try
            {
                _communicationService?.Disconnect();
                _isRunning = false;
                OnConnectionClosed("手动断开连接");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "断开HL7通信连接时发生错误");
                OnConnectionFailed("断开HL7通信连接时发生错误", ex);
            }
        }
        
        /// <summary>
        /// 检查连接状态
        /// </summary>
        /// <returns>连接是否正常</returns>
        public bool IsConnected()
        {
            try
            {
                return _isRunning && (_communicationService?.IsConnected() ?? false);
            }
            catch (Exception ex)
            {
                Log.Information(ex, "检查HL7通信连接状态时发生错误");
                return false;
            }
        }
        
        /// <summary>
        /// 通知连接状态变化
        /// </summary>
        /// <param name="isConnected">是否已连接</param>
        /// <param name="message">状态消息</param>
        public void NotifyConnectionState(bool isConnected, string message)
        {
            try
            {
                // 更新连接状态
                bool stateChanged = false;
                
                if (isConnected && !_isRunning)
                {
                    _isRunning = true;
                    stateChanged = true;
                }
                else if (!isConnected && _isRunning)
                {
                    _isRunning = false;
                    stateChanged = true;
                }
                
                // 如果状态发生变化，触发相应的事件
                if (stateChanged)
                {
                    if (isConnected)
                    {
                        OnConnectionSucceeded(message);
                    }
                    else
                    {
                        OnConnectionClosed(message);
                    }
                }
            }
            catch (Exception ex)
            {
                // 捕获可能的异常，避免影响调用方
                Log.Error(ex, "通知连接状态变化时发生错误");
            }
        }
        
        /// <summary>
        /// 添加连接成功回调
        /// </summary>
        /// <param name="handler">回调方法</param>
        public void AddConnectionSucceededHandler(ConnectionStatusChangedHandler handler)
        {
            if (handler != null)
            {
                ConnectionSucceeded += handler;
            }
        }
        
        /// <summary>
        /// 移除连接成功回调
        /// </summary>
        /// <param name="handler">回调方法</param>
        public void RemoveConnectionSucceededHandler(ConnectionStatusChangedHandler handler)
        {
            if (handler != null)
            {
                ConnectionSucceeded -= handler;
            }
        }
        
        /// <summary>
        /// 添加连接断开回调
        /// </summary>
        /// <param name="handler">回调方法</param>
        public void AddConnectionClosedHandler(ConnectionStatusChangedHandler handler)
        {
            if (handler != null)
            {
                ConnectionClosed += handler;
            }
        }
        
        /// <summary>
        /// 移除连接断开回调
        /// </summary>
        /// <param name="handler">回调方法</param>
        public void RemoveConnectionClosedHandler(ConnectionStatusChangedHandler handler)
        {
            if (handler != null)
            {
                ConnectionClosed -= handler;
            }
        }
        
        /// <summary>
        /// 添加连接失败回调
        /// </summary>
        /// <param name="handler">回调方法</param>
        public void AddConnectionFailedHandler(ConnectionErrorHandler handler)
        {
            if (handler != null)
            {
                ConnectionFailed += handler;
            }
        }
        
        /// <summary>
        /// 移除连接失败回调
        /// </summary>
        /// <param name="handler">回调方法</param>
        public void RemoveConnectionFailedHandler(ConnectionErrorHandler handler)
        {
            if (handler != null)
            {
                ConnectionFailed -= handler;
            }
        }
    }
    
    /// <summary>
    /// 上传状态类
    /// </summary>
    public class UploadStatus
    {
        /// <summary>
        /// 上传是否成功
        /// </summary>
        public bool IsSuccess { get; set; }
        
        /// <summary>
        /// 消息内容
        /// </summary>
        public string Message { get; set; }
        
        /// <summary>
        /// 测试结果ID
        /// </summary>
        public int TestResultId { get; set; }
        
        /// <summary>
        /// 上传结果类型
        /// </summary>
        public Hl7Result.UploadResultType ResultType { get; set; }
    }
}
