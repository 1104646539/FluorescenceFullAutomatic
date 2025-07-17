using FluorescenceFullAutomatic.Core.Model;
using FluorescenceFullAutomatic.Platform.Model;
using FluorescenceFullAutomatic.Platform.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestMain.Repositorys
{
    public class FakePrintRepository : IPrintService
    {
        public void AutoPrintReport(TestResult tr, bool autoPrint, bool autoUploadFtp, bool autoPrintTicket, string printerName)
        {

        }

        public void PrintReport(List<TestResult> trs, string printerName, Action<string> successAction = null, Action<string> failedAction = null)
        {

        }

        public void PrintTicket(List<TestResult> trs, Action<string> successAction, Action<string> failedAction)
        {
            successAction?.Invoke("");
        }

        public void PrintTicket(TestResult tr, Action<string> successAction, Action<string> failedAction)
        {
            successAction?.Invoke("");
        }

        public void PrintTicketQC(string time, string coefficient1, string coefficient2, string scope, string result, List<TestResult> trs, bool isDouble, Action<string> successAction, Action<string> failedAction)
        {
            
        }
    }
}
