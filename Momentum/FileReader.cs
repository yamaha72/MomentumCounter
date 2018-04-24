using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Windows;

namespace Momentum
{
    public class FileReader
    {
        public bool stop_load = false;
        Random _random = new Random();

        public double ReadFile(List<Tick> Ticks, List<MinuteBar> Minutess, string symbol, DateTime? st, out DateTime lastHistoryTickDateTime, double price_step, int rzrdn)
        {
            lastHistoryTickDateTime = new DateTime();
            double lastTickId = 0;
               string contract = st.Value.Year.ToString();

            string directoryPath = Vars.core_path + "\\tickdata\\" + symbol + "\\" + contract;//формир путь к файлу
            string fileName = st.Value.ToString("yyyy.MM.dd");

            if (st.Value.Date < Vars.ServerDate.Value.Date)
            {
                string directoryPathMin = directoryPath + "\\Minutes";
                string file_path = directoryPathMin + "\\" + fileName + ".zip";
                if (!File.Exists(file_path))
                {
                    if (!Directory.Exists(directoryPathMin))
                        Directory.CreateDirectory(directoryPathMin);

                    if(Vars.IsOnConnection)
                        lock(Vars.FileLoader)
                            Vars.FileLoader.DownLoadFile(file_path, symbol, contract, "Minutes", fileName);
                }

                if (File.Exists(file_path))
                {
                    List<string> allLines = ReadZipFile(file_path);
                    Read_minutes(allLines, Minutess, fileName, symbol, price_step, rzrdn, out lastHistoryTickDateTime);
                }
            }

            if (Minutess == null || Minutess.Count == 0)
            {
                directoryPath += "\\Ticks";
                string file_path_without_extension = directoryPath + "\\" + fileName;
                string filePath = file_path_without_extension + ".zip";

                bool isToday = false;
                if (!File.Exists(filePath))
                {
                    if (st.Value.Date >= Vars.ServerDate.Value.Date)
                    {
                        filePath = file_path_without_extension + _random.Next(1000, 5000) + ".zip";
                        isToday = true;
                    }

                    if (!Directory.Exists(directoryPath))
                        Directory.CreateDirectory(directoryPath);

                    if (Vars.IsOnConnection)
                        lock (Vars.FileLoader)
                            Vars.FileLoader.DownLoadFile(filePath, symbol, contract, "Ticks", fileName);
                }

                if (File.Exists(filePath))//если путь к файлу правдив - выполняем чтение файла
                {
                    List<string> allLines = ReadZipFile(filePath);
                    lastTickId = ReadAllTicks(allLines, Ticks, fileName, out lastHistoryTickDateTime, price_step, symbol, rzrdn);                  
                    if (isToday)
                        File.Delete(filePath);
                }
            }

            return lastTickId;
        }

        private List<string> ReadZipFile(string zipPath)
        {
            List<string> allLines = new List<string>();            
            try
            {
                using (FileStream inputStream = File.Open(zipPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    using (GZipStream gZip = new GZipStream(inputStream, CompressionMode.Decompress, true))
                    {
                        //using(MemoryStream memoryStream = new MemoryStream())
                       
                        int bufferSize = 4096, count = 4096;
                        byte[] buffer = new byte[bufferSize];
                        string brokenLine = "";
                        while (count == bufferSize)
                        {
                            if (stop_load) break;
                            count = gZip.Read(buffer, 0, bufferSize);
                            if (count > 0)
                            {
                                //memoryStream.Write(buffer, 0, count);

                                string line = brokenLine + Encoding.ASCII.GetString(buffer, 0, count);
                                string[] splitLine = line.Split('\n');
                                brokenLine = splitLine[splitLine.Length - 1];
                                for (int i = 0; i < splitLine.Length - 1; i++)
                                    allLines.Add(splitLine[i]);
                            }
                        }

                        /*byte[] unpackedBytes = memoryStream.ToArray();
                        string line = Encoding.ASCII.GetString(unpackedBytes, 0, unpackedBytes.Length);
                        allLines = line.Split('\n').ToList();*/
                    }
                }
            }
            catch { }

            return allLines;
        }

        private double ReadAllTicks(List<string> allLines, List<Tick> Ticks, string fileName, out DateTime lastHistoryTickDateTime, double price_step, string symbol, int rzrdn)
        {
            double lastTickID = 0;
            lastHistoryTickDateTime = new DateTime();
            string[] dateSplit = fileName.Split('.');

            foreach (string line in allLines)
            {
                try
                {
                    if (stop_load) break;
                    string[] razrez = line.Split(';');
                    if (razrez.Length < 5)
                        continue;

                    razrez[razrez.Length - 1] = razrez[razrez.Length - 1].Replace("\r", "");
                    Tick newTick = new Tick();
                    newTick.originalPrice = Convert.ToDouble(razrez[1], Vars.FormatInfo);
                    newTick.volume = Convert.ToDouble(razrez[2], Vars.FormatInfo);
                    if (newTick.originalPrice == 0 || newTick.volume == 0) continue;
                    newTick.price = RoundPriceStep(symbol, newTick.originalPrice, price_step, rzrdn);
                    newTick.date =
                        lastHistoryTickDateTime = DateTime.ParseExact(razrez[0], "yyyyMMdd HHmmssfff", CultureInfo.InvariantCulture);
                    newTick.side = Convert.ToInt32(razrez[3]);
                    lastTickID =
                        newTick.id = Convert.ToInt64(razrez[4]);

                    Ticks.Add(newTick);
                }
                catch { }
            }

            return lastTickID;
        }

        private void Read_minutes(List<string> allLines, List<MinuteBar> Minutes, string fileName, string symbol, double price_step, int rzrdn, out DateTime lastHistoryTickDateTime)
        {
            string[] dateSplit = fileName.Split('.');
            int year = Convert.ToInt32(dateSplit[0]), month = Convert.ToInt32(dateSplit[1]), day = Convert.ToInt32(dateSplit[2]);
            
            foreach (string line in allLines)
            {
                try
                {
                    if (stop_load) break;
                    if (line == "") continue;
                    string[] razrez = line.Split(';');
                    razrez[razrez.Length - 1] = razrez[razrez.Length - 1].Replace("\r", "");
                    if (razrez[0] != "-")
                    {
                        if (price_step != Vars.InstrumentsInfoDictionary[symbol][0] && Minutes.Count > 0)
                        {
                            Minutes.Last().minutka = RecalculateCluster(symbol, price_step, Minutes.Last().minutka, rzrdn);
                            Minutes.Last().price_open = RoundPriceStep(symbol, Minutes.Last().price_open, price_step, rzrdn);
                            Minutes.Last().price_close = RoundPriceStep(symbol, Minutes.Last().price_close, price_step, rzrdn);
                        }

                        double price_o = Convert.ToDouble(razrez[1], Vars.FormatInfo);
                        double price_c = Convert.ToDouble(razrez[2], Vars.FormatInfo);
                        if (price_o == 0 || price_c == 0) continue;
                        int hour = Convert.ToInt32(razrez[0].Substring(0, 2));
                        int min = Convert.ToInt32(razrez[0].Substring(2, 2));
                        int sec = Convert.ToInt32(razrez[0].Substring(4, 2));
                        DateTime? dateTime = new DateTime(year, month, day, hour, min, sec);

                        Minutes.Add(new MinuteBar
                        {
                            date = dateTime,
                            price_open = price_o,
                            price_close = price_c,
                            minutka = new List<Cluster>()
                        });
                    }
                    else
                    {
                        double price_t = Convert.ToDouble(razrez[1], Vars.FormatInfo);
                        double volm = Convert.ToDouble(razrez[2], Vars.FormatInfo);
                        if (price_t == 0 || volm == 0) continue;
                        Cluster hk = new Cluster { price = price_t, volume = volm, buy = Convert.ToDouble(razrez[3], Vars.FormatInfo), sell = Convert.ToDouble(razrez[4], Vars.FormatInfo) };
                        Minutes.Last().minutka.Add(hk);
                    }
                }
                catch { }
            }

            if (price_step != Vars.InstrumentsInfoDictionary[symbol][0] && Minutes.Count > 0)
            {
                Minutes.Last().minutka = RecalculateCluster(symbol, price_step, Minutes.Last().minutka, rzrdn);
                Minutes.Last().price_open = RoundPriceStep(symbol, Minutes.Last().price_open, price_step, rzrdn);
                Minutes.Last().price_close = RoundPriceStep(symbol, Minutes.Last().price_close, price_step, rzrdn);
            }

            lastHistoryTickDateTime = new DateTime();
            if (Minutes.Count > 0)
                lastHistoryTickDateTime = Minutes.Last().date.Value.AddSeconds(30).AddMilliseconds(-1);
        }

        private List<Cluster> RecalculateCluster(string symbol, double price_step, List<Cluster> minutka, int rzrdn)
        {
            foreach (Cluster cluster in minutka)
                cluster.price = RoundPriceStep(symbol, cluster.price, price_step, rzrdn);

            List<Cluster> bar = new List<Cluster>();
            IEnumerable<IGrouping<double, Cluster>> gr_intrv = minutka.GroupBy(it => it.price, it => it);

            foreach (IGrouping<double, Cluster> group in gr_intrv)
            {
                double rating = group.Sum(gr => gr.volume);
                double buys = group.Sum(gr => gr.buy);
                double sells = group.Sum(gr => gr.sell);
                bar.Add(new Cluster { price = group.Key, volume = rating, buy = buys, sell = sells });
            }

            return bar;
        }

        public double RoundPriceStep(string symbol, double originalPrice, double price_step, int rzrdn)
        {
            if (price_step != Vars.InstrumentsInfoDictionary[symbol][0])
            {
                double priceIndex = originalPrice / price_step;
                //priceIndex = Vars.MathRound(priceIndex);
                string[] priceIndexSplit = priceIndex.ToString(Vars.FormatInfo).Split('.');
                if (priceIndexSplit.Length == 2)
                {
                    if (priceIndexSplit[1].Last() == '5' && priceIndexSplit[1].Length == 1)
                        priceIndex = Math.Ceiling(priceIndex);
                    else
                        priceIndex = Math.Round(priceIndex);
                }

                return Math.Round(priceIndex * price_step, rzrdn);
            }

            return originalPrice;
        }
    }
}
