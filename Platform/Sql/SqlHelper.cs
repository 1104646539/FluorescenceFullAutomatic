using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using FluorescenceFullAutomatic.Core.Model;
using FluorescenceFullAutomatic.Platform.Model;
using FluorescenceFullAutomatic.Platform.Utils;
using Serilog;
using SqlSugar;
using Point = FluorescenceFullAutomatic.Platform.Model.Point;

namespace FluorescenceFullAutomatic.Platform.Sql
{
    public class SqlHelper
    {
        static SqlHelper instance;
        public static string ConnectionString = @"DataSource=result.db";
        private static readonly object _getTestResultLock = new object();
        static DB db = null;
        public static String CODE_FOB2 = "FOB2";
        public static String CODE_TRF2 = "TRF2";
        public static String CODE_CAL2 = "CAL2";
        public static String CODE_LF2 = "LF2";
        public static String CODE_QC = "QC";
        public static String CODE_DQC = "DQC";
        public static String CODE_DFT = "DFT";
        private static String[] codes = new String[]
        {
            CODE_FOB2,
            CODE_TRF2,
            CODE_CAL2,
            CODE_LF2,
            CODE_QC,
        };
        public static String[] projects = new String[]
        {
            GlobalUtil.GetString(Keys.ProjectFOB),
            GlobalUtil.GetString(Keys.ProjectTRF),
            GlobalUtil.GetString(Keys.ProjectCAL),
            GlobalUtil.GetString(Keys.ProjectLF),
            GlobalUtil.GetString(Keys.ProjectQC),
        };
        private static String[] showCardNames = new String[] { "Fob", "Trf", "Cal", "Lf", "QC" };
        private static int[] conMax = new int[] { 4800, 4800, 4800, 4800, 4800 };
        public static String[] codes2 = new String[] { CODE_DFT, CODE_DQC };
        public static String[] projects2 = new String[]
        {
            GlobalUtil.GetString(Keys.ProjectFOBTRF),
            GlobalUtil.GetString(Keys.ProjectDQC),
        };
        private static String[] showCardNames2 = new String[] { "Fob/Trf", "QC(Fob)/QC(Trf)" };
        private static int[] conMaxd = new int[] { 4800, 4800 };
        private static int[] conMaxd2 = new int[] { 2500, 2500 };
        private static int[] ljz = new int[] { 100, 40, 100, 100, 100 };
        private static int[] ljzd = new int[] { 100, 100 };
        private static int[] ljzd2 = new int[] { 40, 40 };
        public static int DEFAULT_SCAN_START = 650;
        public static int DEFAULT_SCAN_END = 1200;
        public static int DEFAULT_SCAN_PEAKWIDTH = 200;
        public static int DEFAULT_SCAN_PEAKDISTANCE = 210;
        public static int DEFAULT_SCAN_START_D = 470;
        public static int DEFAULT_SCAN_END_D = 1250;
        public static int DEFAULT_SCAN_PEAKWIDTH_D = 160;
        public static int DEFAULT_SCAN_PEAKDISTANCE_D = 180;
        public static String DEFAULT_CONCENTRATEUNIT = "ng/mL";

        /**
    * 双联卡对应的几次幂
    */
        static String[] alphabets = new String[]
        {
            "0",
            "1",
            "2",
            "3",
            "4",
            "5",
            "6",
            "7",
            "8",
            "9",
            "a",
            "b",
            "c",
            "d",
            "e",
            "f",
            "g",
            "i",
            "j",
            "k",
            "l",
            "m",
            "n",
            "o",
            "p",
            "q",
            "r",
            "s",
            "t",
            "u",
            "v",
            "w",
            "x",
            "y",
            "z",
        };
        static Dictionary<String, int> alphabet = new Dictionary<String, int>();

        static SqlHelper()
        {
            for (int i = 0; i < alphabets.Length; i++)
            {
                alphabet.Add(alphabets[i], i);
            }
        }
        private SqlHelper()
        {
            //@"DataSource=" + Environment.CurrentDirectory + @"\DB\test.db"
            db = new DB(DbType.Sqlite, ConnectionString);

            CodeFirstInit();
        }

        private void CodeFirstInit()
        {
            db.Db.CopyNew().DbMaintenance.CreateDatabase();
            db.Db.CopyNew().CodeFirst.InitTables<TestResult, Project, Point, Patient, ApplyTest>();
            db.Db.CopyNew().Aop.OnError = (exp) => //SQL报错
            {
                //获取原生SQL推荐 5.1.4.63  性能OK
                //UtilMethods.GetNativeSql(exp.Sql, exp.Parametres);

                //获取无参数SQL对性能有影响，特别大的SQL参数多的，调试使用
                string err = UtilMethods.GetSqlString(
                    DbType.Sqlite,
                    exp.Sql,
                    exp.Parametres as SugarParameter[]
                );
                Log.Information($"SqlHelper CodeFirstInit err={err} exp={exp.Message}");
            };
            InitDB();
        }

        private void InitDB()
        {
            //SqlVersion sqlVersion =  GetSqlVersion(Application.ResourceAssembly.GetName().Version.ToString());
            //if (sqlVersion == null) {

            //}
            InitProject();
        }

   

        private void InitProject()
        {
            List<Project> oldProject = GetProjectForType(true);
            if (oldProject.Count > 0) return;
            List<Project> savaProjects = new List<Project>();

            for (int i = 0; i < codes.Length; i++)
            {
                Project project = new Project();
                for (int j = 0; j < oldProject.Count; j++)
                {
                    if (oldProject[j].ProjectCode == codes[i])
                    {
                        project = oldProject[j];
                        break;
                    }
                }
                project.ProjectCode = codes[i];
                project.ProjectName = projects[i];
                project.ScanStart = DEFAULT_SCAN_START + "";
                project.ScanEnd = DEFAULT_SCAN_END + "";
                project.PeakWidth = DEFAULT_SCAN_PEAKWIDTH + "";
                project.PeakDistance = DEFAULT_SCAN_PEAKDISTANCE + "";
                project.ProjectUnit = DEFAULT_CONCENTRATEUNIT;
                project.ProjectLjz = ljz[i];
                project.IsDefault = 1;
                project.ConMax = conMax[i];
                project.TestType =
                    codes[i] == CODE_QC ? Project.Test_Type_QC : Project.Test_Type_Stadard;
                project.ProjectType = Project.Project_Type_Single;

                savaProjects.Add(project);
            }
            for (int i = 0; i < codes2.Length; i++)
            {
                Project project = new Project();
                for (int j = 0; j < oldProject.Count; j++)
                {
                    if (oldProject[j].ProjectCode == codes[i])
                    {
                        project = oldProject[j];
                        break;
                    }
                }
                project.ProjectCode = codes2[i];
                project.ProjectName = projects2[i];
                project.ScanStart = DEFAULT_SCAN_START_D + "";
                project.ScanEnd = DEFAULT_SCAN_END_D + "";
                project.PeakWidth = DEFAULT_SCAN_PEAKWIDTH_D + "";
                project.PeakDistance = DEFAULT_SCAN_PEAKDISTANCE_D + "";

                project.ProjectUnit = DEFAULT_CONCENTRATEUNIT;
                project.ProjectUnit2 = DEFAULT_CONCENTRATEUNIT;
                project.ProjectLjz = ljz[i];
                project.ProjectLjz2 = ljzd[i];
                project.IsDefault = 1;
                project.ConMax = conMaxd[i];
                project.ConMax2 = conMaxd2[i];
                project.TestType =
                    codes[i] == CODE_DQC ? Project.Test_Type_QC : Project.Test_Type_Stadard;
                project.ProjectType = Project.Project_Type_Double;

                savaProjects.Add(project);
            }
            for (int i = 0; i < savaProjects.Count; i++)
            {
                InsertProject(savaProjects[i]);
            }
        }

        public static SqlHelper getInstance()
        {
            if (instance == null)
            {
                instance = new SqlHelper();
            }
            return instance;
        }

        public DB GetDB()
        {
            return db;
        }


        private string transformA1(string d)
        {
            String temp = d.Substring(0, d.Length - 1);
            String sindex = d.Substring(d.Length - 1);
            if (sindex.Equals("0"))
            {
                temp = "-" + temp;
            }
            return temp;
        }

        private static String transformA2(String d)
        {
            String temp = d.Substring(0, d.Length - 1);
            String sindex = d.Substring(d.Length - 1);
            int index = getIndex(sindex);

            Log.Information("temp=" + temp + " index=" + index);

            Decimal re = new Decimal(double.Parse(temp));
            //如果是0，则取原数据
            if (index > 0)
            {
                re = Decimal.Multiply(re, new Decimal(Math.Pow(10, index)));
            }
            Log.Information("re=" + re);
            return re.ToString();
        }

        private static int getIndex(String sindex)
        {
            int index = alphabet[sindex];

            return index;
        }

        #region AboutPoint

        /// <summary>
        /// 插入图像
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public int InsertPoint(Point point)
        {
            return (int)db.Db.CopyNew().Insertable(point).ExecuteReturnBigIdentity();
        }

        /// <summary>
        /// 获取图像
        /// </summary>
        /// <param name="pointId"></param>
        /// <returns></returns>
        public Point GetPoint(int pointId)
        {
            return db.Db.CopyNew().Queryable<Point>().Where(p => p.Id == pointId).Single();
        }

        /// <summary>
        /// 更新图像
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public bool UpdatePoint(Point point)
        {
            return db.Db.CopyNew().Updateable(point).ExecuteCommand() > 0;
        }

        #endregion

        #region AboutProject
        /// <summary>
        /// 获取项目
        /// </summary>
        /// <param name="qrCode"></param>
        /// <param name="isMult"></param>
        /// <returns></returns>
        public Project GetProjectForQRCode(string qrCode)
        {
            try
            {
                // 获取项目代号
                string projectCode = "";
                string batchNum = "";
                string identifierCode = "";
                string a1 = "0";
                string a2 = "0";
                string x0 = "0";
                string p = "0";
                string a12 = "0";
                string a22 = "0";
                string x02 = "0";
                string p2 = "0";
                if (GlobalUtil.IsDoubleCard(qrCode) || GlobalUtil.IsDoubleQCCard(qrCode))
                {
                    //双联卡
                    projectCode = qrCode.Substring(0, 3);
                    batchNum = "202" + qrCode.Substring(3, 5);
                    identifierCode = qrCode.Substring(8, 5);
                    a1 = qrCode.Substring(13, 5);
                    a2 = qrCode.Substring(18, 5);
                    x0 = qrCode.Substring(23, 5);
                    p = qrCode.Substring(28, 4);
                    a12 = qrCode.Substring(32, 5);
                    a22 = qrCode.Substring(37, 5);
                    x02 = qrCode.Substring(42, 5);
                    p2 = qrCode.Substring(47, 4);

                    a1 = transformA1(a1);
                    a2 = transformA2(a2);
                    x0 = transformA2(x0);

                    a12 = transformA1(a12);
                    a22 = transformA2(a22);
                    x02 = transformA2(x02);
                }
                else if (GlobalUtil.IsSingleCard(qrCode) || GlobalUtil.IsSingleQCCard(qrCode))
                {
                    //单联卡
                    string[] parts = qrCode.Split(',');
                    if (parts.Length < 7)
                    {
                        return null;
                    }
                    // 获取项目代号
                    projectCode = parts[0];
                    batchNum = parts[1];
                    identifierCode = parts[2];
                    a1 = parts[3];
                    a2 = parts[4];
                    x0 = parts[5];
                    p = parts[6];
                }
                else
                {
                    //既不是双联卡也不是单联卡
                    Log.Information(
                        $"SqlHelper GetProjectForQRCode qrCode={qrCode} 不是双联卡也不是单联卡"
                    );
                    return null;
                }
                if (string.IsNullOrEmpty(projectCode) || string.IsNullOrEmpty(batchNum))
                {
                    Log.Information($"SqlHelper IsNullOrEmpty qrCode={qrCode}");
                    return null;
                }
                //先查找是否有此项目 根据代号，批号，是否默认
                List<Project> ret = db
                    .Db.Queryable<Project>()
                    .Where(p =>
                        p.ProjectCode.ToUpper() == projectCode.ToUpper()
                        && p.BatchNum == batchNum
                        && p.IsDefault == 0
                    )
                    .ToList();

                if (ret.Count > 0)
                {
                    if (GlobalUtil.IsDoubleQCCard(qrCode) || GlobalUtil.IsSingleQCCard(qrCode))
                    {
                        //如果是质控卡，需要用卡上的参数质控
                        Project project = ret[0];
                        project.ScanStart = a1;
                        project.ScanEnd = a2;
                        project.PeakWidth = x0;
                        project.PeakDistance = p;
                        UpdateProject(project);
                        return project;
                    }
                    return ret[0];
                }
                else
                { //如果没有此项目，则查找默认项目
                    List<Project> defRet = db
                        .Db.Queryable<Project>()
                        .Where(p =>
                            p.ProjectCode.ToUpper() == projectCode.ToUpper() && p.IsDefault == 1
                        )
                        .ToList();

                    if (defRet.Count > 0)
                    {
                        //如果有默认项目，则创建此项目保存并返回
                        Project defaultProject = defRet[0];

                        if (GlobalUtil.IsDoubleQCCard(qrCode) || GlobalUtil.IsSingleQCCard(qrCode))
                        {
                            //是质控卡，需要使用卡上的参数
                            Project project = new Project()
                            {
                                ProjectName = defaultProject.ProjectName,
                                ProjectCode = defaultProject.ProjectCode,
                                ProjectLjz = defaultProject.ProjectLjz,
                                ProjectUnit = defaultProject.ProjectUnit,
                                BatchNum = batchNum,
                                IdentifierCode = identifierCode,
                                ProjectType = defaultProject.ProjectType,
                                TestType = defaultProject.TestType,
                                ProjectLjz2 = defaultProject.ProjectLjz2,
                                ProjectUnit2 = defaultProject.ProjectUnit2,
                                ScanStart = a1,
                                ScanEnd = a2,
                                PeakWidth = x0,
                                PeakDistance = p,
                            };
                            int id = InsertProject(project);
                            project.Id = id;
                            return project;
                        }
                        else
                        {
                            //是普通卡，需要使用卡上的参数
                            Project project = new Project()
                            {
                                ProjectName = defaultProject.ProjectName,
                                ProjectCode = defaultProject.ProjectCode,
                                ProjectLjz = defaultProject.ProjectLjz,
                                ProjectUnit = defaultProject.ProjectUnit,
                                ConMax = defaultProject.ConMax,
                                BatchNum = batchNum,
                                IdentifierCode = identifierCode,
                                ProjectType = defaultProject.ProjectType,
                                TestType = defaultProject.TestType,
                                A1 = double.Parse(a1),
                                A2 = double.Parse(a2),
                                X0 = double.Parse(x0),
                                P = double.Parse(p),
                                A12 = double.Parse(a12),
                                A22 = double.Parse(a22),
                                X02 = double.Parse(x02),
                                P2 = double.Parse(p2),
                                ProjectLjz2 = defaultProject.ProjectLjz2,
                                ProjectUnit2 = defaultProject.ProjectUnit2,
                                ScanStart = defaultProject.ScanStart,
                                ScanEnd = defaultProject.ScanEnd,
                                PeakWidth = defaultProject.PeakWidth,
                                PeakDistance = defaultProject.PeakDistance,
                                ConMax2 = defaultProject.ConMax2,
                            };
                            int id = InsertProject(project);
                            project.Id = id;
                            return project;
                        }
                    }
                    else
                    {
                        //如果没有此项目，则返回null
                        Log.Information(
                            $"SqlHelper GetProject projectCode={projectCode} batchNum={batchNum} 没有此项目"
                        );
                    }
                    return null;
                }
            }
            catch (Exception e)
            {
                Log.Information($"项目解析错误 {qrCode} {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// 插入项目，返回ID
        /// </summary>
        /// <param name="project"></param>
        /// <returns></returns>
        public int InsertProject(Project project)
        {
            return (int)db.Db.CopyNew().Insertable(project).ExecuteReturnBigIdentity();
        }

        /// <summary>
        /// 获取所有项目
        /// </summary>
        /// <returns></returns>
        public List<Project> GetAllProject()
        {
            return db.Db.CopyNew().Queryable<Project>().OrderByDescending(t => t.Id).ToList();
        }

        /// <summary>
        /// 获取项目 根据ID
        /// </summary>
        /// <returns></returns>
        public Project GetProjectForID(int id)
        {
            return db.Db.CopyNew().Queryable<Project>().Single(t => t.Id == id);
        }
        /// <summary>
        /// 获取所有项目，异步返回
        /// </summary>
        /// <param name="isDefault"></param>
        /// <returns></returns>
        public Task<List<Project>> GetAllProjectAsync(bool isDefault)
        {
            return db.Db.CopyNew().Queryable<Project>()
                .OrderByDescending(t => t.Id)
                .Where(t => t.IsDefault == (isDefault ? 1 : 0))
                .ToListAsync();
        }

        /// <summary>
        /// 更新项目信息，返回是否成功
        /// </summary>
        /// <param name="project"></param>
        /// <returns></returns>
        public bool UpdateProject(Project project)
        {
            return db.Db.CopyNew().Updateable(project).ExecuteCommand() > 0;
        }

        /// <summary>
        /// 获取项目，根据项目代号，是否默认
        /// </summary>
        /// <param name="projectCode"></param>
        /// <param name="isDefault"></param>
        /// <returns></returns>
        public Project GetProjectForProjectCode(string projectCode, bool isDefault)
        {
            return db.Db.CopyNew().Queryable<Project>()
                .Where(p => p.ProjectCode.ToUpper() == projectCode.ToUpper())
                .Where(p => p.IsDefault == (isDefault ? 1 : 0))
                .First();
        }

        /// <summary>
        /// 获取项目，根据是否默认
        /// </summary>
        /// <param name="isDefault"></param>
        /// <returns></returns>
        public List<Project> GetProjectForType(bool isDefault)
        {
            return db.Db.CopyNew().Queryable<Project>()
                .Where(p => p.IsDefault == (isDefault ? 1 : 0))
                .ToList();
        }
        #endregion


        #region AboutTestResult
        /// <summary>
        /// 获取所有检测结果数量，异步返回
        /// </summary>
        /// <returns></returns>
        public Task<int> GetAllTestResultCountAsync(ConditionModel condition)
        {
            return getTestResultCondition(condition)
                                .CountAsync();
        }
        /// <summary>
        /// 插入检测结果，不带项目，不带图像
        /// </summary>
        /// <param name="testResult"></param>
        /// <returns></returns>
        public int InsertTestResult(TestResult testResult)
        {
            return (int)db.Db.CopyNew().Insertable(testResult).ExecuteReturnBigIdentity();
        }

        /// <summary>
        /// 更新检测结果
        /// </summary>
        /// <param name="testResult"></param>
        /// <returns></returns>
        public bool UpdateTestResult(TestResult testResult)
        {
            return db.Db.Updateable(testResult).ExecuteCommand() > 0;
        }

        /// <summary>
        /// 获取检测结果，根据ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public TestResult GetTestResultForID(int id)
        {
            return db
                .Db.CopyNew().Queryable<TestResult>()
                .Includes(it => it.Patient)
                .Includes(it => it.Project)
                .Single(t => t.Id == id);

        }

        /// <summary>
        /// 获取检测结果和图像，根据ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public TestResult GetTestResultAndPoint(int id)
        {
            return db
                    .Db.CopyNew().Queryable<TestResult>()
                .Includes(it => it.Project)
                .Includes(it => it.Patient)
                .Includes(it => it.Point)
                .Single(t => t.Id == id);
        }

        /// <summary>
        /// 获取已经检测完的所有检测结果，异步返回
        /// </summary>
        /// <returns></returns>
        public Task<List<TestResult>> GetAllTestResultAsync(ConditionModel condition)
        {
            return getTestResultCondition(condition)
               .ToListAsync();
        }

        /// <summary>
        /// 获取已经检测完的所有检测结果，异步返回
        /// </summary>
        /// <returns></returns>
        public Task<List<TestResult>> GetAllTestResultAsync(ConditionModel condition, int page, int pageSize)
        {
            return getTestResultCondition(condition)
                .ToPageListAsync(page, pageSize);

        }
        public ISugarQueryable<TestResult> getTestResultCondition(ConditionModel condition) {
            return db.Db.CopyNew().QueryableWithAttr<TestResult>()
                 .Includes(it => it.Project)
                 .Includes(it => it.Patient)
                 .OrderByDescending(t => t.Id)
                     .WhereIF(condition != null && !string.IsNullOrEmpty(condition.ProjectName) && !"全部".Contains(condition.ProjectName), it => it.Project.ProjectName.Contains(condition.ProjectName))
                     .WhereIF(condition != null && !string.IsNullOrEmpty(condition.TestVerdict) && !"全部".Contains(condition.TestVerdict), it => it.TestVerdict.Contains(condition.TestVerdict))
                     .WhereIF(condition != null && !string.IsNullOrEmpty(condition.PatientName), it => it.Patient != null && condition.TestVerdict.Contains(it.Patient.PatientName))
                     .WhereIF(condition != null && condition.ConcentrationMin != 0, it => GetConcentration(it.Con) >= condition.ConcentrationMin)
                     .WhereIF(condition != null && condition.ConcentrationMax != 0, it => GetConcentration(it.Con) <= condition.ConcentrationMax)
                     .WhereIF(condition != null && condition.ConcentrationMin2 != 0, it => GetConcentration(it.Con2) >= condition.ConcentrationMin2)
                     .WhereIF(condition != null && condition.ConcentrationMax2 != 0, it => GetConcentration(it.Con2) <= condition.ConcentrationMax2)
                     .WhereIF(condition != null && condition.TestTimeMin != null, it => it.TestTime >= condition.TestTimeMin)
                     .WhereIF(condition != null && condition.TestTimeMax != null, it => it.TestTime <= condition.TestTimeMax);
        }
        public double GetConcentration(string concentration)
        {
            if (string.IsNullOrEmpty(concentration))
            {
                return 0;
            }
            int result = 0;
            if (int.TryParse(concentration, out result))
            {
                return result;
            }
            return 0;
        }
        /// <summary>
        /// 删除检测结果，异步返回
        /// </summary>
        /// <returns></returns>
        public int DeleteTestResult(List<TestResult> testResults)
        {
            return db.Db.CopyNew().Deleteable(testResults).ExecuteCommand();
        }
        #endregion

        #region AboutApplyTest
        /// <summary>
        /// 获取全部申请检测列表
        /// </summary>
        /// <param name="applyTest"></param>
        /// <returns></returns>
        public Task<List<ApplyTest>> GetAllApplyTest(ApplyTestType applyTest)
        {
            if (applyTest == null || applyTest == ApplyTestType.All)
            {
                return db.Db.CopyNew().Queryable<ApplyTest>().Includes(it => it.Patient).ToListAsync();
            }
            else
            {
                return db
                    .Db.CopyNew().Queryable<ApplyTest>()
                    .Includes(it => it.Patient)
                    .Where(it => it.ApplyTestType == applyTest)
                    .ToListAsync();
            }
        }

        public int InsertApplyTest(ApplyTest applyTest)
        {
            return (int)(db.Db.CopyNew().Insertable(applyTest).ExecuteReturnBigIdentity());
        }

        public bool UpdateApplyTest(ApplyTest applyTest)
        {
            return db.Db.CopyNew().Updateable(applyTest).ExecuteCommand() > 0;
        }
        public ApplyTest GetApplyTest(string barcode, string testNum)
        {
            return db.Db.CopyNew().Queryable<ApplyTest>()
                .Includes(it => it.Patient)
                .Where(it => it.ApplyTestType == ApplyTestType.WaitTest)
                .First();
        }

        public ApplyTest GetApplyTestForBarcode(string barcode)
        {
            return db
                .Db.CopyNew().Queryable<ApplyTest>()
                .Includes(it => it.Patient)
                .Where(it => it.ApplyTestType == ApplyTestType.WaitTest)
                .Where(it => it.Barcode == barcode)
                .First();
        }

        public ApplyTest GetApplyTestForTestNum(string testNum)
        {
            return db
                .Db.CopyNew().Queryable<ApplyTest>()
                .Includes(it => it.Patient)
                .Where(it => it.ApplyTestType == ApplyTestType.WaitTest)
                .Where(it => it.TestNum == testNum)
                .First();
        }
        public ApplyTest GetApplyTestForID(int id)
        {
            return db
                .Db.CopyNew().Queryable<ApplyTest>()
                .Includes(it => it.Patient)
                .Single(t => t.Id == id);
        }

        internal bool DeleteApplyTest(ApplyTest applyTest)
        {
            return db.Db.CopyNew().Deleteable(applyTest).ExecuteCommand() > 0;
        }
        #endregion

        #region AboutPatient
        public int InsertPatient(Patient patient)
        {
            return (int)db.Db.CopyNew().Insertable(patient).ExecuteReturnBigIdentity();
        }

        public bool UpdatePatient(Patient patient)
        {
            return db.Db.CopyNew().Updateable(patient).ExecuteCommand() > 0;
        }

        public Patient GetPatientForID(int id)
        {
            return db.Db.CopyNew().Queryable<Patient>().Single(it => it.Id == id);
        }



        internal bool DeletePatient(Patient patient)
        {
            return db.Db.CopyNew().Deleteable(patient).ExecuteCommand() > 0;
        }
        #endregion

    }
}
