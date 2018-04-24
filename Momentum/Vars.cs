using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;

namespace Momentum
{
    class Vars
    {
        public static bool IsAppClosed = false;
        public static object object_for_lock = new object();
        public static NumberFormatInfo FormatInfo = new NumberFormatInfo { NumberDecimalSeparator = "." };
        public static List<ChartWindow> main_windows = new List<ChartWindow>();        
        public static FileLoader FileLoader = new FileLoader();
        public static string core_path;
        public static string server_ip = "95.169.184.186";
        public static string login = "";
        public static string pass = "";
        public static DateTime? ServerDate = DateTime.Now;
        public static Dictionary<string, ServerData> ServerQueries = new Dictionary<string, ServerData>();
        public static Dictionary<string, double[]> InstrumentsInfoDictionary = new Dictionary<string, double[]>();

        public static bool IsOnConnection = true;
        
        public static List<string> ServerMessages = new List<string>();
        public static RealTimeLoader RealTimeLoader = null;
        public static double MathRound(double value)
        {
            return MathRound(value, 0);
        }
        public static double MathRound(double value, int digits)
        {
            string[] valueSplit = value.ToString(FormatInfo).Split('.');
            if (valueSplit.Length == 2)//если есть дробная часть после запятой
            {
                if (valueSplit[1].Last() == '5' && valueSplit[1].Length == digits + 1)//если последняя цыфра дробной части = 5 то
                    value += 1 / Math.Pow(10, valueSplit[1].Length);//то увеличиваем её с 5 до 6

                value = Math.Round(value, digits);
            }

            return value;
        }
    }

    public class ServerData
    {
        public int LastTickIndex { get; set; }
        public List<Tick> TicksList { get; set; }
    }

    public class Tick
    {
        public double price { get; set; }
        public double originalPrice { get; set; }
        public double volume { get; set; }
        public DateTime? date { get; set; }
        public double id { get; set; }
        public int side { get; set; }
    }

    public class Cluster
    {
        public double price { get; set; }
        public double volume { get; set; }
        public double buy { get; set; }
        public double sell { get; set; }
    }

    public class MinuteBar
    {
        public DateTime? date { get; set; }
        public double price_open { get; set; }
        public double price_close { get; set; }
        public List<Cluster> minutka { get; set; }
    }
    
    public enum VertcalHistogramType { Volume, CumulativeDelta }
}
