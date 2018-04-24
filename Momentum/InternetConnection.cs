using System;
using System.Runtime.InteropServices;

namespace RTT.Terminal
{
    class InternetConnection
    {
        public bool isInternetConnected;//булпеременная идентифицирующая наличие инета
        public bool isUsingModem;
        public bool isUsingLAN;
        public bool isOffline;
        public bool isUsingProxy;
        public bool isRasEnabled;


        [Flags]
        public enum InternetConnectionState : int
        {
            INTERNET_CONNECTION_MODEM = 0x1,
            INTERNET_CONNECTION_LAN = 0x2,
            INTERNET_CONNECTION_PROXY = 0x4,
            INTERNET_RAS_INSTALLED = 0x10,
            INTERNET_CONNECTION_OFFLINE = 0x20,
            INTERNET_CONNECTION_CONFIGURED = 0x40
        }

        [DllImport("WININET", CharSet = CharSet.Auto)]
        static extern bool InternetGetConnectedState(ref InternetConnectionState lpdwFlags, int dwReserved);

        public void Init()
        {
            InternetConnectionState flags = 0;
            isInternetConnected = InternetGetConnectedState(ref flags, 0);
            isUsingModem = (flags & InternetConnectionState.INTERNET_CONNECTION_MODEM) != 0;
            isUsingLAN = (flags & InternetConnectionState.INTERNET_CONNECTION_LAN) != 0;
            isOffline = (flags & InternetConnectionState.INTERNET_CONNECTION_OFFLINE) != 0;
            isUsingProxy = (flags & InternetConnectionState.INTERNET_CONNECTION_PROXY) != 0;
            isRasEnabled = (flags & InternetConnectionState.INTERNET_RAS_INSTALLED) != 0;
        }
    }
}
