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
        /// �������Խ����Excel
        /// </summary>
        /// <param name="testResults">Ҫ�����Ĳ��Խ���б�</param>
        /// <param name="filePath">����·��</param>
        /// <returns>�Ƿ񵼳��ɹ�</returns>
        Task<bool> ExportTestResultsToExcelAsync(List<TestResult> testResults, string filePath);
    }

    public class ExcelExportService : IExcelExportService
    {
        public async Task<bool> ExportTestResultsToExcelAsync(List<TestResult> testResults, string filePath)
        {
            try
            {
                // ����������
                IWorkbook workbook = new XSSFWorkbook();
                ISheet sheet = workbook.CreateSheet("�����");

                // ���ñ�ͷ
                string[] headers = new string[] 
                { 
                    "ID", "����", "����", "�Ա�", "����", "�������", "��Ŀ����", "���κ�",
                    "���", "���ʱ��", "���״̬", "Tֵ", "Cֵ", "TCֵ", "Ũ��ֵ",
                    "T2ֵ", "C2ֵ", "TC2ֵ", "Ũ��2ֵ"
                };
                int[] minWidths = new int[]{
                    5, 10, 8, 8, 15, 15, 15, 15,
                    10, 20, 10, 10, 10, 10, 10,
                    10, 10, 10, 10
                };
                // ������ͷ��
                IRow headerRow = sheet.CreateRow(0);
                for (int i = 0; i < headers.Length; i++)
                {
                    ICell cell = headerRow.CreateCell(i);
                    cell.SetCellValue(headers[i]);
                    
                    // ���ñ�ͷ��ʽ
                    ICellStyle headerStyle = workbook.CreateCellStyle();
                    IFont headerFont = workbook.CreateFont();
                    headerFont.IsBold = true;
                    headerStyle.SetFont(headerFont);
                    cell.CellStyle = headerStyle;
                }

                // �������
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
                
                // �Զ������п�
                for (int i = 0; i < headers.Length; i++)
                {
                    ICellStyle cellStyle = workbook.CreateCellStyle();
                    sheet.SetDefaultColumnStyle(i, cellStyle);
                    
                    // ������С�п����ַ����Ϊ��λ��
                    int minWidth = minWidths[i]; 
                    sheet.SetColumnWidth(i, minWidth * 256); // NPOI��1���ַ���ȵ���256����λ
                    
                    // �Զ������п���ȷ����С����С���
                    sheet.AutoSizeColumn(i);
                    int currentWidth = (int)sheet.GetColumnWidth(i);
                    if (currentWidth < minWidth * 256)
                    {
                        sheet.SetColumnWidth(i, minWidth * 256);
                    }
                }

                // �����ļ�
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
                Log.Information(ex, "����Excelʧ��");
                return false;
            }
        }
    }
} 