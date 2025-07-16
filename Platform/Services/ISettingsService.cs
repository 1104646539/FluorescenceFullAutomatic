using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluorescenceFullAutomatic.Platform.Services
{
    public interface ISettingsService
    {
        int GetTestNum();
        void SetTestNum();
    }
    public class SettingsService : ISettingsService
    {
        public int GetTestNum()
        {
            return 0;
        }

        public void SetTestNum()
        {
            
        }
    }
}
