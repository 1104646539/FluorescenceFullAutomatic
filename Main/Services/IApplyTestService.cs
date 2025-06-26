using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluorescenceFullAutomatic.Model;
using FluorescenceFullAutomatic.Sql;

namespace FluorescenceFullAutomatic.Services
{
    public interface IApplyTestService 
    {
        Task<List<ApplyTest>> GetApplyTests(ApplyTestType applyTestType);

        int InsertApplyTest(ApplyTest applyTest);

        int InsertPatient(Patient patient);

        bool UpdatePatient(Patient patient);

        bool UpdateApplyTest(ApplyTest applyTest);

        bool DeleteApplyTest(ApplyTest applyTest);

        bool DeletePatient(Patient patient);

        ApplyTest GetApplyTestForID(int id);

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

        public bool DeletePatient(Patient patient)
        {
            return SqlHelper.getInstance().DeletePatient(patient);
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

        public int InsertPatient(Patient patient)
        {
            return SqlHelper.getInstance().InsertPatient(patient);
        }

        public bool UpdateApplyTest(ApplyTest applyTest)
        {
            return SqlHelper.getInstance().UpdateApplyTest(applyTest);
        }

        public bool UpdatePatient(Patient patient)
        {
            return SqlHelper.getInstance().UpdatePatient(patient);
        }
    }
}
