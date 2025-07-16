using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluorescenceFullAutomatic.Platform.Model;
using NHapi.Base.Model;
using static FluorescenceFullAutomatic.UploadModule.Upload.Hl7Result;

namespace FluorescenceFullAutomatic.UploadModule.Upload
{
    /// <summary>
    /// HL7通信服务接口，定义HL7通信的基本操作
    /// </summary>
    public interface IHL7CommunicationService
    {
        /// <summary>
        /// 连接到LIS服务器
        /// </summary>
        /// <returns>连接是否成功</returns>
        bool Connect();

        /// <summary>
        /// 断开与LIS服务器的连接
        /// </summary>
        void Disconnect();

        /// <summary>
        /// 异步查询样本信息
        /// </summary>
        /// <param name="queryType">查询类型</param>
        /// <param name="condition1">附加参数</param>
        /// <param name="condition2">附加参数</param>
        /// <returns>查询结果</returns>
        Task<Hl7Result.QueryResult> QuerySampleInfoAsync(QueryType queryType, string condition1, string condition2 = "");

        /// <summary>
        /// 异步上传检测结果
        /// </summary>
        /// <param name="testResult">测试结果</param>
        /// <returns>上传结果</returns>
        Task<Hl7Result.UploadResult> UploadTestResultAsync(TestResult testResult);

        /// <summary>
        /// 检查连接状态
        /// </summary>
        /// <returns>连接是否正常</returns>
        bool IsConnected();
    }

   
} 