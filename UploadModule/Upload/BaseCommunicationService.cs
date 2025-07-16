using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluorescenceFullAutomatic.Platform.Core.Config;
using FluorescenceFullAutomatic.Platform.Model;
using FluorescenceFullAutomatic.Platform.Services;
using NHapi.Base.Model;
using NHapi.Base.Parser;
using NHapi.Model.V231.Message;
using NHapi.Model.V231.Segment;
using static FluorescenceFullAutomatic.UploadModule.Upload.Hl7Result;

namespace FluorescenceFullAutomatic.UploadModule.Upload
{
    /// <summary>
    /// 通信服务基类，实现HL7通信服务的共同逻辑
    /// </summary>
    public abstract class BaseCommunicationService : IHL7CommunicationService
    {
        protected readonly MLLPProtocol _mllpProtocol;
        protected readonly HL7MessageBuilder _messageBuilder;
        protected readonly PipeParser _parser;
        protected readonly object _lockObject = new object();
        protected bool _isConnected;
        protected CancellationTokenSource _receiveLoopCts;
        protected Task _receiveLoopTask;
        protected int _messageId = 1;
        protected readonly int _timeout;

        // 消息响应字典，键为消息ID，值为TaskCompletionSource
        protected readonly ConcurrentDictionary<
            string,
            TaskCompletionSource<IMessage>
        > _pendingResponses = new ConcurrentDictionary<string, TaskCompletionSource<IMessage>>();

        // 消息接收事件处理器列表
        protected readonly List<Action<IMessage>> _messageReceivedHandlers =
            new List<Action<IMessage>>();

        // 查询响应状态字典，键为查询ID，值为查询响应状态
        protected readonly ConcurrentDictionary<string, QueryResponseState> _pendingQueryResponses =
            new ConcurrentDictionary<string, QueryResponseState>();
        /// <summary>
        /// 连接成功事件
        /// </summary>
        public abstract event ConnectionStatusChangedHandler ConnectionSucceeded;

        /// <summary>
        /// 连接断开事件
        /// </summary>
        public abstract event ConnectionStatusChangedHandler ConnectionClosed;

        /// <summary>
        /// 连接失败事件
        /// </summary>
        public abstract event ConnectionErrorHandler ConnectionFailed;
        /// <summary>
        /// 查询响应状态类，用于跟踪查询响应的完整性
        /// </summary>
        protected class QueryResponseState
        {
            // 初始响应（QCK_Q02）
            public IMessage InitialResponse { get; set; }

            // 收到的DSR_Q03消息列表
            public List<DSR_Q03> DsrMessages { get; set; }

            // 是否接收完成
            public bool IsComplete { get; set; }

            // 完成任务源
            public TaskCompletionSource<List<IMessage>> CompletionSource { get; set; }

            // 最后一次收到消息的时间
            public DateTime LastMessageTime { get; set; }

            // 超时时间（毫秒）
            public int TimeoutMilliseconds { get; set; }
        }
        private readonly ILogService logService;
        /// <summary>
        /// 构造函数
        /// </summary>
        protected BaseCommunicationService(ILogService logService)
        {
            this.logService = logService;
            _timeout = UploadConfig.Instance.Overtime;

            // 获取编码格式
            Encoding encoding = GetEncoding();
            
            _mllpProtocol = new MLLPProtocol(encoding,logService);
            _messageBuilder = new HL7MessageBuilder(logService);
            _parser = new PipeParser();
            _isConnected = false;
            
        }


        /// <summary>
        /// 根据配置获取编码格式
        /// </summary>
        /// <returns>编码格式</returns>
        protected Encoding GetEncoding()
        {
            string charset = UploadConfig.Instance.Charset.ToUpper();
            if (charset == "UTF-8")
            {
                return Encoding.UTF8;
            }
            else if (charset == "GBK")
            {
                return Encoding.GetEncoding("GBK");
            }
            else
            {
                return Encoding.ASCII;
            }
        }

        /// <summary>
        /// 处理接收到的HL7消息
        /// </summary>
        /// <param name="message">接收到的消息</param>
        protected void ProcessReceivedMessage(IMessage message)
        {
            try
            {
                // 获取消息头
                var msh = (NHapi.Model.V231.Segment.MSH)message.GetStructure("MSH");
                string messageType = msh.MessageType.MessageType.Value;
                string triggerEvent = msh.MessageType.TriggerEvent.Value;
                string messageControlId = msh.MessageControlID.Value;

                logService.Info(
                    $"处理接收到的消息: 类型={messageType}, 事件={triggerEvent}, ID={messageControlId}"
                );

                // 通知所有注册的消息处理器
                foreach (var handler in _messageReceivedHandlers.ToArray())
                {
                    try
                    {
                        handler(message);
                    }
                    catch (Exception ex)
                    {
                        logService.Info($"执行消息处理器时出错{ex}");
                    }
                }

                // 检查是否是ACK消息
                if (message is ACK ack)
                {
                    try
                    {
                        // 提取原始消息ID（应该在MSA段中）
                        var msa = (MSA)ack.GetStructure("MSA");
                        string originalMessageId = msa.MessageControlID.Value;

                        // 检查应答状态 - 使用字符串直接处理原始消息
                        string encodedMessage = _parser.Encode(message);
                        string[] segments = encodedMessage.Split(
                            new[] { '\r' },
                            StringSplitOptions.RemoveEmptyEntries
                        );
                        string msaSegment = segments.FirstOrDefault(s => s.StartsWith("MSA"));

                        if (!string.IsNullOrEmpty(msaSegment))
                        {
                            string[] fields = msaSegment.Split('|');
                            if (fields.Length > 1)
                            {
                                string ackCode = fields[1];
                                string textMessage = fields.Length > 3 ? fields[3] : "";

                                if (ackCode == "AA" || ackCode == "CA")
                                {
                                    logService.Info($"收到消息 {originalMessageId} 的肯定确认");
                                }
                                else if (ackCode == "AE" || ackCode == "CE")
                                {
                                    logService.Info(
                                        $"收到消息 {originalMessageId} 的应用错误: {textMessage}"
                                    );
                                }
                                else if (ackCode == "AR" || ackCode == "CR")
                                {
                                    logService.Info(
                                        $"收到消息 {originalMessageId} 的拒绝: {textMessage}"
                                    );
                                }
                            }
                        }

                        // 尝试解析消息ID，找到对应的等待任务
                        if (_pendingResponses.TryRemove(originalMessageId, out var tcs))
                        {
                            tcs.TrySetResult(ack);
                        }
                        else
                        {
                            logService.Info(
                                $"收到ID为 {originalMessageId} 的ACK，但没有找到对应的等待任务"
                            );
                        }
                    }
                    catch (Exception ex)
                    {
                        logService.Info($"处理ACK消息时出错{ex}");
                    }
                }
                else if (message is QCK_Q02 qck)
                {
                    try
                    {
                        // 处理查询响应
                        var qak = qck.QAK;
                        string queryTag = qck.MSA.MessageControlID.Value;
                        string status = qak.QueryResponseStatus.Value;

                        if (status == "OK")
                        {
                            logService.Info($"查询样本信息成功，有数据返回，等待接收详细数据...{queryTag}");

                            // 记录这是一个需要等待后续DSR_Q03消息的查询
                            _pendingQueryResponses[queryTag] = new QueryResponseState
                            {
                                InitialResponse = qck,
                                DsrMessages = new List<DSR_Q03>(),
                                IsComplete = false,
                                CompletionSource = new TaskCompletionSource<List<IMessage>>(),
                                LastMessageTime = DateTime.Now,
                                TimeoutMilliseconds = UploadConfig.Instance.QueryReplyIntervalTime * 1000
                            };
                        }
                        else if (status == "NF")
                        {
                            logService.Info($"查询样本信息成功，无数据返回");
                        }
                        else
                        {
                            logService.Info($"查询样本信息，响应状态异常: {status}");
                        }

                        // 通知等待此查询响应的任务
                        if (_pendingResponses.TryRemove(queryTag, out var tcs))
                        {
                            tcs.TrySetResult(qck);
                        }
                        else
                        {
                            logService.Info($"收到ID为 {queryTag} 的QCK_Q02，但没有找到对应的等待任务");
                        }
                    }
                    catch (Exception ex)
                    {
                        logService.Info($"处理QCK_Q02消息时出错{ex}");
                    }
                }
                else if (message is DSR_Q03 dsr)
                {
                    try
                    {
                        // 立即发送ACK响应
                        try
                        {
                            ACK ackResponse = _messageBuilder.CreateACK(dsr, "AA", "");
                            // 编码消息
                            string encodedAck = _parser.Encode(ackResponse);
                            encodedAck = encodedAck.Replace("\\S\\","^");
                            logService.Info(
                                $"发送DSR_Q03的ACK响应 (ID={ackResponse.MSH.MessageControlID.Value}): {encodedAck}"
                            );

                            // 将消息转换为MLLP格式并发送
                            byte[] ackBytes = _mllpProtocol.WrapMessage(encodedAck);
                            // 异步发送ACK响应
                            _ = SendBytesAsync(ackBytes);
                        }
                        catch (Exception ex)
                        {
                            logService.Info($"发送DSR_Q03的ACK响应时出错{ex}");
                        }

                        // 检查响应（通过MSA字段判断ID）
                        var msgMsa = (MSA)dsr.GetStructure("MSA");
                        string relatedMessageId = msgMsa.MessageControlID.Value;
                        // 检查是否有关联的查询响应
                        if (
                            _pendingQueryResponses.TryGetValue(relatedMessageId, out var queryState)
                        )
                        {
                            // 将DSR消息添加到列表
                            queryState.DsrMessages.Add(dsr);

                            // 更新最后接收消息时间
                            queryState.LastMessageTime = DateTime.Now;

                            // 检查是否有更多段落需要接收
                            bool hasMoreSegments = false;
                            try
                            {
                                var dsc = dsr.DSC;
                                if (dsc != null && dsc.ContinuationPointer != null)
                                {
                                    hasMoreSegments = dsc.ContinuationPointer.Value != "-1";
                                }
                            }
                            catch (Exception ex)
                            {
                                logService.Info($"检查DSR_Q03消息的DSC段是否有更多数据时出错{ex}");
                            }

                            if (!hasMoreSegments)
                            {
                                // 没有更多数据了，可以完成任务
                                queryState.IsComplete = true;

                                // 创建完整的响应列表
                                List<IMessage> responses = new List<IMessage>
                                {
                                    queryState.InitialResponse,
                                };
                                responses.AddRange(queryState.DsrMessages.Select(o => (IMessage)o));

                                // 通知等待的任务
                                queryState.CompletionSource.TrySetResult(responses);

                                // 从等待列表中移除
                                _pendingQueryResponses.TryRemove(relatedMessageId, out _);

                                logService.Info(
                                    $"查询ID {relatedMessageId} 的所有数据已接收完毕，共 {queryState.DsrMessages.Count} 条DSR消息"
                                );
                            }
                            else
                            {
                                logService.Info(
                                    $"查询ID {relatedMessageId} 还有更多数据需要接收，当前已接收 {queryState.DsrMessages.Count} 条DSR消息"
                                );
                            }
                        }
                        else
                        {
                            logService.Info(
                                $"收到DSR_Q03消息，但没有找到对应的查询ID {relatedMessageId}"
                            );
                        }
                    }
                    catch (Exception ex)
                    {
                        logService.Info($"处理DSR_Q03消息时出错{ex}");
                    }
                }
               
                else
                {
                    // 处理其他类型的消息
                    if (_pendingResponses.TryRemove(messageControlId, out var tcs))
                    {
                        tcs.TrySetResult(message);
                    }
                    else
                    {
                        logService.Info(
                            $"收到ID为 {messageControlId} 的消息，但没有找到对应的等待任务"
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                logService.Info($"处理接收到的消息时出错{ex}");
            }
        }

        /// <summary>
        /// 异步查询样本信息
        /// </summary>
        /// <param name="sampleId">样本ID</param>
        /// <param name="queryType">查询类型</param>
        /// <param name="additionalParam">附加参数</param>
        /// <returns>查询结果</returns>
        public async Task<QueryResult> QuerySampleInfoAsync(
            QueryType queryType,
            string condition1,
            string condition2 = ""
        )
        {
            try
            {
                // 检查连接状态
                if (!_isConnected && !Connect())
                {
                    return new QueryResult(
                        QueryResultType.NotConnected,
                        queryType,
                        condition1,
                        condition2,
                        "未连接到服务",
                        null,
                        null,
                        null
                    );
                }

                // 创建QRY_Q02查询消息
                QRY_Q02 queryMessage = _messageBuilder.CreateQRY_Q02(
                    queryType,
                    condition1,
                    condition2
                );

                
                string messageControlId = queryMessage.MSH.MessageControlID.Value;
                try
                {
                    // 发送消息并等待初始响应
                    IMessage initialResponse = await SendAndWaitForResponseAsync(
                        queryMessage,
                        _timeout
                    );

                    // 检查响应类型
                    if (initialResponse is QCK_Q02 qck && qck.QAK.QueryResponseStatus.Value == "OK")
                    {
                        // 如果是OK响应，等待接收所有的DSR_Q03消息
                        try
                        {
                            // 等待x秒接收所有DSR_Q03消息
                            if (
                                _pendingQueryResponses.TryGetValue(
                                    messageControlId,
                                    out var queryState
                                )
                            )
                            {
                                // 等待完成或超时
                                while (!queryState.IsComplete)
                                {
                                    // 检查是否超时
                                    if ((DateTime.Now - queryState.LastMessageTime).TotalMilliseconds > queryState.TimeoutMilliseconds)
                                    {
                                        logService.Info($"等待样本信息完整接收超时，已接收 {queryState.DsrMessages.Count} 条消息，但不返回不完整数据");
                                        return new QueryResult(
                                            QueryResultType.WaitingTimeout,
                                            queryType,
                                            condition1,
                                            condition2,
                                            "接收超时，不返回不完整数据",
                                            initialResponse,
                                            new List<IMessage> { initialResponse },
                                            null
                                        );
                                    }

                                    // 等待一小段时间再检查
                                    await Task.Delay(100);
                                }

                                // 成功接收到所有数据
                                var allResponses = await queryState.CompletionSource.Task;
                                logService.Info(
                                    $"成功接收样本 {queryType} {condition1} {condition2} 的所有信息，共 {allResponses.Count - 1} 条DSR消息"
                                );
                                
                                List<ApplyTest> applyTests = new List<ApplyTest>();
                                // 尝试将所有DSR_Q03消息转换为ApplyTest对象并添加到列表中
                                foreach (var message in allResponses)
                                {
                                    if (message is DSR_Q03 dsr)
                                    {
                                        ApplyTest applyTest = UploadUtil.ConvertDsrToApplyTest(dsr);
                                        if (applyTest != null)
                                        {
                                            applyTests.Add(applyTest);
                                            logService.Info(
                                                $"成功将DSR_Q03消息转换为ApplyTest对象，条码: {applyTest.Barcode}"
                                            );
                                        }
                                    }
                                }
                                // 创建查询结果对象
                                var result = new QueryResult(
                                    QueryResultType.Success,
                                    queryType,
                                    condition1,
                                    condition2,
                                    $"查询成功，接收到 {allResponses.Count - 1} 条消息",
                                    allResponses[allResponses.Count - 1],
                                    allResponses,
                                    applyTests
                                );
                                if (result.ApplyTests.Count > 0)
                                {
                                    logService.Info(
                                        $"共转换了 {result.ApplyTests.Count} 条ApplyTest记录"
                                    );
                                }
                                else
                                {
                                    logService.Info(
                                        "未能成功转换任何ApplyTest记录，请检查消息是否包含DSR_Q03类型消息"
                                    );
                                }

                                return result;
                            }
                            else
                            {
                                logService.Info(
                                    $"未找到查询ID {messageControlId} 的等待状态，返回初始响应"
                                );
                                return new QueryResult(
                                    QueryResultType.WaitingTimeout,
                                    queryType,
                                    condition1,
                                    condition2,
                                    "未找到查询等待状态",
                                    initialResponse,
                                    new List<IMessage> { initialResponse },
                                    null
                                );
                            }
                        }
                        catch (Exception ex)
                        {
                            logService.Info($"处理查询响应时出错{ex}");
                            return new QueryResult(
                                QueryResultType.Timeout,
                                queryType,
                                condition1,
                                condition2,
                                $"处理查询响应时出错: {ex.Message}",
                                null,
                                null,
                                null
                            );
                        }
                    }
                    else if (
                        initialResponse is QCK_Q02 qckNF
                        && qckNF.QAK.QueryResponseStatus.Value == "NF"
                    )
                    {
                        // 未找到结果
                        return new QueryResult(
                            QueryResultType.NotFound,
                            queryType,
                            condition1,
                            condition2,
                            "未找到匹配的样本信息",
                            initialResponse,
                            new List<IMessage> { initialResponse },
                            null
                        );
                    }
                    else
                    {
                        // 其他响应情况
                        string status = "未知";
                        if (initialResponse is QCK_Q02 qckOther)
                        {
                            status = qckOther.QAK.QueryResponseStatus.Value;
                        }

                        return new QueryResult(
                            QueryResultType.WaitingTimeout,
                            queryType,
                            condition1,
                            condition2,
                            $"收到响应，状态: {status}",
                            initialResponse,
                            new List<IMessage> { initialResponse },
                            null
                        );
                    }
                }
                catch (TimeoutException)
                {
                    // 查询超时
                    return new QueryResult(
                        QueryResultType.Timeout,
                        queryType,
                        condition1,
                        condition2,
                        "查询超时，未收到响应",
                        null,
                        null,
                        null
                    );
                }
            }
            catch (Exception ex)
            {
                logService.Info($"查询样本  {queryType} {condition1} {condition2}  信息时发生错误{ex}");
                return new QueryResult(
                    QueryResultType.Timeout,
                    queryType,
                    condition1,
                    condition2,
                    $"发生错误: {ex.Message}",
                    null,
                    null,
                    null
                );
            }
        }

        /// <summary>
        /// 异步上传检测结果
        /// </summary>
        /// <param name="testResult">测试结果</param>
        /// <returns>上传结果</returns>
        public async Task<UploadResult> UploadTestResultAsync(TestResult testResult)
        {
            try
            {
                // 检查连接状态
                if (!_isConnected && !Connect())
                {
                    return new UploadResult(
                        UploadResultType.NotConnected,
                        testResult.Id,
                        "未连接到服务",
                        null
                    );
                }

                // 创建ORU_R01结果上传消息
                ORU_R01 oruMessage = _messageBuilder.CreateORU_R01(testResult);

                try
                {
                    // 发送消息并等待响应
                    IMessage response = await SendAndWaitForResponseAsync(oruMessage, _timeout);

                    // 检查响应类型
                    if (response is ACK ack)
                    {
                        try
                        {
                            // 检查响应状态 - 使用字符串直接处理原始消息
                            string encodedMessage = _parser.Encode(response);
                            string[] segments = encodedMessage.Split(
                                new[] { '\r' },
                                StringSplitOptions.RemoveEmptyEntries
                            );
                            string msaSegment = segments.FirstOrDefault(s => s.StartsWith("MSA"));

                            if (!string.IsNullOrEmpty(msaSegment))
                            {
                                string[] fields = msaSegment.Split('|');
                                if (fields.Length > 1)
                                {
                                    string ackCode = fields[1];
                                    string textMessage = fields.Length > 3 ? fields[3] : "";

                                    if (ackCode == "AA")
                                    {
                                        logService.Info(
                                            $"上传样本 {testResult.Barcode} 结果成功，服务端已接受"
                                        );
                                        return new UploadResult(
                                            UploadResultType.Success,
                                            testResult.Id,
                                            "上传成功",
                                            response
                                        );
                                    }
                                    else if (ackCode == "AE")
                                    {
                                        string errorMsg = $"服务端报告应用错误: {textMessage}";
                                        logService.Info(
                                            $"上传样本 {testResult.Barcode} 结果，{errorMsg}"
                                        );
                                        return new UploadResult(
                                            UploadResultType.Failed,
                                            testResult.Id,
                                            errorMsg,
                                            response
                                        );
                                    }
                                    else if (ackCode == "AR")
                                    {
                                        string errorMsg = $"服务端拒绝消息: {textMessage}";
                                        logService.Info(
                                            $"上传样本 {testResult.Barcode} 结果，{errorMsg}"
                                        );
                                        return new UploadResult(
                                            UploadResultType.Failed,
                                            testResult.Id,
                                            errorMsg,
                                            response
                                        );
                                    }
                                    else
                                    {
                                        string errorMsg = $"响应状态异常: {ackCode}";
                                        logService.Info(
                                            $"上传样本 {testResult.Barcode} 结果，{errorMsg}"
                                        );
                                        return new UploadResult(
                                            UploadResultType.Failed,
                                            testResult.Id,
                                            errorMsg,
                                            response
                                        );
                                    }
                                }
                            }

                            // 如果无法解析MSA段
                            return new UploadResult(
                                UploadResultType.Failed,
                                testResult.Id,
                                "无法解析响应消息",
                                response
                            );
                        }
                        catch (Exception ex)
                        {
                            logService.Info($"处理上传结果ACK消息时出错{ex}");
                            return new UploadResult(
                                UploadResultType.Failed,
                                testResult.Id,
                                $"处理响应时出错: {ex.Message}",
                                response
                            );
                        }
                    }
                    else
                    {
                        string errorMsg = $"响应类型异常: {response.GetType().Name}";
                        logService.Info($"上传样本 {testResult.Barcode} 结果，{errorMsg}");
                        return new UploadResult(
                            UploadResultType.Failed,
                            testResult.Id,
                            errorMsg,
                            response
                        );
                    }
                }
                catch (TimeoutException)
                {
                    string errorMsg = "等待响应超时";
                    logService.Info($"上传样本 {testResult.Barcode} 结果时{errorMsg}");
                    return new UploadResult(
                        UploadResultType.Timeout,
                        testResult.Id,
                        errorMsg,
                        null
                    );
                }
            }
            catch (Exception ex)
            {
                logService.Info($"上传样本 {testResult.Barcode} 结果时发生错误{ex}");
                return new UploadResult(
                    UploadResultType.Failed,
                    testResult.Id,
                    $"发生错误: {ex.Message}",
                    null
                );
            }
        }

        /// <summary>
        /// 发送HL7消息并等待响应
        /// </summary>
        /// <param name="message">要发送的消息</param>
        /// <param name="timeoutMilliseconds">超时时间(毫秒)</param>
        /// <returns>接收到的响应</returns>
        protected async Task<IMessage> SendAndWaitForResponseAsync(
            IMessage message,
            int timeoutMilliseconds = 30000
        )
        {
            if (!_isConnected && !Connect())
            {
                throw new InvalidOperationException("未连接到服务");
            }

            // 确保MSH段包含唯一的消息ID
            var msh = (NHapi.Model.V231.Segment.MSH)message.GetStructure("MSH");
            string messageId = msh.MessageControlID.Value;

            int retryCount = 0;
            int maxRetries = UploadConfig.Instance.OvertimeRetryCount;

            while (retryCount < maxRetries)
            {
                try
                {
                    // 创建任务完成源，用于等待响应
                    var tcs = new TaskCompletionSource<IMessage>();
                    _pendingResponses[messageId] = tcs;

                    // 编码消息
                    string encodedMessage = _parser.Encode(message);
                    encodedMessage = encodedMessage.Replace("\\S\\","^");
                    logService.Info($"发送HL7消息 (ID={messageId}, 重试次数={retryCount + 1}/{maxRetries}): {encodedMessage}");

                    // 将消息转换为MLLP格式并发送
                    byte[] messageBytes = _mllpProtocol.WrapMessage(encodedMessage);

                    // 调用子类实现的发送方法
                    await SendBytesAsync(messageBytes);

                    // 等待响应，设置超时
                    using (var cts = new CancellationTokenSource(timeoutMilliseconds))
                    {
                        var completedTask = await Task.WhenAny(
                            tcs.Task,
                            Task.Delay(timeoutMilliseconds, cts.Token)
                        );

                        if (completedTask == tcs.Task)
                        {
                            // 成功接收到响应
                            return await tcs.Task;
                        }
                        else
                        {
                            // 超时，移除等待任务
                            _pendingResponses.TryRemove(messageId, out _);
                            retryCount++;
                            
                            if (retryCount < maxRetries)
                            {
                                logService.Info($"等待消息 {messageId} 的响应超时，正在进行第 {retryCount + 1} 次重试");
                                continue;
                            }
                            else
                            {
                                logService.Info($"等待消息 {messageId} 的响应超时，已达到最大重试次数 {maxRetries}");
                                throw new TimeoutException($"等待消息 {messageId} 的响应超时，已达到最大重试次数 {maxRetries}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // 发生错误，移除等待任务
                    _pendingResponses.TryRemove(messageId, out _);

                    if (ex is TimeoutException)
                    {
                        retryCount++;
                        if (retryCount < maxRetries)
                        {
                            logService.Info($"发送消息 {messageId} 时发生超时，正在进行第 {retryCount + 1} 次重试");
                            continue;
                        }
                    }

                    logService.Info($"发送消息 {messageId} 时发生错误{ex}");
                    throw;
                }
            }

            throw new TimeoutException($"等待消息 {messageId} 的响应超时，已达到最大重试次数 {maxRetries}");
        }

        /// <summary>
        /// 发送字节数组，由子类实现具体的发送逻辑
        /// </summary>
        /// <param name="bytes">要发送的字节数组</param>
        /// <returns>异步任务</returns>
        protected abstract Task SendBytesAsync(byte[] bytes);

        /// <summary>
        /// 连接到服务器
        /// </summary>
        /// <returns>连接是否成功</returns>
        public abstract bool Connect();

        /// <summary>
        /// 断开与服务器的连接
        /// </summary>
        public abstract void Disconnect();

        /// <summary>
        /// 检查连接状态
        /// </summary>
        /// <returns>连接是否正常</returns>
        public abstract bool IsConnected();

        
    }
}
