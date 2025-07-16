
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluorescenceFullAutomatic.Core.Config
{
    public class SystemGlobal
    {
        /// <summary>
        /// ������ǰ����״̬
        /// </summary>
        public static MachineStatus MachineStatus = MachineStatus.None;
        /// <summary>
        /// �Ƿ����ڽ�����������
        /// </summary>
        public static bool IsRunningtStop = false;

        /// <summary>
        /// ��ǰ�¶��Ƿ�ϸ�
        /// </summary>
        public static bool TempStandard = false;

        /// <summary>
        /// ��ǰ������� ���� ���ʿأ�����
        /// </summary>
        public static TestType TestType = TestType.None;

        /// <summary>
        /// ��������Ƿ�Ҫ�������
        /// </summary>
        public static bool ErrorContinueTest = false;
        /// <summary>
        /// �Ƿ��ǵ��Դ���ģʽ����ʹ����ʵ����
        /// </summary>
        public const bool IsCodeDebug = true;
     
        /// <summary>
        /// �����ļ����̷�����
        /// </summary>
        public const string UpdateFlashName = "STM32";
        /// <summary>
        /// �����ļ���
        /// </summary>
        public const string UpdateFileName = "appup.bin";
        /// <summary>
        /// ��λ���汾
        /// </summary>
        public static string McuVersion = "";
        /// <summary>
        /// Ĭ�ϵ�ģ���ļ�·��
        /// </summary>
        public static string Template_Path = GetCurrentProjectPath + @"/template/A4.xlsx";
        /// <summary>
        /// Ĭ�ϵ�˫����ģ���ļ�·��
        /// </summary>
        public static string DoubleTemplate_Path = GetCurrentProjectPath + @"/template/A4-˫����.xlsx";
        /// <summary>
        /// Ĭ�ϵĻ���·��
        /// </summary>
        public static string Cache_Path = GetCurrentProjectPath + @"/cache";

        public static string GetCurrentProjectPath
        {

            get
            {
                //return Environment.CurrentDirectory.Replace(@"\bin\Debug\net6.0-windows", "");//��ȡ����·��
                return Environment.CurrentDirectory;//��ȡ����·��
            }
        }
    }
   
}
