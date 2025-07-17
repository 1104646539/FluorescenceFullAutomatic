using FluorescenceFullAutomatic.Core.Model;
using FluorescenceFullAutomatic.Platform.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestMain.Services
{
    public class FakeDispatcherService : IDispatcherService
    {
        public void Invoke(Action action)
        {
            action?.Invoke();
        }

        public void InvokeAsync(Action action)
        {
            action?.Invoke();
        }
    }
}
