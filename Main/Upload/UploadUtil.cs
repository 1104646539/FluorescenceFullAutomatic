using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluorescenceFullAutomatic.Ex;
using FluorescenceFullAutomatic.Model;
using NHapi.Base.Model;
using NHapi.Base.Parser;
using NHapi.Model.V231.Message;
using NHapi.Model.V231.Segment;
using NHapiTools.Model.V231.Message;
using Serilog;

namespace FluorescenceFullAutomatic.Upload
{
    /// <summary>
    /// HL7上传工具类
    /// </summary>
    public class UploadUtil
    {
        /// <summary>
        /// 将DSR_Q03消息转换为ApplyTest对象
        /// </summary>
        /// <param name="dsrMessage">DSR_Q03消息</param>
        /// <returns>ApplyTest对象</returns>
        public static ApplyTest ConvertDsrToApplyTest(DSR_Q03 dsrMessage)
        {
            try
            {
                if (dsrMessage == null)
                {
                    Log.Warning("无法转换为ApplyTest：DSR_Q03消息为空");
                    return null;
                }
                string patientName = GetFromDSP(dsrMessage,0,"");
                string patientGender = GetFromDSP(dsrMessage,1,"");
                string patientAge = GetFromDSP(dsrMessage,2,"");
                string barcode = GetFromDSP(dsrMessage,3,"");
                string testNum = GetFromDSP(dsrMessage,4,"");
                string inspectDate = GetFromDSP(dsrMessage,5,"");
                string projectCode = GetFromDSP(dsrMessage,6,"");
                string inspectDoctor = GetFromDSP(dsrMessage,7,"");
                string inspectDepartment = GetFromDSP(dsrMessage,8,"");
                string testDoctor = GetFromDSP(dsrMessage,9,"");
                string checkDoctor = GetFromDSP(dsrMessage,10,"");
                
                patientGender = GetGenderFormGenderCode(patientGender);

                // 创建ApplyTest对象
                ApplyTest applyTest = new ApplyTest();
                Patient patient = new Patient();
                patient.PatientName = patientName;
                patient.PatientGender = patientGender;
                patient.PatientAge = patientAge;
                patient.InspectDate = inspectDate == "" ? DateTime.Now : DateTime.ParseExact(inspectDate, DateTimeEx.DateTimeFormat3, null);
                patient.InspectDoctor = inspectDoctor;
                patient.InspectDepartment = inspectDepartment;
                patient.TestDoctor = testDoctor;
                patient.CheckDoctor = checkDoctor;

                applyTest.Barcode = barcode;
                applyTest.TestNum = testNum;
                applyTest.ProjectCode = projectCode;
                applyTest.Patient = patient;
                
                return applyTest;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "转换DSR_Q03消息为ApplyTest时出错");
                return null;
            }
        }
        /// <summary>
        /// 将性别转换为性别代码
        /// </summary>
        /// <param name="patientGender"></param>
        /// <returns></returns>
        public static string GetGenderCodeFormGender(string patientGender)
        {
            if(patientGender == null || patientGender.ToUpper() == "未知".ToUpper()){
                return "O";
            }else if(patientGender.ToUpper() == "男".ToUpper()){
                return "M";
            }else if(patientGender.ToUpper() == "女".ToUpper()){
                return "F";
            }
            return "O";
        }

        /// <summary>
        /// 将性别代码转换为性别
        /// </summary>
        /// <param name="patientGender"></param>
        /// <returns></returns>
        public static string GetGenderFormGenderCode(string patientGender)
        {
            if(patientGender == null || patientGender.ToUpper() == "O".ToUpper()){
                return "未知";
            }else if(patientGender.ToUpper() == "M".ToUpper()){
                return "男";
            }else if(patientGender.ToUpper() == "F".ToUpper()){
                return "女";
            }
            return "未知"; 
        }

        public static string GetFromDSP(DSR_Q03 dsrMessage,int index,string defaultValue)
        {
            if(dsrMessage == null || dsrMessage.GetAllDSPRecords().Count <= index)
            {
                return defaultValue;
            }
            var dsp = dsrMessage.GetAllDSPRecords()[index];
            return dsp.DataLine.Value;
        }
    }
}
