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
    /// HL7��Ϣ������
    /// ���ڴ�������HL7 2.3.1�汾����Ϣ
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
        /// ���캯��
        /// </summary>
        /// <param name="sendingApplication">����Ӧ�ñ�ʶ</param>
        /// <param name="sendingFacility">���ͻ�����ʶ</param>
        /// <param name="receivingApplication">����Ӧ�ñ�ʶ</param>
        /// <param name="receivingFacility">���ջ�����ʶ</param>
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
        /// ����MSH�Σ���Ϣͷ��
        /// </summary>
        /// <param name="message">HL7��Ϣ����</param>
        /// <param name="messageType">��Ϣ����</param>
        /// <param name="triggerEvent">�����¼�</param>
        /// <param name="messageControlId">��Ϣ����ID</param>
        private void CreateMSH(IMessage message, string messageType, string triggerEvent, string messageControlId = null)
        {
            try
            {
                var msh = (MSH)message.GetStructure("MSH");

                // �����ֶ�
                msh.FieldSeparator.Value = "|";
                msh.EncodingCharacters.Value = "^~\\&";
                msh.SendingApplication.NamespaceID.Value = _sendingApplication;
                msh.SendingFacility.NamespaceID.Value = _sendingFacility;
                msh.ReceivingApplication.NamespaceID.Value = _receivingApplication;
                msh.ReceivingFacility.NamespaceID.Value = _receivingFacility;
                msh.DateTimeOfMessage.TimeOfAnEvent.Value = DateTime.Now.GetDateTimeString3();

                // ��Ϣ���ͺʹ����¼�
                msh.MessageType.MessageType.Value = messageType;
                msh.MessageType.TriggerEvent.Value = triggerEvent;

                // ��Ϣ����ID - ���δ�ṩ����ʹ��ʱ�����ΪΨһ��ʶ
                msh.MessageControlID.Value = messageControlId;

                // ����ID - P��ʾ��������
                msh.ProcessingID.ProcessingID.Value = "P";

                // �汾ID - 2.3.1
                msh.VersionID.VersionID.Value = "2.3.1";

                // ����ȷ������ - AL�����ǣ�
                msh.AcceptAcknowledgmentType.Value = "AL";

                // Ӧ��ȷ������ - AL�����ǣ�
                msh.ApplicationAcknowledgmentType.Value = "AL";

                // ���Ҵ��� - CHN���й���
                msh.CountryCode.Value = "";

                // �ַ��� - �������û�ȡ
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
                Log.Error(ex, "����MSH��ʱ��������");
                throw;
            }
        }


        /// <summary>
        /// ����QRY_Q02��ѯ��Ϣ��֧�ֲ�ѯ���ͺ͸��Ӳ���
        /// </summary>
        /// <param name="sampleId">����ID</param>
        /// <param name="queryType">��ѯ����</param>
        /// <param name="additionalParam">���Ӳ���</param>
        /// <returns>QRY_Q02��Ϣ</returns>
        public QRY_Q02 CreateQRY_Q02(QueryType queryType, string condition1, string condition2 = "")
        {
            var qry = new QRY_Q02();
            try
            {// �ȴ���������QRY_Q02��Ϣ
                CreateMSH(qry, "QRY", "Q02", messageControlId: "" + (mshId++));
                // ��Ӳ�ѯ����
                qry.QRD.QueryPriority.Value = "D";
                qry.QRD.QueryDateTime.TimeOfAnEvent.Value = DateTime.Now.GetDateTimeString3();
                qry.QRD.QueryFormatCode.Value = queryType.ToString().ToUpper();
                qry.QRD.GetWhoSubjectFilter(0).IDNumber.Value = condition1;
                qry.QRD.GetWhatSubjectFilter(0).Identifier.Value = condition2;
                qry.QRD.QueryResultsLevel.Value = "1";

            }
            catch (Exception ex)
            {
                Log.Warning(ex, "���ò�ѯ����ʱ��������");
            }

            return qry;

        }

        /// <summary>
        /// ����ORU_R01����ϴ���Ϣ
        /// </summary>
        /// <param name="testResult">���Խ��</param>
        /// <returns>ORU_R01��Ϣ</returns>
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
            //// ����MSH��
            CreateMSH(oru, "ORU", "R01", messageControlId: "" + (mshId++));

            //// ����PID�Σ����߱�ʶ��
            ORU_R01_PATIENT_RESULT pr = oru.AddPATIENT_RESULT();
            PID pid = pr.PATIENT.PID;
            pid.GetPatientName(0).FamilyLastName.FamilyName.Value = patientName;
            pid.DateTimeOfBirth.TimeOfAnEvent.Value = patientAge;
            pid.Sex.Value = patientGender;
            //OBR ������Ϣ
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
            
            //OBX �����Ϣ
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
        /// ����HL7��Ϣ�ַ���
        /// </summary>
        /// <param name="messageText">HL7��Ϣ�ı�</param>
        /// <returns>�������HL7��Ϣ����</returns>
        public IMessage ParseMessage(string messageText)
        {
            try
            {
                return _parser.Parse(messageText);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "����HL7��Ϣʱ��������");
                throw;
            }
        }

        /// <summary>
        /// ��HL7��Ϣ����ת��Ϊ�ַ���
        /// </summary>
        /// <param name="message">HL7��Ϣ����</param>
        /// <returns>HL7��Ϣ�ַ���</returns>
        public string EncodeMessage(IMessage message)
        {
            try
            {
                // ʹ�ñ�׼Parser������Ϣ
                return _parser.Encode(message);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "����HL7��Ϣʱ��������");
                throw;
            }
        }

        /// <summary>
        /// ����ACKȷ����Ϣ
        /// </summary>
        /// <param name="originalMessage">ԭʼ��Ϣ</param>
        /// <param name="ackCode">ȷ�ϴ��룺AA(����)��AE(����)��AR(�ܾ�)</param>
        /// <param name="textMessage">�ı���Ϣ</param>
        /// <returns>ACK��Ϣ</returns>
        public ACK CreateACK(IMessage originalMessage, string ackCode, string textMessage)
        {
            try
            {
                var originalMsh = (MSH)originalMessage.GetStructure("MSH");
                var ack = new ACK();
                var id = originalMsh.MessageControlID.Value;
                // ����MSH��
                CreateMSH(ack, "ACK", "Q03", messageControlId:""+ mshId++);
                ack.MSA.AcknowledgementCode.Value = "AA";
                ack.MSA.MessageControlID.Value = id;
                ack.MSA.TextMessage.Value = "";
                ack.MSA.ErrorCondition.Identifier.Value = "0";
                
                return ack;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "����ACK��Ϣʱ��������");
                throw;
            }
        }
    }


}