using System.Collections.Generic;
using FluorescenceFullAutomatic.Model;
using NHapi.Base.Model;

namespace Main.Upload
{
    public static class Hl7Result
    {
        /// <summary>
        /// 上传结果实现类
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
        /// 查询结果实现类
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
        /// 查询结果类型枚举
        /// </summary>
        public enum QueryResultType
        {
            /// <summary>
            /// 查询成功 - 接收到完整的信息
            /// </summary>
            Success,

            /// <summary>
            /// 查询未找到结果
            /// </summary>
            NotFound,

            /// <summary>
            /// 查询等待返回超时 - 已收到部分响应或正在等待后续消息
            /// </summary>
            WaitingTimeout,

            /// <summary>
            /// 查询超时 - 未收到响应
            /// </summary>
            Timeout,

            /// <summary>
            /// 未连接
            /// </summary>
            NotConnected,
        }

        public enum QueryType
        {
            /// <summary>
            /// 检测编号
            /// </summary>
            SN,

            /// <summary>
            /// 条码
            /// </summary>
            BC,

            /// <summary>
            /// 送检时间
            /// </summary>
            DT,
        }

        /// <summary>
        /// 上传结果类型枚举
        /// </summary>
        public enum UploadResultType
        {
            /// <summary>
            /// 上传成功
            /// </summary>
            Success,

            /// <summary>
            /// 上传失败
            /// </summary>
            Failed,

            /// <summary>
            /// 上传超时
            /// </summary>
            Timeout,

            /// <summary>
            /// 未连接
            /// </summary>
            NotConnected,
        }
    }
}
