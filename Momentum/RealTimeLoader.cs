using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows;

namespace Momentum
{
    public class RealTimeLoader
    {
        public bool IsCanLoad = false;

        public string SendLogin()
        {
            int RTT_Version = 1;
            string returnResponse = "NoConnection";           
            TcpClient tcpClnt = Connect();
            NetworkStream stream = null;
            if (tcpClnt != null)
            {
                stream = tcpClnt.GetStream();
                try
                {
                    if (WriteMessage("Connect;" + Vars.login + "," + Vars.pass + ";\n", stream))
                    {
                        int i = 0;
                        while (!stream.DataAvailable && i < 50)
                        {
                            Thread.Sleep(200);
                            i++;
                        }

                        if (stream.DataAvailable)
                        {
                            string response = ReadMessage(stream);
                            if (response != "Error")
                            {
                                string[] responceSplit = response.Split(';');                               
                                if (responceSplit[0] == "LoginOK")
                                {
                                    if (responceSplit.Length > 1 && responceSplit[1] != "")
                                    {
                                        int currentRttVersion = Convert.ToInt32(responceSplit[1]);
                                        if (currentRttVersion > RTT_Version)
                                        {
                                            returnResponse = "NeedUpdate";
                                            WriteMessage("Disconnect;" + Vars.login + ";\n!!EndMsg!!\n", stream);
                                            OpenDownloadLink();
                                        }
                                        else if (responceSplit.Length == 4)
                                        {
                                            returnResponse = ParseSymbolsContracts(responceSplit[2], true);// returnResponse = "LoginOK" || returnResponse = "NoConnection"
                                            if (returnResponse == "LoginOK" && Vars.InstrumentsInfoDictionary.Count == 0)
                                            {
                                                returnResponse = "NeedUpdate";
                                                WriteMessage("Disconnect;" + Vars.login + ";\n!!EndMsg!!\n", stream);
                                                OpenDownloadLink();
                                            }
                                        }
                                    }
                                }
                                else if (responceSplit[0] == "WrongLogin")
                                    returnResponse = "WrongLogin";
                                //else if responceSplit[0] == "ServerError" то returnResponse остаётся = "NoConnection"
                            }
                        }
                    }
                }
                catch { }

                if (returnResponse != "LoginOK")
                {
                    tcpClnt.Close();
                    tcpClnt = null;
                }
            }

            if (returnResponse == "NoConnection" || returnResponse == "LoginOK")// returnResponse != "NeedUpdate" && returnResponse != "WrongLogin"
            {
                if (Vars.InstrumentsInfoDictionary.Count == 0)
                    lock(Vars.InstrumentsInfoDictionary)
                        Vars.InstrumentsInfoDictionary = GetInstrumentsInfo();

                //if (Vars.InstrumentsInfoDictionary.Count > 0)
                //{
                    Tuple<TcpClient, NetworkStream, bool, int> arg = Tuple.Create(tcpClnt, stream, returnResponse == "LoginOK", RTT_Version);
                    BackgroundWorker bgw = new BackgroundWorker();
                    bgw.DoWork += RealTimeWork;//(obj, e) => (tcpClnt, stream, returnResponse == "LoginOK");
                    bgw.RunWorkerCompleted += BackgroundWorkerCompleted;
                    bgw.RunWorkerAsync(arg);
                //}
            }

            return returnResponse;
        }

        private TcpClient Connect()
        {
            TcpClient tcpclnt = null;
            try
            {
                tcpclnt = new TcpClient();
                tcpclnt.ReceiveTimeout = 3000;
                tcpclnt.SendTimeout = 3000;
                //tcpclnt.ReceiveBufferSize = 150000;
                tcpclnt.Connect(Vars.server_ip, 2216);
                Vars.IsOnConnection = true;
            }
            catch
            {
                Vars.IsOnConnection = false;
                if (tcpclnt != null)
                {
                    tcpclnt.Close();
                    tcpclnt = null;
                }
            }

            return tcpclnt;
        }

        private string ParseSymbolsContracts(string serverMessage, bool isFromSendLogin)
        {
            try
            {
                lock (Vars.InstrumentsInfoDictionary)
                {
                    Vars.InstrumentsInfoDictionary.Clear();
                    List<string> serverSymbols = serverMessage.Split('|').ToList();
                    Dictionary<string, double[]> instrumentsInfoDictionary = GetInstrumentsInfo();
                    foreach (string symbol in instrumentsInfoDictionary.Keys)
                        if (serverSymbols.Contains(symbol))
                            Vars.InstrumentsInfoDictionary.Add(symbol, instrumentsInfoDictionary[symbol]);
                }

                return "LoginOK";
            }
            catch
            {
                if(isFromSendLogin)
                    lock(Vars.InstrumentsInfoDictionary)
                        Vars.InstrumentsInfoDictionary = GetInstrumentsInfo();

                Vars.IsOnConnection = false;
                return "NoConnection";
            }
        }

        private bool WriteMessage(string message, NetworkStream stream)
        {
            try
            {
                byte[] sendMessage = Encoding.ASCII.GetBytes(message);
                stream.Write(sendMessage, 0, sendMessage.Length);
                return true;
            }
            catch
            {
                Vars.IsOnConnection = false;
                return false;
            }
        }

        private string ReadMessage(NetworkStream stream)
        {
            try
            {
                string response = "";
                do
                {
                    byte[] responbe_bytes = new byte[1024];
                    int bytesCount = stream.Read(responbe_bytes, 0, responbe_bytes.Length);
                    response += Encoding.ASCII.GetString(responbe_bytes, 0, bytesCount);
                }
                while (stream.DataAvailable);

                return response.Replace("\0", "");
            }
            catch
            {
                Vars.IsOnConnection = false;
                return "Error";
            }
        }

        private void RealTimeWork(object sender, DoWorkEventArgs e)
        {
            Tuple<TcpClient, NetworkStream, bool, int> arg = (Tuple<TcpClient, NetworkStream, bool, int>)e.Argument;
            TcpClient tcpClnt = arg.Item1;
            NetworkStream stream = arg.Item2;
            bool isConnect = arg.Item3;
            int RTT_Version = arg.Item4;
            IsCanLoad = true;
            string serverResponse = "";
            DateTime timeStampLastQuery = new DateTime(), serverRecponceLastTime = DateTime.Now;

            while (!Vars.IsAppClosed && IsCanLoad) {
                try {
                    if (tcpClnt == null) {
                        tcpClnt = Connect();
                        if (tcpClnt == null){
                            Thread.Sleep(500);
                            continue;
                        }

                        stream = tcpClnt.GetStream();
                        string sendMessage;
                        if (isConnect)//если список инструментов с сервера мы уже получили
                            sendMessage = "Reconnect;" + Vars.login + "," + Vars.pass + ";\n";
                        else//если список инструментов с сервера мы ещё не получали
                            sendMessage = "Connect;" + Vars.login + "," + Vars.pass + ";\n";

                        if (!WriteMessage(sendMessage, stream)){
                            tcpClnt.Close();
                            tcpClnt = null;
                            continue;
                        }

                        int n = 0;
                        while (!stream.DataAvailable && n < 50) {
                            Thread.Sleep(200);
                            n++;
                        }

                        if (!stream.DataAvailable)
                        {
                            tcpClnt.Close();
                            tcpClnt = null;
                            continue;
                        }

                        string message = ReadMessage(stream);
                        if (message == "Error"){
                            tcpClnt.Close();
                            tcpClnt = null;
                            continue;
                        }

                        string[] messageSplit = message.Split(';');
                        if (messageSplit[0] == "WrongLogin") {
                            tcpClnt.Close();
                            tcpClnt = null;//после обрыва цыкла на сервер не нужно отправлять "Disconnect", т.к. на сервере TcpClient уже закрыт
                            serverResponse = "WrongLogin";
                            break;
                        }

                        if (!isConnect)//если список инструментов с сервера мы ещё не получали
                        {
                            if (messageSplit[0] == "LoginOK" && messageSplit.Length > 1 && messageSplit[1] != "")
                            {
                                int currentRttVersion = Convert.ToInt32(messageSplit[1]);
                                if (currentRttVersion > RTT_Version)
                                {
                                    serverResponse = "NeedUpdate";
                                    break;//после обрыва цыкла на сервер отправиться "Disconnect"
                                }
                                else if (messageSplit.Length == 4 && ParseSymbolsContracts(messageSplit[2], false) == "LoginOK")
                                {
                                    if (Vars.InstrumentsInfoDictionary.Count == 0)
                                    {
                                        serverResponse = "NeedUpdate";
                                        break;//после обрыва цыкла на сервер отправиться "Disconnect"
                                    }
                                    else
                                        isConnect = true;//идентифицируем что список инструментов с сервера мы уже получили
                                }
                                else
                                {
                                    tcpClnt.Close();
                                    tcpClnt = null;
                                    continue;
                                }
                            }
                            else//messageSplit[0] == "ServerError"
                            {
                                tcpClnt.Close();
                                tcpClnt = null;
                                continue;
                            }
                        }
                        //else isConnect == true, значит на сервер отправляли запрос "Reconnect", значит messageSplit[0] по-любому == "LoginOK"

                        serverRecponceLastTime = DateTime.Now;
                        timeStampLastQuery = new DateTime();
                    }

                    string serverQuery = "";
                    if ((DateTime.Now - timeStampLastQuery).TotalSeconds >= 30) {
                        serverQuery = "T;\n";
                        timeStampLastQuery = DateTime.Now;
                    }

                    serverQuery += GetServerQueryes();
                    if (serverQuery == ""){
                        Thread.Sleep(500);
                        continue;
                    }

                    serverQuery += "!!EndMsg!!\n";
                    stream.Flush();
                    if (!WriteMessage(serverQuery, stream)){
                        tcpClnt.Close();
                        tcpClnt = null;
                        continue;
                    }

                    int i = 0;
                    string brokenLine = "";
                    start:
                    while (!stream.DataAvailable && i < 50) {
                        Thread.Sleep(200);
                        i++;
                    }

                    if (!stream.DataAvailable){
                        tcpClnt.Close();
                        tcpClnt = null;
                        continue;
                    }

                    string incomeMessage = ReadDecoder(stream);
                    if (incomeMessage != "" && incomeMessage != "Error"){
                        bool isEndMsg = ProcessReceive(incomeMessage, brokenLine, tcpClnt, out brokenLine, out serverResponse);
                        serverRecponceLastTime = DateTime.Now;
                        if (!isEndMsg) {
                            i = 35;
                            goto start;
                        }
                    }
                    else if (incomeMessage == "Error" || (DateTime.Now - serverRecponceLastTime).TotalSeconds > 60){
                        tcpClnt.Close();
                        tcpClnt = null;
                    }
                }
                catch {
                    tcpClnt.Close();
                    tcpClnt = null;
                }
            }

            if (tcpClnt != null) {
                WriteMessage("Disconnect;" + Vars.login + ";\n!!EndMsg!!\n", stream);
                tcpClnt.Close();
                tcpClnt = null;
            }

            IsCanLoad = false;
            e.Result = serverResponse;
        }

        private string GetServerQueryes()
        {
            string serverQuery = "";
            try
            {
                lock (Vars.ServerQueries)
                    foreach (string query in Vars.ServerQueries.Keys)
                        serverQuery += "Q;" + query + "," + Vars.ServerQueries[query].LastTickIndex + ";\n";
            }
            catch { }

            return serverQuery;
        }

        private string ReadDecoder(NetworkStream stream)
        {
            string response = String.Empty;
            try
            {
                Decoder decoder = Encoding.ASCII.GetDecoder();
                do
                {
                    byte[] incomingBytes = new byte[2048];
                    int count_of_receive_bytes = stream.Read(incomingBytes, 0, incomingBytes.Length);
                    int count_of_chars = decoder.GetCharCount(incomingBytes, 0, count_of_receive_bytes);
                    char[] chars = new char[count_of_chars];
                    count_of_chars = decoder.GetChars(incomingBytes, 0, count_of_receive_bytes, chars, 0);//через decoder мы не можем получить сразу строку, пэ сначало получаем массив символов char[] chars
                    response += new String(chars, 0, count_of_chars);//из массива char[] chars формируем строку
                }
                while (stream.DataAvailable);
                return response.Replace("\0", "");
            }
            catch
            {
                Vars.IsOnConnection = false;
                return "Error";
            }
        }

        private bool ProcessReceive(string incomeMessage, string brokenLine, TcpClient tcpClnt, out string newBrokenLine, out string serverResponse)
        {
            serverResponse = "";
            bool isEndMsg = false;
            string[] allLines = (brokenLine + incomeMessage).Split('\n');
            newBrokenLine = "";

            for (int i = 0; i < allLines.Length; i++)
            {
                try
                {
                    if (allLines[i] == "") continue;
                    newBrokenLine = "";
                    isEndMsg = false;
                    string[] splitLine = allLines[i].Split(';');

                    if (splitLine[0] == "!!EndMsg!!")
                        isEndMsg = true;
                    else if (splitLine.Length > 2)
                    {
                        if (splitLine[0] == "T")
                            Vars.ServerDate = DateTime.ParseExact(splitLine[1], "yyyy.MM.dd HH:mm:ss", CultureInfo.InvariantCulture);
                        else if (splitLine[0] == "S")
                        {
                            if (splitLine[1] == "OtherUser")
                            {
                                serverResponse = "OtherUser";
                                tcpClnt.Close();
                                tcpClnt = null;//после обрыва цыкла на сервер не нужно отправлять "Disconnect", т.к. на сервере TcpClient уже закрыт
                                IsCanLoad = false;
                                isEndMsg = true;
                                break;
                            }
                            else if (splitLine[1] == "NotFound" && splitLine.Length == 4)//Not Found this Symbol
                                lock (Vars.ServerQueries)
                                    if (Vars.ServerQueries.ContainsKey(splitLine[2]))
                                        Vars.ServerQueries.Remove(splitLine[2]);
                        }
                        else if (splitLine[0] == "Q")
                        {
                            string[] querySplit = splitLine[1].Split(',');
                            string symbol = querySplit[0];
                            int lastTickIndex;
                            lock (Vars.ServerQueries)
                            {
                                if (Vars.ServerQueries.ContainsKey(symbol))
                                    lastTickIndex = Vars.ServerQueries[symbol].LastTickIndex;
                                else continue;
                            }

                            int tickIndex = Convert.ToInt32(querySplit[1]);
                            if (lastTickIndex == 0 || tickIndex == 0 || tickIndex == lastTickIndex + 1)
                            {
                                if (splitLine.Length == 8)
                                    ParseTick(splitLine, symbol, tickIndex);
                                else if (splitLine.Length < 8)
                                    newBrokenLine = allLines[i];
                            }
                            //else return true;
                        }
                        else if (splitLine[0] == "M")
                            lock (Vars.ServerMessages)
                                Vars.ServerMessages.Add(splitLine[1]);
                    }
                    else newBrokenLine = allLines[i];
                }
                catch { }
            }

            return isEndMsg;
        }

        private void ParseTick(string[] tickdata, string query, int tickIndex)
        {
            lock (Vars.ServerQueries)
            {
                if (Vars.ServerQueries.ContainsKey(query))
                    Vars.ServerQueries[query].LastTickIndex = tickIndex;
                else return;
            }

            double originalPrice = Convert.ToDouble(tickdata[3], Vars.FormatInfo);
            double volume = Convert.ToDouble(tickdata[4], Vars.FormatInfo);
            if (originalPrice == 0 || volume == 0)
                return;

            Tick currentTick = new Tick
            {
                date = DateTime.ParseExact(tickdata[2], "yyyyMMdd HHmmssfff", CultureInfo.InvariantCulture),//tickTime,
                originalPrice = originalPrice,
                volume = volume,
                id = Convert.ToInt32(tickdata[6]),
                side = Convert.ToInt32(tickdata[5])
            };

            lock (Vars.ServerQueries)
            {
                if (Vars.ServerQueries.ContainsKey(query))
                {
                    if (Vars.ServerQueries[query].TicksList.Count > 0 && Vars.ServerQueries[query].TicksList.Last().id > currentTick.id)//если у текущего тика увеличилась дата
                        Vars.ServerQueries[query].TicksList.Clear();

                    Vars.ServerQueries[query].TicksList.Add(currentTick);
                }
            }
        }

        private void BackgroundWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            try
            {
                string serverResponse = (string)e.Result;
                if (serverResponse == "WrongLogin")
                    new LoginWindow(true).ShowDialog();
                else if (serverResponse != "")
                {
                    lock (Vars.main_windows)
                        for (int i = Vars.main_windows.Count - 1; i >= 0; i--)
                            Vars.main_windows[i].Close();

                    Vars.IsAppClosed = true;

                    if (serverResponse == "OtherUser")
                        MessageBox.Show("Another user connected with the same login");
                    else// if (serverResponse == "NeedUpdate")
                        OpenDownloadLink();

                    Environment.Exit(0);
                }
            }
            catch { }
        }

        private void OpenDownloadLink()
        {
            MessageBox.Show("Download update");
            //Process.Start("https://drive.google.com/open?id=0B1xBffySxO6sakRROTlMUmd1LTg");
        }

        private Dictionary<string, double[]> GetInstrumentsInfo()
        {
            Dictionary<string, double[]> instrumentsInfoDictionary = new Dictionary<string, double[]>();
            
            instrumentsInfoDictionary.Add("BTC-ADA", new double[] { 0.00000001, 0.0000001 });
            instrumentsInfoDictionary.Add("BTC-BCC", new double[] { 0.00000001, 0.0001 });
            instrumentsInfoDictionary.Add("BTC-BTG", new double[] { 0.00000001, 0.00001 });
            instrumentsInfoDictionary.Add("BTC-DASH", new double[] { 0.00000001, 0.00001 });
            instrumentsInfoDictionary.Add("BTC-ETC", new double[] { 0.00000001, 0.000001 });
            instrumentsInfoDictionary.Add("BTC-ETH", new double[] { 0.00000001, 0.0001 });
            instrumentsInfoDictionary.Add("BTC-LTC", new double[] { 0.00000001, 0.00001 });
            instrumentsInfoDictionary.Add("BTC-NEO", new double[] { 0.00000001, 0.00001 });
            instrumentsInfoDictionary.Add("BTC-NXT", new double[] { 0.00000001, 0.00000001 });
            instrumentsInfoDictionary.Add("BTC-OMG", new double[] { 0.00000001, 0.000001 });
            instrumentsInfoDictionary.Add("BTC-XMR", new double[] { 0.00000001, 0.00001 });
            instrumentsInfoDictionary.Add("BTC-XRP", new double[] { 0.00000001, 0.0000001 });
            instrumentsInfoDictionary.Add("BTC-XVG", new double[] { 0.00000001, 0.00000001 });
            instrumentsInfoDictionary.Add("BTC-ZEC", new double[] { 0.00000001, 0.00001 });
            instrumentsInfoDictionary.Add("USDT-ADA", new double[] { 0.00000001, 0.001 });
            instrumentsInfoDictionary.Add("USDT-BCC", new double[] { 0.00000001, 1 });
            instrumentsInfoDictionary.Add("USDT-BTC", new double[] { 0.00000001, 1 });
            instrumentsInfoDictionary.Add("USDT-BTG", new double[] { 0.00000001, 0.1 });
            instrumentsInfoDictionary.Add("USDT-DASH", new double[] { 0.00000001, 1 });
            instrumentsInfoDictionary.Add("USDT-ETC", new double[] { 0.00000001, 0.1 });
            instrumentsInfoDictionary.Add("USDT-ETH", new double[] { 0.00000001, 0.1 });
            instrumentsInfoDictionary.Add("USDT-LTC", new double[] { 0.00000001, 0.01 });
            instrumentsInfoDictionary.Add("USDT-NEO", new double[] { 0.00000001, 0.1 });
            instrumentsInfoDictionary.Add("USDT-NXT", new double[] { 0.00000001, 0.001 });
            instrumentsInfoDictionary.Add("USDT-OMG", new double[] { 0.00000001, 0.001 });
            instrumentsInfoDictionary.Add("USDT-XMR", new double[] { 0.00000001, 0.1 });
            instrumentsInfoDictionary.Add("USDT-XRP", new double[] { 0.00000001, 0.001 });
            instrumentsInfoDictionary.Add("USDT-XVG", new double[] { 0.00000001, 0.0001 });
            instrumentsInfoDictionary.Add("USDT-ZEC", new double[] { 0.00000001, 0.1 });
            
            return instrumentsInfoDictionary;
        }
    }
}
