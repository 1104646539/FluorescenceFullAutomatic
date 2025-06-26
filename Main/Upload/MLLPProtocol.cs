using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NHapi.Base.Parser;
using Serilog;

namespace FluorescenceFullAutomatic.Upload
{
    /// <summary>
    /// MLLPЭ�鴦����
    /// ������MLLPЭ�����Ϣ�ָ�ͷ�װ
    /// MLLPЭ�飺ʹ�ô�ֱ��ǩ��(VT, 0x0B)��Ϊ��ʼ�����ļ�������(FS, 0x1C)�ͻس���(CR, 0x0D)��Ϊ������
    /// </summary>
    public class MLLPProtocol
    {
        // MLLPЭ�鶨��������ַ�
        public const byte VT = 0x0B; // ��ֱ��ǩ����������ʼ��
        public const byte FS = 0x1C; // �ļ��ָ�����������ֹ��
        public const byte CR = 0x0D; // �س�����������ֹ��

        // �����ʽ
        private readonly Encoding _encoding;

        /// <summary>
        /// ���캯��
        /// </summary>
        /// <param name="encoding">�����ʽ</param>
        public MLLPProtocol(Encoding encoding)
        {
            _encoding = encoding ?? Encoding.UTF8;
        }

        /// <summary>
        /// ��HL7��Ϣ��װ��MLLP��ʽ
        /// </summary>
        /// <param name="message">ԭʼHL7��Ϣ</param>
        /// <returns>MLLP��ʽ����Ϣ�ֽ�����</returns>
        public byte[] WrapMessage(string message)
        {
            try
            {
                byte[] messageBytes = _encoding.GetBytes(message);
                byte[] wrappedMessage = new byte[messageBytes.Length + 3];

                wrappedMessage[0] = VT;
                Buffer.BlockCopy(messageBytes, 0, wrappedMessage, 1, messageBytes.Length);
                wrappedMessage[wrappedMessage.Length - 2] = FS;
                wrappedMessage[wrappedMessage.Length - 1] = CR;

                return wrappedMessage;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "��HL7��Ϣ��װ��MLLP��ʽʱ����");
                throw;
            }
        }

        /// <summary>
        /// ��MLLP��ʽ����ȡHL7��Ϣ
        /// </summary>
        /// <param name="mllpMessage">MLLP��ʽ����Ϣ�ֽ�����</param>
        /// <returns>��ȡ����HL7��Ϣ</returns>
        public string UnwrapMessage(byte[] mllpMessage)
        {
            try
            {
                if (mllpMessage == null || mllpMessage.Length < 3)
                {
                    throw new ArgumentException("MLLP��Ϣ��ʽ����ȷ�����ȹ���");
                }

                if (mllpMessage[0] != VT || mllpMessage[mllpMessage.Length - 2] != FS || mllpMessage[mllpMessage.Length - 1] != CR)
                {
                    throw new ArgumentException("MLLP��Ϣ��ʽ����ȷ��ȱ����ʼ��������");
                }

                return _encoding.GetString(mllpMessage, 1, mllpMessage.Length - 3);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "��MLLP��ʽ����ȡHL7��Ϣʱ����");
                throw;
            }
        }

        /// <summary>
        /// �����ж�ȡһ��������MLLP��Ϣ
        /// </summary>
        /// <param name="stream">������</param>
        /// <param name="timeout">��ʱʱ�䣨���룩</param>
        /// <param name="cancellationToken">ȡ������</param>
        /// <returns>��ȡ����HL7��Ϣ</returns>
        public async Task<string> ReadMessageAsync(Stream stream, int timeout, CancellationToken cancellationToken = default)
        {
            using var timeoutCts = new CancellationTokenSource(timeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, cancellationToken);

            try
            {
                var buffer = new MemoryStream();
                bool startFound = false;
                bool endFound = false;
                byte previousByte = 0;

                while (!endFound && !linkedCts.Token.IsCancellationRequested)
                {
                    int byteRead = await stream.ReadAsync(new byte[1], 0, 1, linkedCts.Token);
                    
                    if (byteRead == 0)
                    {
                        // �����ȡΪ0�������������ѶϿ��򵽴�����β
                        throw new IOException("�����ѶϿ��򵽴�����β");
                    }

                    byte currentByte = ((MemoryStream)stream).ToArray()[0];

                    if (!startFound)
                    {
                        if (currentByte == VT)
                        {
                            startFound = true;
                        }
                        continue;
                    }

                    buffer.WriteByte(currentByte);

                    if (previousByte == FS && currentByte == CR)
                    {
                        endFound = true;
                        buffer.SetLength(buffer.Length - 2); // �Ƴ�FS��CR
                        break;
                    }

                    previousByte = currentByte;
                }

                if (!endFound)
                {
                    if (timeoutCts.Token.IsCancellationRequested)
                    {
                        throw new TimeoutException("��ȡMLLP��Ϣ��ʱ");
                    }
                    else if (cancellationToken.IsCancellationRequested)
                    {
                        throw new OperationCanceledException("��ȡMLLP��Ϣ������ȡ��");
                    }
                }

                return _encoding.GetString(buffer.ToArray());
            }
            catch (Exception ex) when (!(ex is TimeoutException || ex is OperationCanceledException))
            {
                Log.Error(ex, "��ȡMLLP��Ϣʱ����");
                throw;
            }
        }

        /// <summary>
        /// ������д��һ��MLLP��ʽ��HL7��Ϣ
        /// </summary>
        /// <param name="stream">�����</param>
        /// <param name="message">HL7��Ϣ</param>
        /// <param name="cancellationToken">ȡ������</param>
        public async Task WriteMessageAsync(Stream stream, string message, CancellationToken cancellationToken = default)
        {
            try
            {
                byte[] mllpMessage = WrapMessage(message);
                await stream.WriteAsync(mllpMessage, 0, mllpMessage.Length, cancellationToken);
                await stream.FlushAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "д��MLLP��Ϣʱ����");
                throw;
            }
        }
    }
} 