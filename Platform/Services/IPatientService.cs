using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluorescenceFullAutomatic.Platform.Model;
using FluorescenceFullAutomatic.Platform.Sql;

namespace FluorescenceFullAutomatic.Platform.Services
{
    public interface IPatientService
    {
        int InsertPatient(Patient patient);
        bool UpdatePatient(Patient patient);
        bool DeletePatient(Patient patient);
        Patient GetPatientForID(int id);
    }

    public class PatientService : IPatientService
    {
        public bool DeletePatient(Patient patient)
        {
            return SqlHelper.getInstance().DeletePatient(patient);
        }

        public Patient GetPatientForID(int id)
        {
            return SqlHelper.getInstance().GetPatientForID(id);
        }

        public int InsertPatient(Patient patient)
        {
            return SqlHelper.getInstance().InsertPatient(patient);
        }

        public bool UpdatePatient(Patient patient)
        {
            return SqlHelper.getInstance().UpdatePatient(patient);
        }
    }
}
