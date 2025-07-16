using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluorescenceFullAutomatic.Platform.Model;
using NHapi.Base.Model;
using static FluorescenceFullAutomatic.UploadModule.Upload.Hl7Result;

namespace FluorescenceFullAutomatic.UploadModule.Upload
{
    /// <summary>
    /// HL7ͨ�ŷ���ӿڣ�����HL7ͨ�ŵĻ�������
    /// </summary>
    public interface IHL7CommunicationService
    {
        /// <summary>
        /// ���ӵ�LIS������
        /// </summary>
        /// <returns>�����Ƿ�ɹ�</returns>
        bool Connect();

        /// <summary>
        /// �Ͽ���LIS������������
        /// </summary>
        void Disconnect();

        /// <summary>
        /// �첽��ѯ������Ϣ
        /// </summary>
        /// <param name="queryType">��ѯ����</param>
        /// <param name="condition1">���Ӳ���</param>
        /// <param name="condition2">���Ӳ���</param>
        /// <returns>��ѯ���</returns>
        Task<Hl7Result.QueryResult> QuerySampleInfoAsync(QueryType queryType, string condition1, string condition2 = "");

        /// <summary>
        /// �첽�ϴ������
        /// </summary>
        /// <param name="testResult">���Խ��</param>
        /// <returns>�ϴ����</returns>
        Task<Hl7Result.UploadResult> UploadTestResultAsync(TestResult testResult);

        /// <summary>
        /// �������״̬
        /// </summary>
        /// <returns>�����Ƿ�����</returns>
        bool IsConnected();
    }

   
} 