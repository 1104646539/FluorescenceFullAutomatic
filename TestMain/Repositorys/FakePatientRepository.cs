using FluorescenceFullAutomatic.Model;
using FluorescenceFullAutomatic.Repositorys;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestMain.Repositorys
{
    public class FakePatientRepository : IPatientRepository
    {
        Dictionary<int, Patient> datas = new Dictionary<int, Patient>();
        int id = 0;
        public FakePatientRepository() {
            for (int i = 0; i < 5; i++)
            {
                Patient p = new Patient
                {
                    PatientName = "张三",
                    PatientGender = "男",
                    PatientAge = "35",
                    InspectDate = DateTime.Now.AddDays(1),
                    InspectDepartment = "内科",
                    InspectDoctor = "李医生",
                    TestDoctor = "王医生",
                    CheckDoctor = "赵医生"
                };
                id = i + 1;
                p.Id = id;
                datas.Add(p.Id,p);
            }
        }
        public bool DeletePatient(Patient patient)
        {
           return datas.Remove(patient.Id);
        }

        public Patient GetPatientForID(int id)
        {
            if (datas.TryGetValue(id, out Patient patient)) {
                return patient;
            }
            return null;
        }

        public int InsertPatient(Patient patient)
        {
            patient.Id = ++id;
            datas.Add(id,patient);
            return 1;
        }

        public bool UpdatePatient(Patient patient)
        {
            if (datas.TryGetValue(id, out Patient p))
            {
                datas[id] = patient;
                return true ;
            }
            return false;
        }
    }
}
