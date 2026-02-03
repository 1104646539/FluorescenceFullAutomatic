using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluorescenceFullAutomatic.Core.Config;

namespace FluorescenceFullAutomatic.Platform.Services
{
    public interface ISystemGlobalService
    {
        void SetMachineStatus(MachineStatus state);

        void SetTestType(TestType testType);

        MachineStatus GetMachineStatus();

        TestType GetTestType();

        bool IsCodeDebug();

    }

    public class SystemGlobalService : ISystemGlobalService
    {

        public void SetMachineStatus(MachineStatus state)
        {
            SystemGlobal.MachineStatus = state;
        }

        public void SetTestType(TestType testType)
        {
            SystemGlobal.TestType = testType;
        }

        public MachineStatus GetMachineStatus()
        {
            return SystemGlobal.MachineStatus;
        }

        public TestType GetTestType()
        {
            return SystemGlobal.TestType;
        }

        public bool IsCodeDebug()
        {
            return SystemGlobal.IsCodeDebug;
        }


    }
}
