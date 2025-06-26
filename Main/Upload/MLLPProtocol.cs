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
    /// MLLP协议处理类
    /// 负责处理MLLP协议的消息分割和封装
    /// MLLP协议：使用垂直标签符(VT, 0x0B)作为开始符，文件结束符(FS, 0x1C)和回车符(CR, 0x0D)作为结束符
    /// </summary>
    public class MLLPProtocol
    {
        // MLLP协议定义的特殊字符
        public const byte VT = 0x0B; // 垂直标签符，用作起始符
        public const byte FS = 0x1C; // 文件分隔符，用作终止符
        public const byte CR = 0x0D; // 回车符，用作终止符

        // 编码格式
        private readonly Encoding _encoding;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="encoding">编码格式</param>
        public MLLPProtocol(Encoding encoding)
        {
            _encoding = encoding ?? Encoding.UTF8;
        }

        /// <summary>
        /// 将HL7消息包装成MLLP格式
        /// </summary>
        /// <param name="message">原始HL7消息</param>
        /// <returns>MLLP格式的消息字节数组</returns>
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
                Log.Error(ex, "将HL7消息包装成MLLP格式时出错");
                throw;
            }
        }

        /// <summary>
        /// 从MLLP格式中提取HL7消息
        /// </summary>
        /// <param name="mllpMessage">MLLP格式的消息字节数组</param>
        /// <returns>提取出的HL7消息</returns>
        public string UnwrapMessage(byte[] mllpMessage)
        {
            try
            {
                if (mllpMessage == null || mllpMessage.Length < 3)
                {
                    throw new ArgumentException("MLLP消息格式不正确，长度过短");
                }

                if (mllpMessage[0] != VT || mllpMessage[mllpMessage.Length - 2] != FS || mllpMessage[mllpMessage.Length - 1] != CR)
                {
                    throw new ArgumentException("MLLP消息格式不正确，缺少起始或结束标记");
                }

                return _encoding.GetString(mllpMessage, 1, mllpMessage.Length - 3);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "从MLLP格式中提取HL7消息时出错");
                throw;
            }
        }

        /// <summary>
        /// 从流中读取一条完整的MLLP消息
        /// </summary>
        /// <param name="stream">输入流</param>
        /// <param name="timeout">超时时间（毫秒）</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>读取到的HL7消息</returns>
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
                        // 如果读取为0，可能是连接已断开或到达流结尾
                        throw new IOException("连接已断开或到达流结尾");
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
                        buffer.SetLength(buffer.Length - 2); // 移除FS和CR
                        break;
                    }

                    previousByte = currentByte;
                }

                if (!endFound)
                {
                    if (timeoutCts.Token.IsCancellationRequested)
                    {
                        throw new TimeoutException("读取MLLP消息超时");
                    }
                    else if (cancellationToken.IsCancellationRequested)
                    {
                        throw new OperationCanceledException("读取MLLP消息操作被取消");
                    }
                }

                return _encoding.GetString(buffer.ToArray());
            }
            catch (Exception ex) when (!(ex is TimeoutException || ex is OperationCanceledException))
            {
                Log.Error(ex, "读取MLLP消息时出错");
                throw;
            }
        }

        /// <summary>
        /// 向流中写入一条MLLP格式的HL7消息
        /// </summary>
        /// <param name="stream">输出流</param>
        /// <param name="message">HL7消息</param>
        /// <param name="cancellationToken">取消令牌</param>
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
                Log.Error(ex, "写入MLLP消息时出错");
                throw;
            }
        }
    }
} 