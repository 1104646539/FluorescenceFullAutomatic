using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluorescenceFullAutomatic.Ex;
using FluorescenceFullAutomatic.Model;
using Serilog;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System.IO;
using FluorescenceFullAutomatic.Utils;
using Spire.Xls;

namespace FluorescenceFullAutomatic.Services
{
    public interface IExcelExportService
    {
        /// <summary>
        /// 导出测试结果到Excel
        /// </summary>
        /// <param name="testResults">要导出的测试结果列表</param>
        /// <param name="filePath">保存路径</param>
        /// <returns>是否导出成功</returns>
        Task<bool> ExportTestResultsToExcelAsync(List<TestResult> testResults, string filePath);
    }

    public class ExcelExportService : IExcelExportService
    {
        public async Task<bool> ExportTestResultsToExcelAsync(List<TestResult> testResults, string filePath)
        {
            try
            {
                // 创建工作簿
                IWorkbook workbook = new XSSFWorkbook();
                ISheet sheet = workbook.CreateSheet("检测结果");

                // 设置表头
                string[] headers = new string[] 
                { 
                    "ID", "姓名", "年龄", "性别", "条码", "样本编号", "项目名称", "批次号",
                    "结果", "检测时间", "结果状态", "T值", "C值", "TC值", "浓度值",
                    "T2值", "C2值", "TC2值", "浓度2值"
                };
                int[] minWidths = new int[]{
                    5, 10, 8, 8, 15, 15, 15, 15,
                    10, 20, 10, 10, 10, 10, 10,
                    10, 10, 10, 10
                };
                // 创建表头行
                IRow headerRow = sheet.CreateRow(0);
                for (int i = 0; i < headers.Length; i++)
                {
                    ICell cell = headerRow.CreateCell(i);
                    cell.SetCellValue(headers[i]);
                    
                    // 设置表头样式
                    ICellStyle headerStyle = workbook.CreateCellStyle();
                    IFont headerFont = workbook.CreateFont();
                    headerFont.IsBold = true;
                    headerStyle.SetFont(headerFont);
                    cell.CellStyle = headerStyle;
                }

                // 填充数据
                for (int i = 0; i < testResults.Count; i++)
                {
                    var result = testResults[i];
                    IRow dataRow = sheet.CreateRow(i + 1);
                    
                    dataRow.CreateCell(0).SetCellValue(GlobalUtil.ToStringOrNull(result.Id.ToString()));
                    dataRow.CreateCell(1).SetCellValue(GlobalUtil.ToStringOrNull(result.Patient?.PatientName));
                    dataRow.CreateCell(2).SetCellValue(GlobalUtil.ToStringOrNull(result.Patient?.PatientGender));
                    dataRow.CreateCell(3).SetCellValue(GlobalUtil.ToStringOrNull(result.Patient?.PatientAge));
                    dataRow.CreateCell(4).SetCellValue(GlobalUtil.ToStringOrNull(result.Barcode));
                    dataRow.CreateCell(5).SetCellValue(GlobalUtil.ToStringOrNull(result.TestNum));
                    dataRow.CreateCell(6).SetCellValue(GlobalUtil.ToStringOrNull(result.Project?.ProjectName));
                    dataRow.CreateCell(7).SetCellValue(GlobalUtil.ToStringOrNull(result.FrameNum));
                    dataRow.CreateCell(8).SetCellValue(GlobalUtil.ToStringOrNull(result.TestVerdict));
                    dataRow.CreateCell(9).SetCellValue(GlobalUtil.ToStringOrNull(result.TestTime.GetDateTimeString()));
                    dataRow.CreateCell(10).SetCellValue(GlobalUtil.ToStringOrNull(result.ResultState.GetDescription()));
                    dataRow.CreateCell(11).SetCellValue(GlobalUtil.ToStringOrNull(result.T));
                    dataRow.CreateCell(12).SetCellValue(GlobalUtil.ToStringOrNull(result.C));
                    dataRow.CreateCell(13).SetCellValue(GlobalUtil.ToStringOrNull(result.Tc));
                    dataRow.CreateCell(14).SetCellValue(GlobalUtil.ToStringOrNull(result.Con));
                    dataRow.CreateCell(15).SetCellValue(GlobalUtil.ToStringOrNull(result.T2));
                    dataRow.CreateCell(16).SetCellValue(GlobalUtil.ToStringOrNull(result.C2));
                    dataRow.CreateCell(17).SetCellValue(GlobalUtil.ToStringOrNull(result.Tc2));
                    dataRow.CreateCell(18).SetCellValue(GlobalUtil.ToStringOrNull(result.Con2));
                }
                
                // 自动调整列宽
                for (int i = 0; i < headers.Length; i++)
                {
                    ICellStyle cellStyle = workbook.CreateCellStyle();
                    sheet.SetDefaultColumnStyle(i, cellStyle);
                    
                    // 设置最小列宽（以字符宽度为单位）
                    int minWidth = minWidths[i]; 
                    sheet.SetColumnWidth(i, minWidth * 256); // NPOI中1个字符宽度等于256个单位
                    
                    // 自动调整列宽，但确保不小于最小宽度
                    sheet.AutoSizeColumn(i);
                    int currentWidth = (int)sheet.GetColumnWidth(i);
                    if (currentWidth < minWidth * 256)
                    {
                        sheet.SetColumnWidth(i, minWidth * 256);
                    }
                }

                // 保存文件
                await Task.Run(() =>
                {
                    using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                    {
                        workbook.Write(fs);
                    }
                });

                return true;
            }
            catch (Exception ex)
            {
                Log.Information(ex, "导出Excel失败");
                return false;
            }
        }
    }
} 