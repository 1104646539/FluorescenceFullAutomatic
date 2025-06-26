using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluorescenceFullAutomatic.Model;
using FluorescenceFullAutomatic.Sql;
using FluorescenceFullAutomatic.Upload;
using Main.Upload;
using static Main.Upload.Hl7Result;

namespace FluorescenceFullAutomatic.Services
{
    public interface ILisService
    {
        Task<Hl7Result.QueryResult> QueryApplyTestAsync(bool isNeedLisGet, bool isMatchingBarcode, string barcode, string testNum);
        Task<Hl7Result.QueryResult> QueryApplyTestFormTestNumAsync(string condition1, string condition2 = "");
        Task<Hl7Result.QueryResult> QueryApplyTestFormBarcodeAsync(string condition1);
        Task<Hl7Result.QueryResult> QueryApplyTestFormInspectDateAsync(string condition1, string condition2);

        Task<Hl7Result.UploadResult> UploadTestResultAsync(TestResult testResult);
    }

    public class LisService : ILisService
    {
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
            
            var applyTest = isMatchingBarcode 
                ? SqlHelper.getInstance().GetApplyTestForBarcode(barcode)
                : SqlHelper.getInstance().GetApplyTestForTestNum(barcode);

            if (applyTest == null)
            {
                if (isNeedLisGet)
                {
                    return await HL7Helper.Instance.QueryApplyTestAsync(queryType, queryValue);
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
            return await HL7Helper.Instance.QueryApplyTestAsync(QueryType.SN, condition1, condition2);
        }

        public async Task<Hl7Result.QueryResult> QueryApplyTestFormBarcodeAsync(string condition1)
        {
            return await HL7Helper.Instance.QueryApplyTestAsync(QueryType.BC, condition1);
        }

        public async Task<Hl7Result.UploadResult> UploadTestResultAsync(TestResult testResult)
        {
            return await HL7Helper.Instance.UploadTestResultAsync(testResult);
        }

        public async Task<Hl7Result.QueryResult> QueryApplyTestFormInspectDateAsync(string condition1, string condition2)
        {
            return await HL7Helper.Instance.QueryApplyTestAsync(QueryType.DT, condition1,condition2);

        }
    }
}
