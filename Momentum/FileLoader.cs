using RTT.Terminal;
using System;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace Momentum
{
    public class FileLoader
    {
        static TcpClient _tcpClient = null;
        static NetworkStream _stream = null;
        static DateTime _lastTimeServerQuery = new DateTime();
        InternetConnection _internetConnect = new InternetConnection();
        //[System.Diagnostics.DebuggerNonUserCode]
        public void DownLoadFile(string filePath, string symbol, string contract, string content, string fileDate)
        {
            _internetConnect.Init();//вызываем ф-цию, кот проверяет наличие инета.
            if (!_internetConnect.isInternetConnected) return;
            string quiery = symbol + "\\" + contract + "\\" + content + "\\" + fileDate + ";\n";
            StartDownLoad(quiery, filePath, true);
        }

        private void StartDownLoad(string quiery, string filePath, bool isFirstDawnLoad)
        { 
            try
            {
                string fileSizeString;
                byte[] secondPart;
                ConnectRequest(quiery, out fileSizeString, out secondPart);
                if (fileSizeString == "")
                {
                    if (_tcpClient != null)
                    {
                        _tcpClient.Close();
                        _tcpClient = null;
                        _stream = null;
                    }

                    ConnectRequest(quiery, out fileSizeString, out secondPart);
                }

                if (fileSizeString == "" || fileSizeString == "NoFile" || Convert.ToInt64(fileSizeString) == 0)
                {
                    if (_tcpClient != null)
                    {
                        _tcpClient.Close();
                        _tcpClient = null;
                        _stream = null;
                    }

                    return;
                }
                else if(fileSizeString == "Request")
                    StartDownLoad(quiery, filePath, false);

                long fileSize = Convert.ToInt64(fileSizeString);
                _tcpClient.ReceiveBufferSize = (int)fileSize + 1000;
                using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite))
                {
                    long totalBytes = 0;
                    if (secondPart != null)
                    {
                        fs.Write(secondPart, 0, secondPart.Length);
                        totalBytes = secondPart.Length;
                    }

                    byte[] buffer = new byte[fileSize];
                    int receivedBytes = 1;
                    while (receivedBytes > 0 && totalBytes < fileSize)
                    {                        
                        receivedBytes = _stream.Read(buffer, 0, buffer.Length);
                        totalBytes += receivedBytes;
                        if (receivedBytes > 0)
                            fs.Write(buffer, 0, receivedBytes);  
                    }
                }

                _stream.Flush();
                if (File.Exists(filePath))
                {
                    FileInfo fileInfo = new FileInfo(filePath);
                    if (fileInfo.Length != fileSize)
                    {
                        File.Delete(filePath);
                        if (_tcpClient != null)
                        {
                            _tcpClient.Close();
                            _tcpClient = null;
                            _stream = null;
                        }

                        if(isFirstDawnLoad)
                            StartDownLoad(quiery, filePath, false);
                    }
                }
                else
                {
                    if (_tcpClient != null)
                    {
                        _tcpClient.Close();
                        _tcpClient = null;
                        _stream = null;
                    }

                    if (isFirstDawnLoad)
                        StartDownLoad(quiery, filePath, false);
                }
            }
            catch
            {
                if (File.Exists(filePath)) File.Delete(filePath);
                if (_tcpClient != null)
                {
                    _tcpClient.Close();
                    _tcpClient = null;
                    _stream = null;
                }

                if (isFirstDawnLoad)
                    StartDownLoad(quiery, filePath, false);
            }
        }
        
        private void ConnectRequest(string quiery, out string fileSizeString, out byte[] secondPart)
        {
            byte[] buffer = Encoding.ASCII.GetBytes(quiery);
            fileSizeString = "";
            secondPart = null;
            try
            {
                if (_tcpClient == null || (DateTime.Now - _lastTimeServerQuery).TotalSeconds >= 110)
                    Connect();
            }
            catch
            {
                if (_tcpClient != null)
                {
                    _stream.Flush();
                    _tcpClient.Close();
                    _tcpClient = null;
                    _stream = null;
                }

                return;
            }

            _stream.Flush();
            _stream.Write(buffer, 0, buffer.Length);
            _lastTimeServerQuery = DateTime.Now;
            ReadMessage(out fileSizeString, out secondPart);
        }

        private void Connect()
        {
            if (_tcpClient != null)
            {
                _tcpClient.Close();
                _tcpClient = null;
                _stream = null;
            }

            _tcpClient = new TcpClient();
            _tcpClient.ReceiveTimeout = 3000;
            _tcpClient.Connect(Vars.server_ip, 7616);
            _stream = _tcpClient.GetStream();
        }

        private void ReadMessage(out string fileSizeString, out byte[] secondPart)
        {
            fileSizeString = "";
            secondPart = null;

            byte[] buffer = new byte[1024];
            int readBytesCount = _stream.Read(buffer, 0, buffer.Length);

            string firstPart = "";
            for (int i = 0; i < readBytesCount; i++)
            {
                char ch = Convert.ToChar(buffer[i]);
                if (ch == ',')
                {
                    fileSizeString = firstPart;
                    if (i < readBytesCount - 1)
                    {
                        secondPart = new byte[readBytesCount - i - 1];
                        Array.Copy(buffer, i + 1, secondPart, 0, secondPart.Length);
                    }

                    break;
                }
                else
                    firstPart += ch;
            }

           /*string message = Encoding.ASCII.GetString(buffer, 0, readBytesCount).Replace("\0", "");
           string[] messageSplit = message.Split(',');
            if (messageSplit.Length >= 2)
            {
                fileSizeString = messageSplit[0];
                if (messageSplit[1] != "" || messageSplit.Length > 2)
                {
                    int bytesCount = Encoding.ASCII.GetByteCount(fileSizeString + ",");
                    secondPart = new byte[readBytesCount - bytesCount];
                    Array.Copy(buffer, bytesCount, secondPart, 0, readBytesCount - bytesCount);
                }
            }*/
        }
    }
}
