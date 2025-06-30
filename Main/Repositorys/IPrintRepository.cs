using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluorescenceFullAutomatic.Model;
using FluorescenceFullAutomatic.Utils;

namespace FluorescenceFullAutomatic.Repositorys
{
    public interface IPrintRepository
    {
        public void PrintReport(List<TestResult> trs, string printerName, Action<string> successAction = null, Action<string> failedAction = null);

        public void PrintTicket(List<TestResult> trs, Action<string> successAction, Action<string> failedAction);

        public void AutoPrintReport(TestResult tr, bool autoPrint, bool autoUploadFtp, bool autoPrintTicket, string printerName);
        public void PrintTicket(TestResult tr, Action<string> successAction, Action<string> failedAction);

    }
    public class PrintRepository : IPrintRepository
    {
        public void AutoPrintReport(TestResult tr, bool autoPrint, bool autoUploadFtp, bool autoPrintTicket, string printerName)
        {
            A4ReportUtil.AutoExecReport(tr, autoPrint, autoUploadFtp, printerName);
            //打印小票
            if (autoPrintTicket)
            {
                PrintTicket(tr, (msg) => { }, (err) => { });
            }
        }


        public void PrintReport(List<TestResult> trs, string printerName, Action<string> successAction = null, Action<string> failedAction = null)
        {
            A4ReportUtil.PrintReport(trs, printerName, successAction, failedAction);
        }

        public void PrintTicket(List<TestResult> trs, Action<string> successAction, Action<string> failedAction)
        {
            TicketReportUtil.Instance.PrintTicket(trs, successAction, failedAction);
        }

        public void PrintTicket(TestResult tr, Action<string> successAction, Action<string> failedAction)
        {
            TicketReportUtil.Instance.PrintTicket(tr, successAction, failedAction);
        }
    }
}
