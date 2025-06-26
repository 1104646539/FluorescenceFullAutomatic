using System;
using System.Text;
using FluorescenceFullAutomatic.Config;
using FluorescenceFullAutomatic.Ex;
using FluorescenceFullAutomatic.Model;
using NHapi.Base.Model;
using NHapi.Base.Parser;
using NHapi.Model.V231.Datatype;
using NHapi.Model.V231.Group;
using NHapi.Model.V231.Message;
using NHapi.Model.V231.Segment;
using NHapiTools.Model.V231.Segment;
using Serilog;
using static Main.Upload.Hl7Result;
using System.Linq;

namespace FluorescenceFullAutomatic.Upload
{
    /// <summary>
    /// HL7消息构建器
    /// 用于创建各种HL7 2.3.1版本的消息
    /// </summary>
    public class HL7MessageBuilder
    {
        private readonly string _sendingApplication;
        private readonly string _sendingFacility;
        private readonly string _receivingApplication;
        private readonly string _receivingFacility;
        private readonly PipeParser _parser;
        private int mshId = 1;
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="sendingApplication">发送应用标识</param>
        /// <param name="sendingFacility">发送机构标识</param>
        /// <param name="receivingApplication">接收应用标识</param>
        /// <param name="receivingFacility">接收机构标识</param>
        public HL7MessageBuilder(
            string sendingApplication = "yingguang2500",
            string sendingFacility = "yingguang2500",
            string receivingApplication = "LIS",
            string receivingFacility = "HOSPITAL")
        {
            _sendingApplication = sendingApplication;
            _sendingFacility = sendingFacility;
            _receivingApplication = receivingApplication;
            _receivingFacility = receivingFacility;
            _parser = new PipeParser();
        }

        /// <summary>
        /// 创建MSH段（消息头）
        /// </summary>
        /// <param name="message">HL7消息对象</param>
        /// <param name="messageType">消息类型</param>
        /// <param name="triggerEvent">触发事件</param>
        /// <param name="messageControlId">消息控制ID</param>
        private void CreateMSH(IMessage message, string messageType, string triggerEvent, string messageControlId = null)
        {
            try
            {
                var msh = (MSH)message.GetStructure("MSH");

                // 必填字段
                msh.FieldSeparator.Value = "|";
                msh.EncodingCharacters.Value = "^~\\&";
                msh.SendingApplication.NamespaceID.Value = _sendingApplication;
                msh.SendingFacility.NamespaceID.Value = _sendingFacility;
                msh.ReceivingApplication.NamespaceID.Value = _receivingApplication;
                msh.ReceivingFacility.NamespaceID.Value = _receivingFacility;
                msh.DateTimeOfMessage.TimeOfAnEvent.Value = DateTime.Now.GetDateTimeString3();

                // 消息类型和触发事件
                msh.MessageType.MessageType.Value = messageType;
                msh.MessageType.TriggerEvent.Value = triggerEvent;

                // 消息控制ID - 如果未提供，则使用时间戳作为唯一标识
                msh.MessageControlID.Value = messageControlId;

                // 处理ID - P表示生产环境
                msh.ProcessingID.ProcessingID.Value = "P";

                // 版本ID - 2.3.1
                msh.VersionID.VersionID.Value = "2.3.1";

                // 接收确认类型 - AL（总是）
                msh.AcceptAcknowledgmentType.Value = "AL";

                // 应用确认类型 - AL（总是）
                msh.ApplicationAcknowledgmentType.Value = "AL";

                // 国家代码 - CHN（中国）
                msh.CountryCode.Value = "";

                // 字符集 - 根据配置获取
                string charset = UploadConfig.Instance.Charset.ToUpper();
                if (charset == "UTF-8")
                {
                    msh.GetCharacterSet(0).Value = "UNICODE UTF-8";
                }
                else if (charset == "GBK")
                {
                    msh.GetCharacterSet(0).Value = "GB18030-2000";
                }
                else
                {
                    msh.GetCharacterSet(0).Value = "ASCII";
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "创建MSH段时发生错误");
                throw;
            }
        }


        /// <summary>
        /// 创建QRY_Q02查询消息，支持查询类型和附加参数
        /// </summary>
        /// <param name="sampleId">样本ID</param>
        /// <param name="queryType">查询类型</param>
        /// <param name="additionalParam">附加参数</param>
        /// <returns>QRY_Q02消息</returns>
        public QRY_Q02 CreateQRY_Q02(QueryType queryType, string condition1, string condition2 = "")
        {
            var qry = new QRY_Q02();
            try
            {// 先创建基本的QRY_Q02消息
                CreateMSH(qry, "QRY", "Q02", messageControlId: "" + (mshId++));
                // 添加查询参数
                qry.QRD.QueryPriority.Value = "D";
                qry.QRD.QueryDateTime.TimeOfAnEvent.Value = DateTime.Now.GetDateTimeString3();
                qry.QRD.QueryFormatCode.Value = queryType.ToString().ToUpper();
                qry.QRD.GetWhoSubjectFilter(0).IDNumber.Value = condition1;
                qry.QRD.GetWhatSubjectFilter(0).Identifier.Value = condition2;
                qry.QRD.QueryResultsLevel.Value = "1";

            }
            catch (Exception ex)
            {
                Log.Warning(ex, "设置查询参数时发生错误");
            }

            return qry;

        }

        /// <summary>
        /// 创建ORU_R01结果上传消息
        /// </summary>
        /// <param name="testResult">测试结果</param>
        /// <returns>ORU_R01消息</returns>
        public ORU_R01 CreateORU_R01(TestResult testResult)
        {

            var oru = new ORU_R01();

            string patientName = testResult?.Patient?.PatientName ?? "";
            string patientGender = UploadUtil.GetGenderCodeFormGender(testResult?.Patient?.PatientGender ?? "");
            string patientAge = testResult?.Patient?.PatientAge ?? "";

            string barcode = testResult?.Barcode ?? "";
            string testNum = testResult?.TestNum ?? "";
            string inspectDate = "";
            if (testResult?.Patient?.InspectDate != null)
            {
                inspectDate = testResult.Patient.InspectDate.GetDateTimeString3();
            }
            string inspectDepartment = testResult?.Patient?.InspectDepartment ?? "";
            string inspectDoctor = testResult?.Patient?.InspectDoctor ?? "";
            string testDoctor = testResult?.Patient?.TestDoctor ?? "";
            string checkDoctor = testResult?.Patient?.CheckDoctor ?? "";

            string projectCode = testResult?.Project?.ProjectCode ?? "";
            string con1 = testResult?.Con ?? "";
            string con2 = testResult?.Con2 ?? "";
            string con = "";
            string unit1 = testResult?.Project?.ProjectUnit ?? "";
            string unit2 = testResult?.Project?.ProjectUnit2 ?? "";
            string units = "";
            string range1 = "0-" + (testResult?.Project?.ProjectLjz ?? 0).ToString();
            string range2 = "0-" + (testResult?.Project?.ProjectLjz2 ?? 0).ToString();
            string range = "";
            string result = testResult?.TestVerdict??"";
            string tc1 = testResult?.Tc ?? "";
            string tc2 = testResult?.Tc2 ?? "";
            string tc = "";
            if (testResult?.Project?.ProjectType == Project.Project_Type_Double)
            {
                range = range1 + "^" + range2;
                con = con1 + "^" + con2;
                units = unit1 + "^" + unit2;
                tc = tc1 + "^" + tc2;
            }
            else
            {
                range = range1;
                con = con1;
                units = unit1;
                tc = tc1;
            }
            string testTime = (testResult?.TestTime ?? DateTime.Now).GetDateTimeString3();
            //// 创建MSH段
            CreateMSH(oru, "ORU", "R01", messageControlId: "" + (mshId++));

            //// 创建PID段（患者标识）
            ORU_R01_PATIENT_RESULT pr = oru.AddPATIENT_RESULT();
            PID pid = pr.PATIENT.PID;
            pid.GetPatientName(0).FamilyLastName.FamilyName.Value = patientName;
            pid.DateTimeOfBirth.TimeOfAnEvent.Value = patientAge;
            pid.Sex.Value = patientGender;
            //OBR 其他信息
            ORU_R01_ORDER_OBSERVATION obt = pr.AddORDER_OBSERVATION();
            OBR obr = obt.OBR;
            obr.PlacerOrderNumber.EntityIdentifier.Value = barcode;
            obr.FillerOrderNumber.EntityIdentifier.Value = testNum;
            obr.SpecimenReceivedDateTime.TimeOfAnEvent.Value = inspectDate;
            XTN orderCallbackPhoneNumber = obr.AddOrderCallbackPhoneNumber();
            orderCallbackPhoneNumber.Get9999999X99999CAnyText.Value = inspectDepartment;
            XCN xcn = obr.AddOrderingProvider();
            xcn.IDNumber.Value = inspectDoctor;
            obr.FillerField1.Value = testDoctor;
            obr.PrincipalResultInterpreter.Name.IDNumber.Value = checkDoctor;
            
            //OBX 检测信息
            ORU_R01_OBSERVATION observation = obt.AddOBSERVATION();
            OBX obx = observation.OBX;
            obx.ValueType.Value = "NM";
            obx.ObservationIdentifier.Identifier.Value = projectCode;
            obx.ObservationSubID.Value = projectCode;
            NM conNM = new NM(obx.Message);
            conNM.Value = con;
            obx.AddObservationValue().Data = conNM;
            obx.Units.Identifier.Value = units;
            obx.ReferencesRange.Value = range;
            obx.AddProbability().Value = result;
            obx.UserDefinedAccessChecks.Value = tc;
            obx.DateTimeOfTheObservation.TimeOfAnEvent.Value = testTime;

            return oru;
        }


        /// <summary>
        /// 解析HL7消息字符串
        /// </summary>
        /// <param name="messageText">HL7消息文本</param>
        /// <returns>解析后的HL7消息对象</returns>
        public IMessage ParseMessage(string messageText)
        {
            try
            {
                return _parser.Parse(messageText);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "解析HL7消息时发生错误");
                throw;
            }
        }

        /// <summary>
        /// 将HL7消息对象转换为字符串
        /// </summary>
        /// <param name="message">HL7消息对象</param>
        /// <returns>HL7消息字符串</returns>
        public string EncodeMessage(IMessage message)
        {
            try
            {
                // 使用标准Parser编码消息
                return _parser.Encode(message);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "编码HL7消息时发生错误");
                throw;
            }
        }

        /// <summary>
        /// 创建ACK确认消息
        /// </summary>
        /// <param name="originalMessage">原始消息</param>
        /// <param name="ackCode">确认代码：AA(接受)、AE(错误)、AR(拒绝)</param>
        /// <param name="textMessage">文本消息</param>
        /// <returns>ACK消息</returns>
        public ACK CreateACK(IMessage originalMessage, string ackCode, string textMessage)
        {
            try
            {
                var originalMsh = (MSH)originalMessage.GetStructure("MSH");
                var ack = new ACK();
                var id = originalMsh.MessageControlID.Value;
                // 创建MSH段
                CreateMSH(ack, "ACK", "Q03", messageControlId:""+ mshId++);
                ack.MSA.AcknowledgementCode.Value = "AA";
                ack.MSA.MessageControlID.Value = id;
                ack.MSA.TextMessage.Value = "";
                ack.MSA.ErrorCondition.Identifier.Value = "0";
                
                return ack;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "创建ACK消息时发生错误");
                throw;
            }
        }
    }


}