using FluorescenceFullAutomatic.Model;
using FluorescenceFullAutomatic.Repositorys;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestResult = FluorescenceFullAutomatic.Model.TestResult;

namespace TestMain.Repositorys
{
    public class FakeTestResultRepository : ITestResultRepository
    {
        public Dictionary<int, TestResult> datas = new Dictionary<int, TestResult>();
        int id = 0;
        public FakeTestResultRepository() {
            InitData();
        }
        public void InitData() {
            id = 0;
            datas.Clear();
            Dictionary<int, TestResult> temp =  CreateData(120);
            for (int i = 0; i < temp.Keys.ToList().Count; i++)
            {
                int key = temp.Keys.ToList()[i];
                datas.Add(key, temp[key]);
            }
            
        }
        public Dictionary<int, TestResult> CreateData(int count) {
            string[] genders = {"阴性","阳性","无效" };
            Dictionary<int,TestResult> temp = new Dictionary<int,TestResult>();
            for (int i = 0; i < count; i++)
            {
                id ++;
                TestResult tr = new TestResult();
                string gender = "";
                    // 病人信息
                    tr.Patient = new Patient
                {
                    PatientName = "张三",
                    PatientGender = genders[i%3],
                    PatientAge = "35",
                    InspectDate = DateTime.Now.AddDays(1),
                    InspectDepartment = "内科",
                    InspectDoctor = "李医生",
                    TestDoctor = "王医生",
                    CheckDoctor = "赵医生"
                };

                // 项目信息
                tr.Project = new Project
                {
                    ProjectCode = "FOB",
                    ProjectName = "血红蛋白",
                    ProjectUnit = "mg/L",
                    ProjectLjz = 100,
                    BatchNum = "20240401",
                    IdentifierCode = "FBHB001",
                    A1 = 1.2,
                    A2 = 0.8,
                    X0 = 10,
                    P = 1.5,
                    ProjectUnit2 = "ng/mL",
                    ProjectLjz2 = 80,
                    ConMax = 200,
                    A12 = 1.0,
                    A22 = 0.6,
                    X02 = 8,
                    P2 = 1.2,
                    ConMax2 = 150,
                    ProjectType = Project.Project_Type_Single,
                    TestType = Project.Test_Type_Stadard,
                    IsDefault = 0,
                    ScanStart = "100",
                    ScanEnd = "500",
                    PeakWidth = "2.5",
                    PeakDistance = "5"
                };

                // 检测信息
                Random random = new Random();
                int[] randomPoints = new int[600];
                for (int j = 0; j < 600; j++)
                {
                    randomPoints[j] = random.Next(0, 5000);
                }

                tr.Point = new Point
                {
                    Points = randomPoints,
                    Location = new int[] { 1, 2 },
                    T = "10.5",
                    C = "5.2",
                    Tc = "2",
                    T2 = "8.7",
                    C2 = "4.3",
                    Tc2 = "1.5"
                };

                // 设置基本检测信息
                tr.Barcode = "BarCode" + i;
                tr.CardQRCode = "QR100200300";
                tr.TestNum = i + "";
                tr.FrameNum = "1";
                tr.TestTime = DateTime.Now.AddMonths(5*i);

                // 设置检测结果
                if (i % 3 == 0)
                {
                    tr.T = "0";
                    tr.C = "20000";
                    tr.Tc = "0";
                    tr.Con = "0";
                    tr.Result = "阴性";
                    tr.T2 = "0";
                    tr.C2 = "20000";
                    tr.Tc2 = "0";
                    tr.Con2 = "0";
                    tr.Result2 = "阴性";
                    tr.TestVerdict = "阴性";
                }
                else if (i % 3 == 1)
                {
                    tr.T = "10.5";
                    tr.C = "5.2";
                    tr.Tc = "2";
                    tr.Con = "265";
                    tr.Result = "阳性";
                    tr.T2 = "8.7";
                    tr.C2 = "4.3";
                    tr.Tc2 = "2.1";
                    tr.Con2 = "200";
                    tr.Result2 = "阳性";
                    tr.TestVerdict = "阳性";
                }
                else if (i % 3 == 2)
                {
                    tr.T = "10.5";
                    tr.C = "0";
                    tr.Tc = "0";
                    tr.Con = "0";
                    tr.Result = "无效";
                    tr.T2 = "8.7";
                    tr.C2 = "0";
                    tr.Tc2 = "0";
                    tr.Con2 = "0";
                    tr.Result2 = "无效";
                    tr.TestVerdict = "无效";
                }
                tr.Id = id;
                temp.Add(id, tr);
            }
            return temp;
        }
        public int DeleteTestResult(List<TestResult> testResults)
        {
            for (int i = 0; i < testResults.Count; i++)
            {
                datas.Remove(testResults[i].Id);
            }
            return 1;
        }

        private IQueryable<TestResult> ApplyFilter(IQueryable<TestResult> query, ConditionModel condition)
        {
            if (condition == null) return query;

            if (!string.IsNullOrEmpty(condition.ProjectName) && condition.ProjectName != "全部")
            {
                query = query.Where(tr => tr.Project.ProjectName.Contains(condition.ProjectName));
            }

            if (!string.IsNullOrEmpty(condition.TestVerdict) && condition.TestVerdict != "全部")
            {
                query = query.Where(tr => tr.TestVerdict.Contains(condition.TestVerdict));
            }

            if (!string.IsNullOrEmpty(condition.PatientName))
            {
                query = query.Where(tr => tr.Patient.PatientName.Contains(condition.PatientName));
            }

            if (condition.TestTimeMin.HasValue)
            {
                query = query.Where(tr => tr.TestTime >= condition.TestTimeMin.Value);
            }

            if (condition.TestTimeMax.HasValue)
            {
                query = query.Where(tr => tr.TestTime <= condition.TestTimeMax.Value);
            }

            //if (condition.ConcentrationMin > 0)
            //{
            //    query = query.Where(tr => {

            //        return tr.Con <= condition.ConcentrationMax && tr.Con >= condition.ConcentrationMin;
            //    });
            //}

            //if (condition.ConcentrationMax > 0)
            //{
            //    query = query.Where(tr => {
            //        return double.TryParse(tr.Con, out concentration) && concentration <= condition.ConcentrationMax;
            //    });
            //}

            if (!string.IsNullOrEmpty(condition.Barcode))
            {
                query = query.Where(tr => tr.Barcode.Contains(condition.Barcode));
            }

            return query;
        }

        public Task<List<TestResult>> GetAllTestResultAsync(ConditionModel condition)
        {
            var query = datas.Values.AsQueryable();
            query = ApplyFilter(query, condition);
            var results = query.OrderByDescending(x => x.Id).ToList();
            return Task.FromResult(results);
        }

        public async Task<List<TestResult>> GetAllTestResultAsync(ConditionModel condition, int page, int pageSize)
        {
            var query = datas.Values.AsQueryable();
            query = ApplyFilter(query, condition);
            var orderedQuery = query.OrderByDescending(x => x.Id);
            
            int startIndex = (page - 1) * pageSize;
            var pagedResults = orderedQuery.Skip(startIndex).Take(pageSize).ToList();
            return pagedResults;
        }

        public Task<int> GetAllTestResultCountAsync(ConditionModel condition)
        {
            var query = datas.Values.AsQueryable();
            query = ApplyFilter(query, condition);
            return Task.FromResult(query.Count());
        }

        public async Task<int> GetAllTestResultCountPageAsync(ConditionModel condition, int pageSize)
        {
            int totalCount = await GetAllTestResultCountAsync(condition);
            return (totalCount + pageSize - 1) / pageSize; // 向上取整
        }

        public TestResult GetTestResultForID(int id)
        {
            if (datas.TryGetValue(id, out TestResult existingResult))
            {
                return existingResult;
            }
            return null;
        }

        public TestResult GetTestResultPointForID(int id)
        {
            if (datas.TryGetValue(id, out TestResult existingResult))
            {
                return existingResult;
            }
            return null;
        }

        public int InsertTestResult(TestResult testResult)
        {
            testResult.Id = ++id;
            datas.Add(testResult.Id, testResult);
            return testResult.Id;
        }

        public bool UpdateTestResult(TestResult testResult)
        {

            if (datas.TryGetValue(testResult.Id, out TestResult existingResult)) {
                datas[testResult.Id] = testResult;
                return true;
            }
            return false;
        }
    }
}
