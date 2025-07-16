using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluorescenceFullAutomatic.Platform.Core.Config;
using FluorescenceFullAutomatic.Platform.Model;
using FluorescenceFullAutomatic.Platform.Sql;
using FluorescenceFullAutomatic.UploadModule.Upload;
using static FluorescenceFullAutomatic.UploadModule.Upload.Hl7Result;

namespace FluorescenceFullAutomatic.Platform.Services
{
    public interface ILisService
    {
        Task<Hl7Result.QueryResult> QueryApplyTestAsync(bool isNeedLisGet, bool isMatchingBarcode, string barcode, string testNum);
        Task<Hl7Result.QueryResult> QueryApplyTestFormTestNumAsync(string condition1, string condition2 = "");
        Task<Hl7Result.QueryResult> QueryApplyTestFormBarcodeAsync(string condition1);
        Task<Hl7Result.QueryResult> QueryApplyTestFormInspectDateAsync(string condition1, string condition2);
        Task<Hl7Result.UploadResult> UploadTestResultAsync(TestResult testResult);
        void UploadTestResult(List<TestResult> testResults, Action<List<Hl7Result.UploadResult>> callback);
        // 基本配置
        bool GetOpenUpload();
        void SetOpenUpload(bool value);

        bool GetAutoUpload();
        void SetAutoUpload(bool value);

        bool GetAutoReconnection();
        void SetAutoReconnection(bool value);

        int GetUploadIntervalTime();
        void SetUploadIntervalTime(int value);

        bool GetTwoWay();
        void SetTwoWay(bool value);

        int GetOvertimeRetryCount();
        void SetOvertimeRetryCount(int value);

        int GetOvertime();
        void SetOvertime(int value);

        bool GetAutoGetApplyTest();
        void SetAutoGetApplyTest(bool value);

        bool GetMatchBarcode();
        void SetMatchBarcode(bool value);

        // 通讯方式
        bool GetSerialPort();
        void SetSerialPort(bool value);

        // 编码设置
        string GetCharset();
        void SetCharset(string value);

        // 串口设置
        string GetBaudRate();
        void SetBaudRate(string value);

        string GetDataBit();
        void SetDataBit(string value);

        string GetStopBit();
        void SetStopBit(string value);

        string GetOddEven();
        void SetOddEven(string value);

        // 网口设置
        string GetServiceIP();
        void SetServiceIP(string value);

        string GetServicePort();
        void SetServicePort(string value);

        // 连接管理
        void Connect();
        void Disconnect();
        bool IsConnected();

        // HL7Helper 连接事件处理
        void AddConnectionSucceededHandler(ConnectionStatusChangedHandler handler);
        void RemoveConnectionSucceededHandler(ConnectionStatusChangedHandler handler);
        void AddConnectionClosedHandler(ConnectionStatusChangedHandler handler);
        void RemoveConnectionClosedHandler(ConnectionStatusChangedHandler handler);
        void AddConnectionFailedHandler(ConnectionErrorHandler handler);
        void RemoveConnectionFailedHandler(ConnectionErrorHandler handler);

        // 保存所有设置
        void SaveAllSettings(
            bool openUpload,
            bool autoUpload,
            bool autoReconnection,
            int uploadIntervalTime,
            bool twoWay,
            int overtimeRetryCount,
            int overtime,
            bool autoGetApplyTest,
            bool matchBarcode,
            bool serialPort,
            string charset,
            string baudRate,
            string dataBit,
            string stopBit,
            string oddEven,
            string serviceIP,
            string servicePort
        );
    }
    public class LisService : ILisService
    {
        HL7Helper hL7Helper ;
        public LisService(ILogService logService) {
            hL7Helper = new HL7Helper(logService);
        }
        /// <summary>
        /// 根据条码或编号查询申请检验信息
        /// </summary>
        /// <param name="isNeedLisGet">是否需要从LIS系统获取数据</param>
        /// <param name="isMatchingBarcode">是否使用条码匹配</param>
        /// <param name="barcode">条码</param>
        /// <param name="testNum">检验单号</param>
        /// <returns>查询结果</returns>
        public async Task<Hl7Result.QueryResult> QueryApplyTestAsync(bool isNeedLisGet, bool isMatchingBarcode, string barcode, string testNum)
        {
            var queryType = isMatchingBarcode ? QueryType.BC : QueryType.SN;
            var queryValue = isMatchingBarcode ? barcode : testNum;

            ApplyTest applyTest = isMatchingBarcode
                ? SqlHelper.getInstance().GetApplyTestForBarcode(barcode)
                : SqlHelper.getInstance().GetApplyTestForTestNum(barcode);

            if (applyTest == null)
            {
                if (isNeedLisGet)
                {
                    return await hL7Helper.QueryApplyTestAsync(queryType, queryValue);
                }

                return new QueryResult(
                    QueryResultType.NotFound,
                    queryType,
                    queryValue,
                    "",
                    "数据库获取失败",
                    null,
                    null,
                    null);
            }

            return new QueryResult(
                QueryResultType.Success,
                queryType,
                queryValue,
                "",
                "数据库获取成功",
                null,
                null,
                new List<ApplyTest>() { applyTest });
        }

        public async Task<Hl7Result.QueryResult> QueryApplyTestFormTestNumAsync(string condition1, string condition2 = "")
        {
            return await hL7Helper.QueryApplyTestAsync(QueryType.SN, condition1, condition2);
        }

        public async Task<Hl7Result.QueryResult> QueryApplyTestFormBarcodeAsync(string condition1)
        {
            return await hL7Helper.QueryApplyTestAsync(QueryType.BC, condition1);
        }

        public async Task<Hl7Result.UploadResult> UploadTestResultAsync(TestResult testResult)
        {
            return await hL7Helper.UploadTestResultAsync(testResult);
        }

        public async Task<Hl7Result.QueryResult> QueryApplyTestFormInspectDateAsync(string condition1, string condition2)
        {
            return await hL7Helper.QueryApplyTestAsync(QueryType.DT, condition1, condition2);

        }

        // 基本配置
        public bool GetOpenUpload() => UploadConfig.Instance.OpenUpload;
        public void SetOpenUpload(bool value) => UploadConfig.Instance.OpenUpload = value;

        public bool GetAutoUpload() => UploadConfig.Instance.AutoUpload;
        public void SetAutoUpload(bool value) => UploadConfig.Instance.AutoUpload = value;

        public bool GetAutoReconnection() => UploadConfig.Instance.AutoReconnection;
        public void SetAutoReconnection(bool value) => UploadConfig.Instance.AutoReconnection = value;

        public int GetUploadIntervalTime() => UploadConfig.Instance.UploadIntervalTime;
        public void SetUploadIntervalTime(int value) => UploadConfig.Instance.UploadIntervalTime = value;

        public bool GetTwoWay() => UploadConfig.Instance.TwoWay;
        public void SetTwoWay(bool value) => UploadConfig.Instance.TwoWay = value;

        public int GetOvertimeRetryCount() => UploadConfig.Instance.OvertimeRetryCount;
        public void SetOvertimeRetryCount(int value) => UploadConfig.Instance.OvertimeRetryCount = value;

        public int GetOvertime() => UploadConfig.Instance.Overtime;
        public void SetOvertime(int value) => UploadConfig.Instance.Overtime = value;

        public bool GetAutoGetApplyTest() => UploadConfig.Instance.AutoGetApplyTest;
        public void SetAutoGetApplyTest(bool value) => UploadConfig.Instance.AutoGetApplyTest = value;

        public bool GetMatchBarcode() => UploadConfig.Instance.MatchBarcode;
        public void SetMatchBarcode(bool value) => UploadConfig.Instance.MatchBarcode = value;

        // 通讯方式
        public bool GetSerialPort() => UploadConfig.Instance.SerialPort;
        public void SetSerialPort(bool value) => UploadConfig.Instance.SerialPort = value;

        // 编码设置
        public string GetCharset() => UploadConfig.Instance.Charset;
        public void SetCharset(string value) => UploadConfig.Instance.Charset = value;

        // 串口设置
        public string GetBaudRate() => UploadConfig.Instance.BaudRate;
        public void SetBaudRate(string value) => UploadConfig.Instance.BaudRate = value;

        public string GetDataBit() => UploadConfig.Instance.DataBit;
        public void SetDataBit(string value) => UploadConfig.Instance.DataBit = value;

        public string GetStopBit() => UploadConfig.Instance.StopBit;
        public void SetStopBit(string value) => UploadConfig.Instance.StopBit = value;

        public string GetOddEven() => UploadConfig.Instance.OddEven;
        public void SetOddEven(string value) => UploadConfig.Instance.OddEven = value;

        // 网口设置
        public string GetServiceIP() => UploadConfig.Instance.ServiceIP;
        public void SetServiceIP(string value) => UploadConfig.Instance.ServiceIP = value;

        public string GetServicePort() => UploadConfig.Instance.ServicePort;
        public void SetServicePort(string value) => UploadConfig.Instance.ServicePort = value;

        // 连接管理
        public void Connect()
        {
            hL7Helper.InitializeService();
        }

        public void Disconnect()
        {
            hL7Helper.Disconnect();
        }

        public bool IsConnected() => hL7Helper.IsConnected();

        // HL7Helper 连接事件处理
        public void AddConnectionSucceededHandler(ConnectionStatusChangedHandler handler) => hL7Helper.AddConnectionSucceededHandler(handler);
        public void RemoveConnectionSucceededHandler(ConnectionStatusChangedHandler handler) => hL7Helper.RemoveConnectionSucceededHandler(handler);
        public void AddConnectionClosedHandler(ConnectionStatusChangedHandler handler) => hL7Helper.AddConnectionClosedHandler(handler);
        public void RemoveConnectionClosedHandler(ConnectionStatusChangedHandler handler) => hL7Helper.RemoveConnectionClosedHandler(handler);
        public void AddConnectionFailedHandler(ConnectionErrorHandler handler) => hL7Helper.AddConnectionFailedHandler(handler);
        public void RemoveConnectionFailedHandler(ConnectionErrorHandler handler) => hL7Helper.RemoveConnectionFailedHandler(handler);

        // 保存所有设置
        public void SaveAllSettings(
            bool openUpload,
            bool autoUpload,
            bool autoReconnection,
            int uploadIntervalTime,
            bool twoWay,
            int overtimeRetryCount,
            int overtime,
            bool autoGetApplyTest,
            bool matchBarcode,
            bool serialPort,
            string charset,
            string baudRate,
            string dataBit,
            string stopBit,
            string oddEven,
            string serviceIP,
            string servicePort
        )
        {
            var config = UploadConfig.Instance;
            config.OpenUpload = openUpload;
            config.AutoUpload = autoUpload;
            config.AutoReconnection = autoReconnection;
            config.UploadIntervalTime = uploadIntervalTime;
            config.TwoWay = twoWay;
            config.OvertimeRetryCount = overtimeRetryCount;
            config.Overtime = overtime;
            config.AutoGetApplyTest = autoGetApplyTest;
            config.MatchBarcode = matchBarcode;
            config.SerialPort = serialPort;
            config.Charset = charset;
            config.BaudRate = baudRate;
            config.DataBit = dataBit;
            config.StopBit = stopBit;
            config.OddEven = oddEven;
            config.ServiceIP = serviceIP;
            config.ServicePort = servicePort;

            hL7Helper.InitializeService();
        }

        public void UploadTestResult(List<TestResult> testResults, Action<List<Hl7Result.UploadResult>> callback)
        {
            hL7Helper.UploadTestResult(testResults, callback);
        }
    }
}
