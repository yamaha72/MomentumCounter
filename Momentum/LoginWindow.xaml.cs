using System;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace Momentum
{
    public partial class LoginWindow : Window
    {
        string _serverResponse = "";
        bool _isStartApp = false;
        
        public LoginWindow()
        {
            try
            {
                bool createdNew = false;
                Mutex mutex = new Mutex(true, "CSharpHow_SingleInstanceApp", out createdNew);
                if (!createdNew)
                {
                    MessageBox.Show("'RTT' is already running.");
                    Environment.Exit(0);
                }
                else
                {
                    InitializeComponent();
                    this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                    _isStartApp = true;
                    SetDataPath();

                    bool isSaveLogin = ReadLoginPassword();
                    textbox_1.Text = Vars.login;
                    if (isSaveLogin)
                    {
                        CheckBoxSaveLogin.IsChecked = true;
                        passwordbox_1.Password = Vars.pass;
                        Connect(null,null);
                    }
                }
            }
            catch { }
        }

        public LoginWindow(bool isWrongLogin)
        {
            InitializeComponent();
            try
            {
                this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                _isStartApp = false;
                textbox_1.Text = Vars.login;  
                passwordbox_1.Password = Vars.pass;
                CheckBoxSaveLogin.IsChecked = ReadLoginPassword();
                ConnectButton.IsEnabled = false;
                if (isWrongLogin)
                {
                    feed_back_label.Content = "Incorrect Username or Password";
                    _serverResponse = "WrongLogin";//чтобы при закрытии данного окна закрылось всё приложение
                }
            }
            catch {
            }
        }

        private void SetDataPath()
        {
            try
            {
                string fullAppPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                string disk = fullAppPath.Split(':')[0];
                Vars.core_path = disk + ":\\RTT_Data";
            }
            catch { }
        }

        private void Connect(object sender, RoutedEventArgs e)
        {
            try
            {
                Vars.login = textbox_1.Text;
                Vars.pass = passwordbox_1.Password; 
                ConnectButton.IsEnabled = false;

                if (Vars.RealTimeLoader == null)
                    Vars.RealTimeLoader = new RealTimeLoader();
                else if(Vars.RealTimeLoader.IsCanLoad)
                {
                    //останавливаем работающий РеалТайм-поток
                    Vars.RealTimeLoader.IsCanLoad = false;
                    //Инициируем новый РеалТайм-обьект на тот случай если в старом обьекте поток ещё не успел остановиться
                    Vars.RealTimeLoader = new RealTimeLoader();
                }

                _serverResponse = Vars.RealTimeLoader.SendLogin();
                if (_serverResponse == "WrongLogin")
                {
                    feed_back_label.Content = "Incorrect Username or Password";
                    ConnectButton.IsEnabled = true;
                    return;
                }

                if (_serverResponse != "NeedUpdate")
                {
                    if (_isStartApp)
                    {
                        if (Vars.InstrumentsInfoDictionary.Count > 0)
                            new ChartWindow().Show();
                        else//NoConnection, ServerError, ConnectionLost
                        {
                            feed_back_label.Content = "              No connection";
                            ConnectButton.IsEnabled = true;
                            return;
                        }
                    }
                    else if (Vars.InstrumentsInfoDictionary.Count == 0)
                    {
                        feed_back_label.Content = "              No connection";
                        ConnectButton.IsEnabled = true;
                        return;
                    }
                }

                Close();
            }
            catch { }
        }
        
        private bool ReadLoginPassword()
        {
            bool isSaveLogin = false;

            try
            {
                string bin_path = Vars.core_path + "\\RTTSttngs.bin";
                if (File.Exists(bin_path))
                {
                    using (BinaryReader reader = new BinaryReader(File.Open(bin_path, FileMode.Open, FileAccess.Read, FileShare.Read)))
                    {
                        while (reader.BaseStream.Position != reader.BaseStream.Length)
                        {
                            isSaveLogin = reader.ReadBoolean();
                            Vars.login = reader.ReadString();
                            if (isSaveLogin)
                                Vars.pass = reader.ReadString();
                            else break;
                        }

                        reader.Close();
                    }
                }
            }
            catch { }

            return isSaveLogin;
        }
        
        private void TextLoginChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                ConnectButton.IsEnabled = true;
                string login = textbox_1.Text;
                if (login.Length > 14)
                {
                    int frt_1 = textbox_1.SelectionStart;
                    login = login.Substring(0, 14);
                    textbox_1.Text = login;
                    textbox_1.SelectionStart = frt_1;
                }
            }
            catch { }
        }

        private void PasswordBoxChanged(object sender, RoutedEventArgs e)
        {
            try
            {
                ConnectButton.IsEnabled = true;
                string passw = passwordbox_1.Password;
                if (passw.Length > 14)
                {
                    passw = passw.Substring(0, 14);
                    passwordbox_1.Password = passw;
                }
            }
            catch { }
        }

        private void WindowClosed(object sender, EventArgs e)
        {
            try
            {
                SaveLoginPassword();

                if(_serverResponse == "WrongLogin" || _serverResponse == "NeedUpdate")
                {
                    lock (Vars.main_windows)
                        for (int i = Vars.main_windows.Count - 1; i >= 0; i--)
                            Vars.main_windows[i].Close();

                    Vars.IsAppClosed = true;
                    if (Vars.RealTimeLoader != null)
                        Vars.RealTimeLoader.IsCanLoad = false;

                    Environment.Exit(0);
                }
            }
            catch { }
        }

        private void SaveLoginPassword()
        {
            string bin_path = Vars.core_path;
            if (!Directory.Exists(bin_path))
                Directory.CreateDirectory(bin_path);

            bin_path += "\\RTTSttngs.bin";
            Vars.login = textbox_1.Text;
            Vars.pass = passwordbox_1.Password;
            bool isSaveLogin = CheckBoxSaveLogin.IsChecked.Value;

            using (BinaryWriter writer = new BinaryWriter(File.Open(bin_path, FileMode.Create)))
            {
                writer.Write(isSaveLogin);
                writer.Write(Vars.login);

                if (isSaveLogin)
                    writer.Write(Vars.pass);

                writer.Close();
            }
        }
    }
}
