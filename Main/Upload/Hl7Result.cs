using System.Collections.Generic;
using FluorescenceFullAutomatic.Model;
using NHapi.Base.Model;

namespace Main.Upload
{
    public static class Hl7Result
    {
        /// <summary>
        /// �ϴ����ʵ����
        /// </summary>
        public class UploadResult
        {
            public UploadResultType ResultType { get; }
            public int TestResultId { get; }
            public string Message { get; }
            public IMessage OriginalResponse { get; }

            public UploadResult(
                UploadResultType resultType,
                int testResultId,
                string message,
                IMessage originalResponse
            )
            {
                ResultType = resultType;
                TestResultId = testResultId;
                Message = message;
                OriginalResponse = originalResponse;
            }
        }

        /// <summary>
        /// ��ѯ���ʵ����
        /// </summary>
        public class QueryResult
        {
            public QueryResultType ResultType { get; }
            public QueryType QueryType { get; }
            public string Condition1 { get; }
            public string Condition2 { get; }
            public string Message { get; }
            public IMessage OriginalResponse { get; }
            public List<IMessage> AllResponses { get; }
            public List<ApplyTest> ApplyTests { get; set; }

            public QueryResult(
                QueryResultType resultType,
                QueryType queryType,
                string condition1,
                string condition2,
                string message,
                IMessage originalResponse,
                List<IMessage> allResponses,
                List<ApplyTest> applyTests
            )
            {
                ResultType = resultType;
                QueryType = queryType;
                Condition1 = condition1;
                Condition2 = condition2;
                Message = message;
                OriginalResponse = originalResponse;
                AllResponses = allResponses ?? new List<IMessage>();
                ApplyTests = applyTests;
            }
        }

        /// <summary>
        /// ��ѯ�������ö��
        /// </summary>
        public enum QueryResultType
        {
            /// <summary>
            /// ��ѯ�ɹ� - ���յ���������Ϣ
            /// </summary>
            Success,

            /// <summary>
            /// ��ѯδ�ҵ����
            /// </summary>
            NotFound,

            /// <summary>
            /// ��ѯ�ȴ����س�ʱ - ���յ�������Ӧ�����ڵȴ�������Ϣ
            /// </summary>
            WaitingTimeout,

            /// <summary>
            /// ��ѯ��ʱ - δ�յ���Ӧ
            /// </summary>
            Timeout,

            /// <summary>
            /// δ����
            /// </summary>
            NotConnected,
        }

        public enum QueryType
        {
            /// <summary>
            /// �����
            /// </summary>
            SN,

            /// <summary>
            /// ����
            /// </summary>
            BC,

            /// <summary>
            /// �ͼ�ʱ��
            /// </summary>
            DT,
        }

        /// <summary>
        /// �ϴ��������ö��
        /// </summary>
        public enum UploadResultType
        {
            /// <summary>
            /// �ϴ��ɹ�
            /// </summary>
            Success,

            /// <summary>
            /// �ϴ�ʧ��
            /// </summary>
            Failed,

            /// <summary>
            /// �ϴ���ʱ
            /// </summary>
            Timeout,

            /// <summary>
            /// δ����
            /// </summary>
            NotConnected,
        }
    }
}
