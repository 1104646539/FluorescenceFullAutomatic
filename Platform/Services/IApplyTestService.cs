using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluorescenceFullAutomatic.Platform.Model;
using FluorescenceFullAutomatic.Platform.Sql;

namespace FluorescenceFullAutomatic.Platform.Services
{
    public interface IApplyTestService
    {
        Task<List<ApplyTest>> GetApplyTests(ApplyTestType applyTestType);

        int InsertApplyTest(ApplyTest applyTest);

        bool UpdateApplyTest(ApplyTest applyTest);

        bool DeleteApplyTest(ApplyTest applyTest);

        ApplyTest GetApplyTestForID(int id);

        void UpdateApplyTestCompleted(ApplyTest applyTest);

        Task<ApplyTest> GetApplyTestAsync(int testResultId, string barcode, string testNum);

    }
    public class ApplyTestService : IApplyTestService
    {
        public ApplyTestService()
        {
        }

        public bool DeleteApplyTest(ApplyTest applyTest)
        {
            return SqlHelper.getInstance().DeleteApplyTest(applyTest);
        }

        public ApplyTest GetApplyTestForID(int id)
        {
            return SqlHelper.getInstance().GetApplyTestForID(id);
        }

        public Task<List<ApplyTest>> GetApplyTests(ApplyTestType applyTestType)
        {
            return SqlHelper.getInstance().GetAllApplyTest(applyTestType);
        }

        public int InsertApplyTest(ApplyTest applyTest)
        {
            return SqlHelper.getInstance().InsertApplyTest(applyTest);
        }



        public bool UpdateApplyTest(ApplyTest applyTest)
        {
            return SqlHelper.getInstance().UpdateApplyTest(applyTest);
        }

        public void UpdateApplyTestCompleted(ApplyTest applyTest)
        {
            applyTest.ApplyTestType = ApplyTestType.TestEnd;
            SqlHelper.getInstance().UpdateApplyTest(applyTest);
        }
        /// <summary>
        /// 根据条码或检测编号， 获取申请检测信息
        /// 获取规则，先从Lis远端获取，如果没有，则从本地数据库获取
        /// 1、先从条码获取
        /// 2、如果没有条码，则从检测编号获取
        /// </summary>
        /// <param name="testResultId"></param>
        /// <param name="barcode"></param>
        /// <param name="testNum"></param>
        /// <returns></returns>
        public async Task<ApplyTest> GetApplyTestAsync(int testResultId, string barcode, string testNum)
        {
            if (string.IsNullOrEmpty(barcode) && string.IsNullOrEmpty(barcode))
            {
                return null;
            }
            ApplyTest applyTest;
            if (!string.IsNullOrEmpty(barcode))
            {
                applyTest = SqlHelper.getInstance().GetApplyTestForBarcode(barcode);
            }
            else
            {
                applyTest = SqlHelper.getInstance().GetApplyTestForTestNum(testNum);
            }
            if (applyTest != null)
            {
                applyTest.TestResultId = testResultId;
            }
            return applyTest;
        }

    }
}
