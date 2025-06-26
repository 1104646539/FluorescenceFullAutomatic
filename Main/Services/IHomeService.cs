using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluorescenceFullAutomatic.Config;
using FluorescenceFullAutomatic.Model;
using FluorescenceFullAutomatic.Sql;
using FluorescenceFullAutomatic.Utils;
using FluorescenceFullAutomatic.ViewModels;

namespace FluorescenceFullAutomatic.Services
{
    public interface IHomeService
    {
        bool GetIsDebug();
        int InsertTestResult(TestResult testResult);
        bool UpdateTestResult(TestResult testResult);
        TestResult GetTestResultAndPoint(int id);
        TestResult GetTestResult(int id);
        int InsertPoint(Point point);

        Task<ApplyTest> GetApplyTestAsync(int testResultId, string barcode, string testNum);
      
        void UpdateApplyTestCompleted(ApplyTest applyTest);
    }

    public class HomeService : IHomeService
    {
        public HomeService() { }

        public bool GetIsDebug()
        {
            return true;
        }

        public int InsertTestResult(TestResult testResult)
        {
            return SqlHelper.getInstance().InsertTestResult(testResult);
        }

        public bool UpdateTestResult(TestResult testResult)
        {
            return SqlHelper.getInstance().UpdateTestResult(testResult);
        }

        public TestResult GetTestResult(int id)
        {
            return SqlHelper.getInstance().GetTestResultForID(id);
        }

        public TestResult GetTestResultAndPoint(int id)
        {
            return SqlHelper.getInstance().GetTestResultAndPoint(id);
        }

        public int InsertPoint(Point point)
        {
            return SqlHelper.getInstance().InsertPoint(point);
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
            if (!string.IsNullOrEmpty(barcode)) {
                 applyTest =  SqlHelper.getInstance().GetApplyTestForBarcode(barcode);
            }
            else 
            {
                 applyTest = SqlHelper.getInstance().GetApplyTestForTestNum(testNum);
            }
            if (applyTest != null) { 
                applyTest.TestResultId = testResultId;
            }
            return applyTest;
        }


        public void UpdateApplyTestCompleted(ApplyTest applyTest)
        {
            applyTest.ApplyTestType = ApplyTestType.TestEnd;
            SqlHelper.getInstance().UpdateApplyTest(applyTest);
        }
    }
}
