using FluorescenceFullAutomatic.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluorescenceFullAutomatic.Services
{
    public interface IDataManagerService
    {
        /// <summary>
        /// 获取所有项目
        /// </summary>
        /// <returns></returns>
        List<Project> GetAllProject();
        /// <summary>
        /// 获取所有结果
        /// </summary>
        /// <returns></returns>
        Task<List<TestResult>> GetAllTestResult(ConditionModel conditio);
        Task<List<TestResult>> GetAllTestResult(ConditionModel condition,int page,int pageSize);
        TestResult GetTestResult(int id);
        Task<int> GetAllTestResultCountPage(ConditionModel condition,int pageSize);
        Task<int> GetAllTestResultCount(ConditionModel condition);
        int DeleteTestResult(List<TestResult> testResults);
    }
    public class DataManagerService : IDataManagerService
    {
        public List<Project> GetAllProject()
        {
            return Sql.SqlHelper.getInstance().GetAllProject();
        }
        public  Task<List<TestResult>> GetAllTestResult(ConditionModel condition)
        {
            return Sql.SqlHelper.getInstance().GetAllTestResultAsync(condition);
        }

        public Task<List<TestResult>> GetAllTestResult(ConditionModel condition,int page,int pageSize)
        {
            return Sql.SqlHelper.getInstance().GetAllTestResultAsync(condition,page,pageSize);
        }

        public async Task<int> GetAllTestResultCountPage(ConditionModel condition,int pageSize)
        {
            int count = await Sql.SqlHelper.getInstance().GetAllTestResultCountAsync(condition);
            int page = count / pageSize;
            if(page * pageSize < count){
                page++;
            }
            return page;
        }
        public Task<int> GetAllTestResultCount(ConditionModel condition)
        {
            return Sql.SqlHelper.getInstance().GetAllTestResultCountAsync(condition);
        }
        public TestResult GetTestResult(int id)
        {
            return Sql.SqlHelper.getInstance().GetTestResultForID(id);
        }

        public int DeleteTestResult(List<TestResult> testResults)
        {
            return Sql.SqlHelper.getInstance().DeleteTestResult(testResults);
        }
    }
}
