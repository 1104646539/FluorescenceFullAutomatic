using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluorescenceFullAutomatic.Model;
using FluorescenceFullAutomatic.Sql;

namespace FluorescenceFullAutomatic.Repositorys
{
    public interface IApplyTestRepository
    {
        Task<List<ApplyTest>> GetApplyTests(ApplyTestType applyTestType);

        int InsertApplyTest(ApplyTest applyTest);

        bool UpdateApplyTest(ApplyTest applyTest);

        bool DeleteApplyTest(ApplyTest applyTest);

        ApplyTest GetApplyTestForID(int id);

        void UpdateApplyTestCompleted(ApplyTest applyTest);
    }
    public class ApplyTestRepository : IApplyTestRepository
    {
        public ApplyTestRepository()
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
    }
}
