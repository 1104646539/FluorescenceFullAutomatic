using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluorescenceFullAutomatic.Config;
using FluorescenceFullAutomatic.Model;
using NHapi.Base.Model;
using NHapi.Base.Parser;
using NHapi.Model.V231.Message;
using NHapi.Model.V231.Segment;
using Serilog;
using static Main.Upload.Hl7Result;

namespace FluorescenceFullAutomatic.Upload
{
    /// <summary>
    /// ͨ�ŷ�����࣬ʵ��HL7ͨ�ŷ���Ĺ�ͬ�߼�
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

        // ��Ϣ��Ӧ�ֵ䣬��Ϊ��ϢID��ֵΪTaskCompletionSource
        protected readonly ConcurrentDictionary<
            string,
            TaskCompletionSource<IMessage>
        > _pendingResponses = new ConcurrentDictionary<string, TaskCompletionSource<IMessage>>();

        // ��Ϣ�����¼��������б�
        protected readonly List<Action<IMessage>> _messageReceivedHandlers =
            new List<Action<IMessage>>();

        // ��ѯ��Ӧ״̬�ֵ䣬��Ϊ��ѯID��ֵΪ��ѯ��Ӧ״̬
        protected readonly ConcurrentDictionary<string, QueryResponseState> _pendingQueryResponses =
            new ConcurrentDictionary<string, QueryResponseState>();

        /// <summary>
        /// ��ѯ��Ӧ״̬�࣬���ڸ��ٲ�ѯ��Ӧ��������
        /// </summary>
        protected class QueryResponseState
        {
            // ��ʼ��Ӧ��QCK_Q02��
            public IMessage InitialResponse { get; set; }

            // �յ���DSR_Q03��Ϣ�б�
            public List<DSR_Q03> DsrMessages { get; set; }

            // �Ƿ�������
            public bool IsComplete { get; set; }

            // �������Դ
            public TaskCompletionSource<List<IMessage>> CompletionSource { get; set; }

            // ���һ���յ���Ϣ��ʱ��
            public DateTime LastMessageTime { get; set; }

            // ��ʱʱ�䣨���룩
            public int TimeoutMilliseconds { get; set; }
        }

        /// <summary>
        /// ���캯��
        /// </summary>
        protected BaseCommunicationService()
        {
            _timeout = UploadConfig.Instance.Overtime;

            // ��ȡ�����ʽ
            Encoding encoding = GetEncoding();

            _mllpProtocol = new MLLPProtocol(encoding);
            _messageBuilder = new HL7MessageBuilder();
            _parser = new PipeParser();
            _isConnected = false;
        }

        /// <summary>
        /// �������û�ȡ�����ʽ
        /// </summary>
        /// <returns>�����ʽ</returns>
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
        /// ������յ���HL7��Ϣ
        /// </summary>
        /// <param name="message">���յ�����Ϣ</param>
        protected void ProcessReceivedMessage(IMessage message)
        {
            try
            {
                // ��ȡ��Ϣͷ
                var msh = (NHapi.Model.V231.Segment.MSH)message.GetStructure("MSH");
                string messageType = msh.MessageType.MessageType.Value;
                string triggerEvent = msh.MessageType.TriggerEvent.Value;
                string messageControlId = msh.MessageControlID.Value;

                Log.Information(
                    $"������յ�����Ϣ: ����={messageType}, �¼�={triggerEvent}, ID={messageControlId}"
                );

                // ֪ͨ����ע�����Ϣ������
                foreach (var handler in _messageReceivedHandlers.ToArray())
                {
                    try
                    {
                        handler(message);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "ִ����Ϣ������ʱ����");
                    }
                }

                // ����Ƿ���ACK��Ϣ
                if (message is ACK ack)
                {
                    try
                    {
                        // ��ȡԭʼ��ϢID��Ӧ����MSA���У�
                        var msa = (MSA)ack.GetStructure("MSA");
                        string originalMessageId = msa.MessageControlID.Value;

                        // ���Ӧ��״̬ - ʹ���ַ���ֱ�Ӵ���ԭʼ��Ϣ
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
                                    Log.Information($"�յ���Ϣ {originalMessageId} �Ŀ϶�ȷ��");
                                }
                                else if (ackCode == "AE" || ackCode == "CE")
                                {
                                    Log.Warning(
                                        $"�յ���Ϣ {originalMessageId} ��Ӧ�ô���: {textMessage}"
                                    );
                                }
                                else if (ackCode == "AR" || ackCode == "CR")
                                {
                                    Log.Warning(
                                        $"�յ���Ϣ {originalMessageId} �ľܾ�: {textMessage}"
                                    );
                                }
                            }
                        }

                        // ���Խ�����ϢID���ҵ���Ӧ�ĵȴ�����
                        if (_pendingResponses.TryRemove(originalMessageId, out var tcs))
                        {
                            tcs.TrySetResult(ack);
                        }
                        else
                        {
                            Log.Warning(
                                $"�յ�IDΪ {originalMessageId} ��ACK����û���ҵ���Ӧ�ĵȴ�����"
                            );
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "����ACK��Ϣʱ����");
                    }
                }
                else if (message is QCK_Q02 qck)
                {
                    try
                    {
                        // �����ѯ��Ӧ
                        var qak = qck.QAK;
                        string queryTag = qck.MSA.MessageControlID.Value;
                        string status = qak.QueryResponseStatus.Value;

                        if (status == "OK")
                        {
                            Log.Information($"��ѯ������Ϣ�ɹ��������ݷ��أ��ȴ�������ϸ����...{queryTag}");

                            // ��¼����һ����Ҫ�ȴ�����DSR_Q03��Ϣ�Ĳ�ѯ
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
                            Log.Information($"��ѯ������Ϣ�ɹ��������ݷ���");
                        }
                        else
                        {
                            Log.Warning($"��ѯ������Ϣ����Ӧ״̬�쳣: {status}");
                        }

                        // ֪ͨ�ȴ��˲�ѯ��Ӧ������
                        if (_pendingResponses.TryRemove(queryTag, out var tcs))
                        {
                            tcs.TrySetResult(qck);
                        }
                        else
                        {
                            Log.Warning($"�յ�IDΪ {queryTag} ��QCK_Q02����û���ҵ���Ӧ�ĵȴ�����");
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "����QCK_Q02��Ϣʱ����");
                    }
                }
                else if (message is DSR_Q03 dsr)
                {
                    try
                    {
                        // ��������ACK��Ӧ
                        try
                        {
                            ACK ackResponse = _messageBuilder.CreateACK(dsr, "AA", "");
                            // ������Ϣ
                            string encodedAck = _parser.Encode(ackResponse);
                            encodedAck = encodedAck.Replace("\\S\\","^");
                            Log.Information(
                                $"����DSR_Q03��ACK��Ӧ (ID={ackResponse.MSH.MessageControlID.Value}): {encodedAck}"
                            );

                            // ����Ϣת��ΪMLLP��ʽ������
                            byte[] ackBytes = _mllpProtocol.WrapMessage(encodedAck);
                            // �첽����ACK��Ӧ
                            _ = SendBytesAsync(ackBytes);
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "����DSR_Q03��ACK��Ӧʱ����");
                        }

                        // �����Ӧ��ͨ��MSA�ֶ��ж�ID��
                        var msgMsa = (MSA)dsr.GetStructure("MSA");
                        string relatedMessageId = msgMsa.MessageControlID.Value;
                        // ����Ƿ��й����Ĳ�ѯ��Ӧ
                        if (
                            _pendingQueryResponses.TryGetValue(relatedMessageId, out var queryState)
                        )
                        {
                            // ��DSR��Ϣ��ӵ��б�
                            queryState.DsrMessages.Add(dsr);

                            // ������������Ϣʱ��
                            queryState.LastMessageTime = DateTime.Now;

                            // ����Ƿ��и��������Ҫ����
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
                                Log.Error(ex, "���DSR_Q03��Ϣ��DSC���Ƿ��и�������ʱ����");
                            }

                            if (!hasMoreSegments)
                            {
                                // û�и��������ˣ������������
                                queryState.IsComplete = true;

                                // ������������Ӧ�б�
                                List<IMessage> responses = new List<IMessage>
                                {
                                    queryState.InitialResponse,
                                };
                                responses.AddRange(queryState.DsrMessages.Select(o => (IMessage)o));

                                // ֪ͨ�ȴ�������
                                queryState.CompletionSource.TrySetResult(responses);

                                // �ӵȴ��б����Ƴ�
                                _pendingQueryResponses.TryRemove(relatedMessageId, out _);

                                Log.Information(
                                    $"��ѯID {relatedMessageId} �����������ѽ�����ϣ��� {queryState.DsrMessages.Count} ��DSR��Ϣ"
                                );
                            }
                            else
                            {
                                Log.Information(
                                    $"��ѯID {relatedMessageId} ���и���������Ҫ���գ���ǰ�ѽ��� {queryState.DsrMessages.Count} ��DSR��Ϣ"
                                );
                            }
                        }
                        else
                        {
                            Log.Warning(
                                $"�յ�DSR_Q03��Ϣ����û���ҵ���Ӧ�Ĳ�ѯID {relatedMessageId}"
                            );
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "����DSR_Q03��Ϣʱ����");
                    }
                }
               
                else
                {
                    // �����������͵���Ϣ
                    if (_pendingResponses.TryRemove(messageControlId, out var tcs))
                    {
                        tcs.TrySetResult(message);
                    }
                    else
                    {
                        Log.Warning(
                            $"�յ�IDΪ {messageControlId} ����Ϣ����û���ҵ���Ӧ�ĵȴ�����"
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "������յ�����Ϣʱ����");
            }
        }

        /// <summary>
        /// �첽��ѯ������Ϣ
        /// </summary>
        /// <param name="sampleId">����ID</param>
        /// <param name="queryType">��ѯ����</param>
        /// <param name="additionalParam">���Ӳ���</param>
        /// <returns>��ѯ���</returns>
        public async Task<QueryResult> QuerySampleInfoAsync(
            QueryType queryType,
            string condition1,
            string condition2 = ""
        )
        {
            try
            {
                // �������״̬
                if (!_isConnected && !Connect())
                {
                    return new QueryResult(
                        QueryResultType.NotConnected,
                        queryType,
                        condition1,
                        condition2,
                        "δ���ӵ�����",
                        null,
                        null,
                        null
                    );
                }

                // ����QRY_Q02��ѯ��Ϣ
                QRY_Q02 queryMessage = _messageBuilder.CreateQRY_Q02(
                    queryType,
                    condition1,
                    condition2
                );

                
                string messageControlId = queryMessage.MSH.MessageControlID.Value;
                try
                {
                    // ������Ϣ���ȴ���ʼ��Ӧ
                    IMessage initialResponse = await SendAndWaitForResponseAsync(
                        queryMessage,
                        _timeout
                    );

                    // �����Ӧ����
                    if (initialResponse is QCK_Q02 qck && qck.QAK.QueryResponseStatus.Value == "OK")
                    {
                        // �����OK��Ӧ���ȴ��������е�DSR_Q03��Ϣ
                        try
                        {
                            // �ȴ�x���������DSR_Q03��Ϣ
                            if (
                                _pendingQueryResponses.TryGetValue(
                                    messageControlId,
                                    out var queryState
                                )
                            )
                            {
                                // �ȴ���ɻ�ʱ
                                while (!queryState.IsComplete)
                                {
                                    // ����Ƿ�ʱ
                                    if ((DateTime.Now - queryState.LastMessageTime).TotalMilliseconds > queryState.TimeoutMilliseconds)
                                    {
                                        Log.Warning($"�ȴ�������Ϣ�������ճ�ʱ���ѽ��� {queryState.DsrMessages.Count} ����Ϣ���������ز���������");
                                        return new QueryResult(
                                            QueryResultType.WaitingTimeout,
                                            queryType,
                                            condition1,
                                            condition2,
                                            "���ճ�ʱ�������ز���������",
                                            initialResponse,
                                            new List<IMessage> { initialResponse },
                                            null
                                        );
                                    }

                                    // �ȴ�һС��ʱ���ټ��
                                    await Task.Delay(100);
                                }

                                // �ɹ����յ���������
                                var allResponses = await queryState.CompletionSource.Task;
                                Log.Information(
                                    $"�ɹ��������� {queryType} {condition1} {condition2} ��������Ϣ���� {allResponses.Count - 1} ��DSR��Ϣ"
                                );
                                
                                List<ApplyTest> applyTests = new List<ApplyTest>();
                                // ���Խ�����DSR_Q03��Ϣת��ΪApplyTest������ӵ��б���
                                foreach (var message in allResponses)
                                {
                                    if (message is DSR_Q03 dsr)
                                    {
                                        ApplyTest applyTest = UploadUtil.ConvertDsrToApplyTest(dsr);
                                        if (applyTest != null)
                                        {
                                            applyTests.Add(applyTest);
                                            Log.Information(
                                                $"�ɹ���DSR_Q03��Ϣת��ΪApplyTest��������: {applyTest.Barcode}"
                                            );
                                        }
                                    }
                                }
                                // ������ѯ�������
                                var result = new QueryResult(
                                    QueryResultType.Success,
                                    queryType,
                                    condition1,
                                    condition2,
                                    $"��ѯ�ɹ������յ� {allResponses.Count - 1} ����Ϣ",
                                    allResponses[allResponses.Count - 1],
                                    allResponses,
                                    applyTests
                                );
                                if (result.ApplyTests.Count > 0)
                                {
                                    Log.Information(
                                        $"��ת���� {result.ApplyTests.Count} ��ApplyTest��¼"
                                    );
                                }
                                else
                                {
                                    Log.Warning(
                                        "δ�ܳɹ�ת���κ�ApplyTest��¼��������Ϣ�Ƿ����DSR_Q03������Ϣ"
                                    );
                                }

                                return result;
                            }
                            else
                            {
                                Log.Warning(
                                    $"δ�ҵ���ѯID {messageControlId} �ĵȴ�״̬�����س�ʼ��Ӧ"
                                );
                                return new QueryResult(
                                    QueryResultType.WaitingTimeout,
                                    queryType,
                                    condition1,
                                    condition2,
                                    "δ�ҵ���ѯ�ȴ�״̬",
                                    initialResponse,
                                    new List<IMessage> { initialResponse },
                                    null
                                );
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "�����ѯ��Ӧʱ����");
                            return new QueryResult(
                                QueryResultType.Timeout,
                                queryType,
                                condition1,
                                condition2,
                                $"�����ѯ��Ӧʱ����: {ex.Message}",
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
                        // δ�ҵ����
                        return new QueryResult(
                            QueryResultType.NotFound,
                            queryType,
                            condition1,
                            condition2,
                            "δ�ҵ�ƥ���������Ϣ",
                            initialResponse,
                            new List<IMessage> { initialResponse },
                            null
                        );
                    }
                    else
                    {
                        // ������Ӧ���
                        string status = "δ֪";
                        if (initialResponse is QCK_Q02 qckOther)
                        {
                            status = qckOther.QAK.QueryResponseStatus.Value;
                        }

                        return new QueryResult(
                            QueryResultType.WaitingTimeout,
                            queryType,
                            condition1,
                            condition2,
                            $"�յ���Ӧ��״̬: {status}",
                            initialResponse,
                            new List<IMessage> { initialResponse },
                            null
                        );
                    }
                }
                catch (TimeoutException)
                {
                    // ��ѯ��ʱ
                    return new QueryResult(
                        QueryResultType.Timeout,
                        queryType,
                        condition1,
                        condition2,
                        "��ѯ��ʱ��δ�յ���Ӧ",
                        null,
                        null,
                        null
                    );
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"��ѯ����  {queryType} {condition1} {condition2}  ��Ϣʱ��������");
                return new QueryResult(
                    QueryResultType.Timeout,
                    queryType,
                    condition1,
                    condition2,
                    $"��������: {ex.Message}",
                    null,
                    null,
                    null
                );
            }
        }

        /// <summary>
        /// �첽�ϴ������
        /// </summary>
        /// <param name="testResult">���Խ��</param>
        /// <returns>�ϴ����</returns>
        public async Task<UploadResult> UploadTestResultAsync(TestResult testResult)
        {
            try
            {
                // �������״̬
                if (!_isConnected && !Connect())
                {
                    return new UploadResult(
                        UploadResultType.NotConnected,
                        testResult.Id,
                        "δ���ӵ�����",
                        null
                    );
                }

                // ����ORU_R01����ϴ���Ϣ
                ORU_R01 oruMessage = _messageBuilder.CreateORU_R01(testResult);

                try
                {
                    // ������Ϣ���ȴ���Ӧ
                    IMessage response = await SendAndWaitForResponseAsync(oruMessage, _timeout);

                    // �����Ӧ����
                    if (response is ACK ack)
                    {
                        try
                        {
                            // �����Ӧ״̬ - ʹ���ַ���ֱ�Ӵ���ԭʼ��Ϣ
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
                                        Log.Information(
                                            $"�ϴ����� {testResult.Barcode} ����ɹ���������ѽ���"
                                        );
                                        return new UploadResult(
                                            UploadResultType.Success,
                                            testResult.Id,
                                            "�ϴ��ɹ�",
                                            response
                                        );
                                    }
                                    else if (ackCode == "AE")
                                    {
                                        string errorMsg = $"����˱���Ӧ�ô���: {textMessage}";
                                        Log.Warning(
                                            $"�ϴ����� {testResult.Barcode} �����{errorMsg}"
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
                                        string errorMsg = $"����˾ܾ���Ϣ: {textMessage}";
                                        Log.Warning(
                                            $"�ϴ����� {testResult.Barcode} �����{errorMsg}"
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
                                        string errorMsg = $"��Ӧ״̬�쳣: {ackCode}";
                                        Log.Warning(
                                            $"�ϴ����� {testResult.Barcode} �����{errorMsg}"
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

                            // ����޷�����MSA��
                            return new UploadResult(
                                UploadResultType.Failed,
                                testResult.Id,
                                "�޷�������Ӧ��Ϣ",
                                response
                            );
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "�����ϴ����ACK��Ϣʱ����");
                            return new UploadResult(
                                UploadResultType.Failed,
                                testResult.Id,
                                $"������Ӧʱ����: {ex.Message}",
                                response
                            );
                        }
                    }
                    else
                    {
                        string errorMsg = $"��Ӧ�����쳣: {response.GetType().Name}";
                        Log.Warning($"�ϴ����� {testResult.Barcode} �����{errorMsg}");
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
                    string errorMsg = "�ȴ���Ӧ��ʱ";
                    Log.Error($"�ϴ����� {testResult.Barcode} ���ʱ{errorMsg}");
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
                Log.Error(ex, $"�ϴ����� {testResult.Barcode} ���ʱ��������");
                return new UploadResult(
                    UploadResultType.Failed,
                    testResult.Id,
                    $"��������: {ex.Message}",
                    null
                );
            }
        }

        /// <summary>
        /// ����HL7��Ϣ���ȴ���Ӧ
        /// </summary>
        /// <param name="message">Ҫ���͵���Ϣ</param>
        /// <param name="timeoutMilliseconds">��ʱʱ��(����)</param>
        /// <returns>���յ�����Ӧ</returns>
        protected async Task<IMessage> SendAndWaitForResponseAsync(
            IMessage message,
            int timeoutMilliseconds = 30000
        )
        {
            if (!_isConnected && !Connect())
            {
                throw new InvalidOperationException("δ���ӵ�����");
            }

            // ȷ��MSH�ΰ���Ψһ����ϢID
            var msh = (NHapi.Model.V231.Segment.MSH)message.GetStructure("MSH");
            string messageId = msh.MessageControlID.Value;

            int retryCount = 0;
            int maxRetries = UploadConfig.Instance.OvertimeRetryCount;

            while (retryCount < maxRetries)
            {
                try
                {
                    // �����������Դ�����ڵȴ���Ӧ
                    var tcs = new TaskCompletionSource<IMessage>();
                    _pendingResponses[messageId] = tcs;

                    // ������Ϣ
                    string encodedMessage = _parser.Encode(message);
                    encodedMessage = encodedMessage.Replace("\\S\\","^");
                    Log.Information($"����HL7��Ϣ (ID={messageId}, ���Դ���={retryCount + 1}/{maxRetries}): {encodedMessage}");

                    // ����Ϣת��ΪMLLP��ʽ������
                    byte[] messageBytes = _mllpProtocol.WrapMessage(encodedMessage);

                    // ��������ʵ�ֵķ��ͷ���
                    await SendBytesAsync(messageBytes);

                    // �ȴ���Ӧ�����ó�ʱ
                    using (var cts = new CancellationTokenSource(timeoutMilliseconds))
                    {
                        var completedTask = await Task.WhenAny(
                            tcs.Task,
                            Task.Delay(timeoutMilliseconds, cts.Token)
                        );

                        if (completedTask == tcs.Task)
                        {
                            // �ɹ����յ���Ӧ
                            return await tcs.Task;
                        }
                        else
                        {
                            // ��ʱ���Ƴ��ȴ�����
                            _pendingResponses.TryRemove(messageId, out _);
                            retryCount++;
                            
                            if (retryCount < maxRetries)
                            {
                                Log.Warning($"�ȴ���Ϣ {messageId} ����Ӧ��ʱ�����ڽ��е� {retryCount + 1} ������");
                                continue;
                            }
                            else
                            {
                                Log.Error($"�ȴ���Ϣ {messageId} ����Ӧ��ʱ���Ѵﵽ������Դ��� {maxRetries}");
                                throw new TimeoutException($"�ȴ���Ϣ {messageId} ����Ӧ��ʱ���Ѵﵽ������Դ��� {maxRetries}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // ���������Ƴ��ȴ�����
                    _pendingResponses.TryRemove(messageId, out _);

                    if (ex is TimeoutException)
                    {
                        retryCount++;
                        if (retryCount < maxRetries)
                        {
                            Log.Warning($"������Ϣ {messageId} ʱ������ʱ�����ڽ��е� {retryCount + 1} ������");
                            continue;
                        }
                    }

                    Log.Error(ex, $"������Ϣ {messageId} ʱ��������");
                    throw;
                }
            }

            throw new TimeoutException($"�ȴ���Ϣ {messageId} ����Ӧ��ʱ���Ѵﵽ������Դ��� {maxRetries}");
        }

        /// <summary>
        /// �����ֽ����飬������ʵ�־���ķ����߼�
        /// </summary>
        /// <param name="bytes">Ҫ���͵��ֽ�����</param>
        /// <returns>�첽����</returns>
        protected abstract Task SendBytesAsync(byte[] bytes);

        /// <summary>
        /// ���ӵ�������
        /// </summary>
        /// <returns>�����Ƿ�ɹ�</returns>
        public abstract bool Connect();

        /// <summary>
        /// �Ͽ��������������
        /// </summary>
        public abstract void Disconnect();

        /// <summary>
        /// �������״̬
        /// </summary>
        /// <returns>�����Ƿ�����</returns>
        public abstract bool IsConnected();
    }
}
