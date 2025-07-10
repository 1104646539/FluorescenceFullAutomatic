using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluorescenceFullAutomatic.Model;
using FluorescenceFullAutomatic.Sql;

namespace FluorescenceFullAutomatic.Services
{
    public interface ITestResultService
    {
        int InsertTestResult(TestResult testResult);
        bool UpdateTestResult(TestResult testResult);
        TestResult GetTestResultPointForID(int id);
        TestResult GetTestResultForID(int id);
        int DeleteTestResult(List<TestResult> testResults);
        Task<List<TestResult>> GetAllTestResultAsync(ConditionModel conditio);
        Task<List<TestResult>> GetAllTestResultAsync(ConditionModel condition, int page, int pageSize);

        Task<int> GetAllTestResultCountPageAsync(ConditionModel condition, int pageSize);
        Task<int> GetAllTestResultCountAsync(ConditionModel condition);
    }
    public class TestResultRepository : ITestResultService
    {
        public TestResultRepository()
        {
        }

        public int DeleteTestResult(List<TestResult> testResults)
        {
            return SqlHelper.getInstance().DeleteTestResult(testResults);
        }

        public Task<List<TestResult>> GetAllTestResultAsync(ConditionModel conditio)
        {
            return SqlHelper.getInstance().GetAllTestResultAsync(conditio);
        }

        public Task<List<TestResult>> GetAllTestResultAsync(ConditionModel condition, int page, int pageSize)
        {
            return SqlHelper.getInstance().GetAllTestResultAsync(condition, page, pageSize);
        }

        public Task<int> GetAllTestResultCountAsync(ConditionModel condition)
        {
            return SqlHelper.getInstance().GetAllTestResultCountAsync(condition);
        }

        public async Task<int>  GetAllTestResultCountPageAsync(ConditionModel condition, int pageSize) 
        {
            int page = await GetAllTestResultCountAsync(condition);
            if(page % pageSize == 0) {
                return page / pageSize;
            }
            return (page / pageSize) + 1;
        }

        public TestResult GetTestResultForID(int id)
        {
            return SqlHelper.getInstance().GetTestResultForID(id);
        }

        public TestResult GetTestResultPointForID(int id)
        {
            return SqlHelper.getInstance().GetTestResultAndPoint(id);
        }

        public int InsertTestResult(TestResult testResult)
        {
            return SqlHelper.getInstance().InsertTestResult(testResult);
        }

        public bool UpdateTestResult(TestResult testResult)
        {
            return SqlHelper.getInstance().UpdateTestResult(testResult);
        }
    }
}
