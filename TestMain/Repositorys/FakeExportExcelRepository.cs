using FluorescenceFullAutomatic.Model;
using FluorescenceFullAutomatic.Repositorys;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestMain.Repositorys
{
    public class FakeExportExcelRepository : IExportExcelService
    {

        public Task<bool> ExportTestResultsToExcelAsync(List<TestResult> testResults, string filePath)
        {
            return Task.FromResult(true);
        }
    }
}
