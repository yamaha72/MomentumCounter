using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Momentum
{
    public partial class ChartWindow : Window
    {
        public ChartWindow()
        {
           InitializeComponent();
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        #region buttons

        private void Click_1(object sender, RoutedEventArgs e)
        {
            ChartWindow win2 = new ChartWindow();
            win2.Show();
        }
        private void LoginClick(object sender, RoutedEventArgs e)
        {
            LoginWindow win2 = new LoginWindow(false);
            win2.ShowDialog();
        }
        private void AppClose(object sender, RoutedEventArgs e)
        {
            if (Vars.main_windows.Count > 1)
            {
                // Configure the message box to be displayed
                string messageBoxText = "Do you want to close RTT?";
                string caption = "Comfirmation";
                MessageBoxButton button = MessageBoxButton.YesNo;
                MessageBoxImage icon = MessageBoxImage.Question;

                // Display message box
                MessageBoxResult result = MessageBox.Show(messageBoxText, caption, button, icon);

                // Process message box results
                if (result == MessageBoxResult.Yes)
                {
                    Vars.IsAppClosed = true;
                    if (Vars.RealTimeLoader != null) Vars.RealTimeLoader.IsCanLoad = false;
                    lock (Vars.main_windows)
                        for (int i = Vars.main_windows.Count - 1; i >= 0; i--)
                            Vars.main_windows[i].Close();

                    Environment.Exit(0);
                }
            }
            else
            {
                Vars.IsAppClosed = true;
                if (Vars.RealTimeLoader != null) Vars.RealTimeLoader.IsCanLoad = false;
                Close();
                Environment.Exit(0);
            }
        }

        private void AllWindowMinimized(object sender, RoutedEventArgs e)
        {
            lock (Vars.main_windows)
                foreach (ChartWindow mw in Vars.main_windows)
                    mw.WindowState = WindowState.Minimized;
        }
        
        private void AllWindowNormalized(object sender, RoutedEventArgs e)
        {
            lock (Vars.main_windows)
                foreach (ChartWindow mw in Vars.main_windows)
                    mw.WindowState = WindowState.Normal;
        }

        #endregion

        #region Lists
               
        List<PriceScale> price_canvases = new List<PriceScale>();
        List<Chart> chart = new List<Chart>();

        class PriceScale
        {
            public double price { get; set; }
            public Label price_label { get; set; }
        }

        class Chart
        {
            public Border total_vol_gist { get; set; }
            public double vertic_gist_volum { get; set; }
            public List<Cluster> Bars { get; set; }
            public double bar_highs { get; set; }
            public double bar_lows { get; set; }
            public Border chart_b { get; set; }
            public Label date_Labels { get; set; }
            public Canvas time_labels { get; set; }
            public DateTime? time_of_bar { get; set; }
            public Canvas cnv_lbls { get; set; }
            public Canvas filters { get; set; }
            public List<ClusterProfile> canv_filters { get; set; }
            public int color { get; set; }
            public double open_price { get; set; }
            public double close_price { get; set; }
            public Rectangle shadow { get; set; }
            public double bar_highs_double { get; set; }
            public double bar_lows_double { get; set; }
            public double bar_height_pips_count { get; set; }
            public double candle_body_height_pips { get; set; }
            public double N_cel_in_Price_scale_list { get; set; }
            public double candle_body_N_cell { get; set; }
            public List<Label> volume_labels { get; set; }
            public Line VerticalHistogramLine { get; set; }
            public double cumulative_delta { get; set; }
            public double Delta { get;set;}
        }

        class ClusterProfile
        {
            public Rectangle fltr { get; set; }
            public double price { get; set; }
        }

        #endregion

        #region Window_Load_Close_ChangeSize

        private void Window_Close(object sender, CancelEventArgs e)//активируется при закрытии окна 
        {
            if (timer != null)
                timer.Stop();
            timer = null;
            IsWorkRealTime = false;
            Save_settings();
            
            hstr.stop_load = true;

            lock (Vars.main_windows)
            {
                Vars.main_windows.Remove(this);
                lock (Vars.ServerQueries)
                    if (Vars.ServerQueries.ContainsKey(ServerQuery))
                        if (!Vars.main_windows.Any(mw => mw.IsWorkRealTime && mw.ServerQuery == ServerQuery))
                            Vars.ServerQueries.Remove(ServerQuery);
            }

            check_close_app();
        }

        private void Save_settings()
        {
            if (instrument != "")
            {
                Save_fltrs();
                Save_lines();
            }
        }

        public static void check_close_app()
        {
            if (Vars.main_windows.Count == 0)
            {
                Vars.IsAppClosed = true;
                if (Vars.RealTimeLoader != null) Vars.RealTimeLoader.IsCanLoad = false;
            }
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)//для изменения размеров окна
        {
            if (st_frm_height != 0 && st_frm_width != 0)
            {
                if (WindowState == WindowState.Normal)
                {
                    double razn_height = this.Height - st_frm_height;
                    double razn_width = this.Width - st_frm_width;

                    changed_size_window(razn_height, razn_width);

                    st_frm_height = this.Height;
                    st_frm_width = this.Width;
                }
                else if (WindowState == WindowState.Maximized)
                {
                    double razn_height = this.RenderSize.Height - st_frm_height;
                    double razn_width = this.RenderSize.Width - st_frm_width;

                    changed_size_window(razn_height, razn_width);

                    st_frm_height = this.RenderSize.Height;
                    st_frm_width = this.RenderSize.Width;
                }
            }
        }

        private void changed_size_window(double razn_height, double razn_width)
        {
            main_grid.Height += razn_height;//меняем размеры графика
            border_chart.Height += razn_height;
           
            border_times.Margin = new Thickness(-1, border_times.Margin.Top + razn_height, 0, 0);
            border_prices.Height += razn_height;//ценовая шкала меняет тока высоту
            main_chart_canvas.Height += razn_height;
            button13.Margin = new Thickness(button13.Margin.Left + razn_width, 0, 0, 0);//двигаем кнопку "Свернуть все окна"
            button17.Margin = new Thickness(button17.Margin.Left + razn_width, 0, 0, 0);
            NormalStateButton.Margin = new Thickness(NormalStateButton.Margin.Left + razn_width, 0, 0, 0);
            main_grid.Width += razn_width;//меняем ширину графика
            border_chart.Width += razn_width;
            main_chart_canvas.Width = border_chart.Width-1;
            canvas_for_button.Width += razn_width;
            border_times.Width += razn_width;//временная шкала меняет тока ширину
            border_prices.Margin = new Thickness(border_prices.Margin.Left + razn_width, border_prices.Margin.Top, 0, 0);//двигаем ценовую шкалу
            label7.Margin = new Thickness(label7.Margin.Left + razn_width, label7.Margin.Top, 0, 0);//двигаем Баттон12 и лейбл "Лоадин"
            button12.Margin = new Thickness(button12.Margin.Left + razn_width, button12.Margin.Top, 0, 0);
            button14.Margin = new Thickness(button14.Margin.Left + razn_width, button14.Margin.Top + razn_height, 0, 0);
            button15.Margin = new Thickness(button15.Margin.Left + razn_width, button15.Margin.Top + razn_height, 0, 0);
            First_visibl_date_label.Margin = new Thickness(-2, First_visibl_date_label.Margin.Top + razn_height, 0, 0);

            if (canvas_prices.Children.Count > 0)
            {
                lock (chart)
                {
                    if (pozizionir)
                        Positionirov(true, true);//позиционируем график                       
                    else
                    {
                        double ps_x = canvas_chart.Margin.Left;
                        double pozt_y = canvas_chart.Margin.Top;

                        if (razn_width > 0)//если окно расширяется
                        {
                            double pos_X_last_bar_date = Canvas.GetLeft(chart[N_lb].chart_b);
                            if (ps_x < border_chart.Width / 2 - pos_X_last_bar_date)//чтобы график не задвигали сильно влево
                                ps_x = Math.Round(border_chart.Width / 2 - pos_X_last_bar_date, 0, MidpointRounding.AwayFromZero);

                            if (N_fb > 0)
                                insert_revers_sleva(ps_x, false);

                            if (N_lb < chart.Count - 1)
                                add_sprava(ps_x, false);
                        }
                        else if (razn_width < 0)//если окно cужается
                        {
                            double pos_X_first_bar_date = Canvas.GetLeft(chart[N_fb].chart_b);
                            if (ps_x > border_chart.Width / 2 - pos_X_first_bar_date)//чтобы график не задвигали сильно впрао
                                ps_x = Math.Round(border_chart.Width / 2 - pos_X_first_bar_date, 0, MidpointRounding.AwayFromZero);

                            remove_sprava(ps_x, false);
                        }

                        pozt_y = vverx_vniz(pozt_y);

                        canvas_chart.Margin = new Thickness(ps_x, pozt_y, 0, 0); //задаем новое положение для canvas1-график
                        canvas_prices.Margin = new Thickness(0, pozt_y, 0, 0); //задаем новое положение для ценовой шкалы
                        LinesCanvas.Margin = new Thickness(0, pozt_y, 0, 0);
                        canvas_times.Margin = new Thickness(ps_x, 0, 0, 0); //задаем новое положение для временной шкалы
                        canvas_vertical_line.Margin = new Thickness(ps_x, 0, 0, 0);
                        canvas_gistogramm.Margin = new Thickness(0, pozt_y, 0, 0);
                        canv_vert_gstgr.Margin = new Thickness(ps_x, border_chart.Height - VerticalHistohramBarMaxHeight, 0, 0);

                        PriceCursorSetLeft(canvas_chart.Margin.Left); 
                    }

                    if (canv_vert_gstgr.Children.Count > 0 && VerticalHistohramBarMaxHeight >= border_chart.Height - 75)
                    {
                        VerticalHistohramBarMaxHeight = border_chart.Height - 76;

                        canv_vert_gstgr.Margin = new Thickness(canv_vert_gstgr.Margin.Left, border_chart.Height - VerticalHistohramBarMaxHeight, 0, 0);
                        
                        updt_vert_hstgrm(chart.Count - 1, true);
                    }

                    foreach (ChartLines z in lines_Y)//меняем ширину горизонтальных линий
                        z.line_body.Width = border_chart.Width;

                    foreach (ChartLines z in lines_X)//меняем высоту вертикальных линий
                        z.line_body.Height = border_chart.Height;

                    //линия-курсор-указатель последней цены
                    //PriceCursor.line_body.Width = border_chart.Width;//ШИРИНА
                }
            }
            else if (canvas_chart.Children.Count > 0)
            {
                double pozt_x = canvas_chart.Margin.Left;
                double pozt_y = canvas_chart.Margin.Top;
                canvas_chart.Margin = new Thickness(pozt_x + razn_width / 2, pozt_y + razn_height / 2, 0, 0);
            }
        }

        private delegate void EmptyDelegate();//для того чтобы кнопки были активны во время выполения цикла
        public static void DoEvents() //для того чтобы кнопки были активны и не зависала программа во время выполения цикла
        {
            Dispatcher.CurrentDispatcher.Invoke(DispatcherPriority.Background, new EmptyDelegate(delegate { }));
        }

        //NumberFormatInfo nfi = new CultureInfo("en-US", false).NumberFormat;        
        BrushConverter bc = new BrushConverter();
        DispatcherTimer timer = null;//1-ый таймер для отправки дискретных запросов на сервер для получения от него реалтайм-данных(создаётся в Window_Loaded_1, запускается в Click_12)                
        double st_frm_height = 0, st_frm_width = 0;//размеры окна, определяются в Window_Loaded_1 и Window_SizeChanged
        BackgroundWorker bgw_l;
        ContextMenu BorderChartContextMenu = new ContextMenu();
        MenuItem Load_BorderChartContextMenu = new MenuItem();//кнопка Load в контекстном меню

        private void Window_Loaded_1(object sender, RoutedEventArgs e)
        {
            //nfi.NumberGroupSeparator = "'";
            lock(Vars.main_windows)
                Vars.main_windows.Add(this);

            st_frm_height = Height;//определяем размеры главного окна
            st_frm_width = Width;

            lock(Vars.InstrumentsInfoDictionary)
                foreach (string key in Vars.InstrumentsInfoDictionary.Keys)
                {
                    ComboBoxItem item = new ComboBoxItem();
                    item.Content = key;
                    item.Background = (Brush)bc.ConvertFrom("#FF8C7AE0");
                    comboBox1.Items.Add(item);
                }

            comboBox1.SelectedIndex = 16;//"USDT-BTC"

            timer = new DispatcherTimer();
            timer.Interval = new TimeSpan(0, 0, 0, 1);
            timer.Tick += new EventHandler(send_to_server);//при каждой сработке таймера будет вызываться ф-ция send_to_server для отправки на сервер нового запроса на получение от него следующей порции релтаймданных

            CreatePriceCursor();//линия-указатель текущей цены

            //задаём постоянные параметры для лейбла, выплывающего возле вертикальной гистограммы на активной линии
            vertical_gstgrm_vsbl_lbl = new Label();
            vertical_gstgrm_vsbl_lbl.Padding = new Thickness(5, 0, 0, 0);
            vertical_gstgrm_vsbl_lbl.Background = Brushes.Black;
            vertical_gstgrm_vsbl_lbl.BorderThickness = new Thickness(1, 1, 1, 1);
            vertical_gstgrm_vsbl_lbl.BorderBrush = (Brush)bc.ConvertFrom("#FF00FF40");
            vertical_gstgrm_vsbl_lbl.Height = 20;
            vertical_gstgrm_vsbl_lbl.Foreground = (Brush)bc.ConvertFrom("#FF00FF40");

            //задаём постоянные параметры для лейбла, выплывающего возле горизонтальной гистограммы на активной линии
            horisontalal_gstgrm_vsbl_lbl = new Label();
            horisontalal_gstgrm_vsbl_lbl.Padding = new Thickness(5, 0, 0, 0);
            horisontalal_gstgrm_vsbl_lbl.Background = Brushes.Black;
            horisontalal_gstgrm_vsbl_lbl.BorderThickness = new Thickness(1, 1, 1, 1);
            horisontalal_gstgrm_vsbl_lbl.BorderBrush = (Brush)bc.ConvertFrom("#FF00FF40");
            horisontalal_gstgrm_vsbl_lbl.Height = 20;
            Canvas.SetZIndex(horisontalal_gstgrm_vsbl_lbl, 2);
            horisontalal_gstgrm_vsbl_lbl.Foreground = (Brush)bc.ConvertFrom("#FF00FF40");

            CreateOpenCloseLabel();

            bgw_l = new BackgroundWorker();
            bgw_l.WorkerSupportsCancellation = true;
            bgw_l.DoWork += ReadHistory;
            bgw_l.RunWorkerCompleted += RunWorkerCompleted;
            /*BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += (obj, e) => ClientThreadProcess(client, threadIndex, ipAddress);
            bw.RunWorkerCompleted += (obj, e) => RWComplette(threadIndex);*/

            CreateContextMenu();

            datePicker1.SelectedDate = Vars.ServerDate.Value.Date;
            datePicker2.SelectedDate = Vars.ServerDate.Value.Date;

            if (Vars.IsOnConnection)
            {
                Load_сhart(null, null);
            }
            else
                this.Background = Brushes.Red;

            //button12.IsEnabled = false;
            //Histogramms_setting.IsEnabled = false;
            //Filters_setting.IsEnabled = false;
        }

        private void CreateContextMenu()
        {
            BorderChartContextMenu.Items.Add(Load_BorderChartContextMenu);
            //Load_BorderChartContextMenu.IsEnabled = false;
            Load_BorderChartContextMenu.Header = "Load chart";
            Load_BorderChartContextMenu.Background = (Brush)bc.ConvertFrom("#FF8C7AE0");
            Load_BorderChartContextMenu.Click += Load_сhart;

            border_chart.ContextMenu = BorderChartContextMenu;
            border_chart.ContextMenuOpening += new ContextMenuEventHandler(Opening_Context_Menu);//этот метод вызывается перед появлением контекстного меню
        }

        #endregion

        #region Select Instrument

        string instrument = "";
        double price_step;//шаг цены  инструмента 
        bool isEventBusy = false;//указывает что в данный момент изменяем выбранный инструмент(используется в ф-ции comboBox2_SelectionChanged, чтобы там раньше времени не вызывался Click_12)
        DateTime? start_history;//читается из ф-ла инфо, использ в Check_Options
        int[] transfrm_steps;//Определ в Change_contracts, испол в Mod_Bars_У, Apdate_Y_1, Create_Price_Axis
        int[] fltrss = new int[42];//сюда считываем фильтра из тхт-ф-ла
        int rzrdn;//к-во знаков после запятой в price_step

        private void Select_Instrument(object sender, SelectionChangedEventArgs e)//активируется при изменении комбобокса-инструмент
        {
            int index_1 = comboBox1.SelectedIndex;//определяем номер выбранного пользователем обьекта в комбобоксе1
            if (index_1 == -1) return;

            isEventBusy = true;
            Reset_UI_elsements();
            lines_X.Clear();
            Save_settings();
            re_posit = false;
            ComboBoxItem item_instr = comboBox1.Items[index_1] as ComboBoxItem;//читаем выбранный обьект из комбобокса1
            instrument = item_instr.Content.ToString();//конвертируем  выбранный обьект в текст - это назван инструм
            lock (Vars.InstrumentsInfoDictionary)
                price_step = Vars.InstrumentsInfoDictionary[instrument][1];//шаг цены, прочитанный из ф-ла info
            string pr_st = ((decimal)price_step).ToString(Vars.FormatInfo);
            string[] poslezpt = pr_st.Split('.');
            if (poslezpt.Length > 1)
                rzrdn = poslezpt[1].Length;
            else rzrdn = 0;
            
            start_history = new DateTime(2018, 1, 13);//DateTime.ParseExact(info[9], "yyyy.MM.dd", CultureInfo.InvariantCulture);
            set_DisplayDateStart_End();
            if (price_step == 0.25 || price_step == 0.0078125)
                transfrm_steps = new int[] { 4, 8, 20, 40, 80, 200, 400 };//,800
            else if (price_step == 0.5 || price_step == 0.05 || price_step == 0.005 || price_step == 5 || price_step == 0.03125 || price_step == 0.015625)
                transfrm_steps = new int[] { 4, 10, 20, 40, 100, 200, 400 };//,1000
            else//price_step =  0.1, 0.01, 0.001, 0.0001, 1, 10 
                transfrm_steps = new int[] { 5, 10, 20, 50, 100, 200, 500 };//,1000

            ReadLineFile();//читаем линии
            Read_filters_file();//читаем фильтры
            set_volume_filters();
            Check_Options();

            isEventBusy = false;
        }

        private void set_DisplayDateStart_End()
        {
            datePicker1.DisplayDateStart = start_history;
            datePicker2.DisplayDateStart = start_history;
            datePicker1.DisplayDateEnd = null;
            datePicker2.DisplayDateEnd = null;
            if (datePicker1.SelectedDate != null && start_history != null && datePicker1.SelectedDate < start_history)
                datePicker1.SelectedDate = start_history;
            if (datePicker2.SelectedDate != null && start_history != null && datePicker2.SelectedDate < start_history)
                datePicker2.SelectedDate = start_history;
        }

        #endregion

        #region Select Interval
        
        double interval = 5;
        int otstup = 20;//otstup чтобы двоеточие во временном лейбле было по центру бара
        double distance_between_visible_time_labels = 101;

        private void Click_5(object sender, RoutedEventArgs e)
        {
            if (interval != 1)
            {
                button5.Background = (Brush)bc.ConvertFrom("#FF2323BD");
                button5.Foreground = Brushes.Red;
                button6.Background = (Brush)bc.ConvertFrom("#FF009AA0");
                button7.Background = (Brush)bc.ConvertFrom("#FF009AA0");
                button8.Background = (Brush)bc.ConvertFrom("#FF009AA0");
                button9.Background = (Brush)bc.ConvertFrom("#FF009AA0");
                button10.Background = (Brush)bc.ConvertFrom("#FF009AA0");
                button11.Background = (Brush)bc.ConvertFrom("#FF009AA0");
                button6.Foreground = Brushes.Black;
                button7.Foreground = Brushes.Black;
                button8.Foreground = Brushes.Black;
                button9.Foreground = Brushes.Black;
                button10.Foreground = Brushes.Black;
                button11.Foreground = Brushes.Black;

                interval = 1;
                Select_Interval_Common_Actions(1);

                if (chek_opt == true && !button_press)
                    Load_сhart(null, null);
            }
        }

        private void Click_6(object sender, RoutedEventArgs e)
        {
            if (interval != 5)
            {
                button6.Background = (Brush)bc.ConvertFrom("#FF2323BD");
                button6.Foreground = Brushes.Red;
                button5.Background = (Brush)bc.ConvertFrom("#FF009AA0");
                button7.Background = (Brush)bc.ConvertFrom("#FF009AA0");
                button8.Background = (Brush)bc.ConvertFrom("#FF009AA0");
                button9.Background = (Brush)bc.ConvertFrom("#FF009AA0");
                button10.Background = (Brush)bc.ConvertFrom("#FF009AA0");
                button11.Background = (Brush)bc.ConvertFrom("#FF009AA0");
                button5.Foreground = Brushes.Black;
                button7.Foreground = Brushes.Black;
                button8.Foreground = Brushes.Black;
                button9.Foreground = Brushes.Black;
                button10.Foreground = Brushes.Black;
                button11.Foreground = Brushes.Black;

                interval = 5;
                Select_Interval_Common_Actions(5);

                if (chek_opt == true && !button_press)
                    Load_сhart(null, null);
            }
        }

        private void Click_7(object sender, RoutedEventArgs e)
        {
            if (interval != 240)
            {
                button7.Background = (Brush)bc.ConvertFrom("#FF2323BD");
                button7.Foreground = Brushes.Red;
                button5.Background = (Brush)bc.ConvertFrom("#FF009AA0");
                button6.Background = (Brush)bc.ConvertFrom("#FF009AA0");
                button8.Background = (Brush)bc.ConvertFrom("#FF009AA0");
                button9.Background = (Brush)bc.ConvertFrom("#FF009AA0");
                button10.Background = (Brush)bc.ConvertFrom("#FF009AA0");
                button11.Background = (Brush)bc.ConvertFrom("#FF009AA0");
                button5.Foreground = Brushes.Black;
                button6.Foreground = Brushes.Black;
                button8.Foreground = Brushes.Black;
                button9.Foreground = Brushes.Black;
                button10.Foreground = Brushes.Black;
                button11.Foreground = Brushes.Black;

                interval = 240;
                Select_Interval_Common_Actions(240);

                if (chek_opt == true && !button_press)
                    Load_сhart(null, null);
            }
        }

        private void Click_8(object sender, RoutedEventArgs e)
        {
            if (interval != 15)
            {
                button8.Background = (Brush)bc.ConvertFrom("#FF2323BD");
                button8.Foreground = Brushes.Red;
                button5.Background = (Brush)bc.ConvertFrom("#FF009AA0");
                button6.Background = (Brush)bc.ConvertFrom("#FF009AA0");
                button7.Background = (Brush)bc.ConvertFrom("#FF009AA0");
                button9.Background = (Brush)bc.ConvertFrom("#FF009AA0");
                button10.Background = (Brush)bc.ConvertFrom("#FF009AA0");
                button11.Background = (Brush)bc.ConvertFrom("#FF009AA0");
                button5.Foreground = Brushes.Black;
                button6.Foreground = Brushes.Black;
                button7.Foreground = Brushes.Black;
                button9.Foreground = Brushes.Black;
                button10.Foreground = Brushes.Black;
                button11.Foreground = Brushes.Black;

                interval = 15;
                Select_Interval_Common_Actions(15);

                if (chek_opt == true && !button_press)
                    Load_сhart(null, null);
            }
        }

        private void Click_9(object sender, RoutedEventArgs e)
        {
            if (interval != 30)
            {
                button9.Background = (Brush)bc.ConvertFrom("#FF2323BD");
                button9.Foreground = Brushes.Red;
                button5.Background = (Brush)bc.ConvertFrom("#FF009AA0");
                button6.Background = (Brush)bc.ConvertFrom("#FF009AA0");
                button7.Background = (Brush)bc.ConvertFrom("#FF009AA0");
                button8.Background = (Brush)bc.ConvertFrom("#FF009AA0");
                button10.Background = (Brush)bc.ConvertFrom("#FF009AA0");
                button11.Background = (Brush)bc.ConvertFrom("#FF009AA0");
                button5.Foreground = Brushes.Black;
                button6.Foreground = Brushes.Black;
                button7.Foreground = Brushes.Black;
                button8.Foreground = Brushes.Black;
                button10.Foreground = Brushes.Black;
                button11.Foreground = Brushes.Black;
               
                interval = 30;
                Select_Interval_Common_Actions(30);

                if (chek_opt == true && !button_press)
                    Load_сhart(null, null);
            }
        }

        private void Click_10(object sender, RoutedEventArgs e)
        {
            if (interval != 60)
            {
                button10.Background = (Brush)bc.ConvertFrom("#FF2323BD");
                button10.Foreground = Brushes.Red;
                button5.Background = (Brush)bc.ConvertFrom("#FF009AA0");
                button6.Background = (Brush)bc.ConvertFrom("#FF009AA0");
                button7.Background = (Brush)bc.ConvertFrom("#FF009AA0");
                button8.Background = (Brush)bc.ConvertFrom("#FF009AA0");
                button9.Background = (Brush)bc.ConvertFrom("#FF009AA0");
                button11.Background = (Brush)bc.ConvertFrom("#FF009AA0");
                button5.Foreground = Brushes.Black;
                button6.Foreground = Brushes.Black;
                button7.Foreground = Brushes.Black;
                button8.Foreground = Brushes.Black;
                button9.Foreground = Brushes.Black;
                button11.Foreground = Brushes.Black;


                Select_Interval_Common_Actions(60);
                interval = 60;

                if (chek_opt == true && !button_press)
                    Load_сhart(null, null);
            }
        }

        private void Click_11(object sender, RoutedEventArgs e)
        {
            if (interval != 1440)
            {
                button11.Background = (Brush)bc.ConvertFrom("#FF2323BD");
                button11.Foreground = Brushes.Red;
                button5.Background = (Brush)bc.ConvertFrom("#FF009AA0");
                button6.Background = (Brush)bc.ConvertFrom("#FF009AA0");
                button7.Background = (Brush)bc.ConvertFrom("#FF009AA0");
                button8.Background = (Brush)bc.ConvertFrom("#FF009AA0");
                button9.Background = (Brush)bc.ConvertFrom("#FF009AA0");
                button10.Background = (Brush)bc.ConvertFrom("#FF009AA0");
                button5.Foreground = Brushes.Black;
                button6.Foreground = Brushes.Black;
                button7.Foreground = Brushes.Black;
                button8.Foreground = Brushes.Black;
                button9.Foreground = Brushes.Black;
                button10.Foreground = Brushes.Black;
                interval = 1440;
                Select_Interval_Common_Actions(1440);
                if (chek_opt == true && !button_press)
                    Load_сhart(null, null);
            }

        }

        private void Select_Interval_Common_Actions(double intrvl)
        {
            Reset_UI_elsements();
            set_volume_filters();
            if (intrvl == 1440)
                distance_between_visible_time_labels = 51;
            else distance_between_visible_time_labels = 101;
        }

        #endregion

        #region Change Comboboxes

        DateTime? start_date, end_date;
        bool chek_opt = true;//можно или нельзя запускать Click_12, активна или нет кнопка Draw chart

        private void Check_Options()
        {
            start_date = datePicker1.SelectedDate;
            end_date = datePicker2.SelectedDate;//по end_date будем рисовать график в реалтайме 
            if(start_date == null && end_date == null)
                start_date = 
                    end_date = 
                        datePicker1.SelectedDate =
                            datePicker2.SelectedDate = Vars.ServerDate;

            if (start_date.Value.Date > Vars.ServerDate.Value.Date)
                start_date = Vars.ServerDate.Value.Date;

            if (end_date < start_date)
                end_date = start_date;

            if (instrument == "")
                instrument = "USDT-BTC";
        }

        private void datePicker1_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            Reset_UI_elsements();

            if (datePicker1.SelectedDate != null)
            {
                if (datePicker1.DisplayDateStart != null && datePicker1.SelectedDate < datePicker1.DisplayDateStart)
                    datePicker1.SelectedDate = datePicker1.DisplayDateStart;
                else if (start_history != null && datePicker1.SelectedDate < start_history)
                    datePicker1.SelectedDate = start_history;
                else if (datePicker1.DisplayDateEnd != null && datePicker1.SelectedDate > datePicker1.DisplayDateEnd)
                    datePicker1.SelectedDate = datePicker1.DisplayDateEnd;
                else if (datePicker1.SelectedDate.Value.Date > Vars.ServerDate.Value.Date.AddDays(1))//если послезавтра и дальше
                    datePicker1.SelectedDate = Vars.ServerDate.Value.Date.AddDays(1);//то ставим завтрашн дату

                if (datePicker2.SelectedDate == null || datePicker1.SelectedDate > datePicker2.SelectedDate)
                datePicker2.SelectedDate = datePicker1.SelectedDate;
            }

            Check_Options();// чтобы задать start_date_1 и end_date
        }

        private void datePicker2_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            Reset_UI_elsements();

            if (datePicker2.SelectedDate != null)
            {
                if (datePicker2.DisplayDateStart != null && datePicker2.SelectedDate < datePicker2.DisplayDateStart)
                    datePicker2.SelectedDate = datePicker2.DisplayDateStart;
                else if (start_history != null && datePicker2.SelectedDate < start_history)
                    datePicker2.SelectedDate = start_history;
                else if (datePicker2.DisplayDateEnd != null && datePicker2.SelectedDate > datePicker2.DisplayDateEnd)
                    datePicker2.SelectedDate = datePicker2.DisplayDateEnd;

                if (datePicker1.SelectedDate == null || datePicker2.SelectedDate < datePicker1.SelectedDate)
                {
                    if (datePicker2.SelectedDate <= Vars.ServerDate.Value.Date.AddDays(1))
                        datePicker1.SelectedDate = datePicker2.SelectedDate;
                    else
                        datePicker1.SelectedDate = Vars.ServerDate.Value.Date.AddDays(1);
                }
            }

            Check_Options();// чтобы задать start_date_1 и end_date
        }

        private void Clear_canvases()
        {
            canvas_chart.Children.Clear();//очищаем график
            canvas_prices.Children.Clear();//и вертикальн шкалу цен
            canvas_times.Children.Clear();//и временную шкалу  
            LinesCanvas.Children.Clear();
            canvas_vertical_line.Children.Clear();
            canvas_gistogramm.Children.Clear();
            canvas_for_label.Children.Clear();
            ClearVerticalHistogamm();
        }

        private void Reset_UI_elsements()
        {
            hstr.stop_load = true;
            if (timer != null) timer.Stop();
            if (label7 != null) label7.Content = "";
            if (button12 != null) button12.Content = "Load chart";
            Load_BorderChartContextMenu.Header = "Load chart";
            if (First_visibl_date_label != null)
                First_visibl_date_label.Content = "";

            if (canvas_chart != null && canvas_prices != null && canvas_times != null && LinesCanvas != null)
                Clear_canvases();
        }

        private void TodayContextMenu(object sender, RoutedEventArgs e)
        {
            if (datePicker1.DisplayDateEnd == null || Vars.ServerDate.Value.Date <= datePicker1.DisplayDateEnd)
            {
                    datePicker1.SelectedDate = Vars.ServerDate.Value.Date;
            }
            else
                datePicker1.SelectedDate = datePicker1.DisplayDateEnd;
        }

        private void TodayContextMenu_EndCalendar(object sender, RoutedEventArgs e)
        {
            if (datePicker2.DisplayDateEnd == null || Vars.ServerDate.Value.Date <= datePicker2.DisplayDateEnd.Value.Date)
            {
                    datePicker2.SelectedDate = Vars.ServerDate.Value.Date;
            }
            else
                datePicker2.SelectedDate = datePicker2.DisplayDateEnd;
        }

        private void PriceStepTextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                
                string filter1 = PriceStepTextBox.Text;
                int frt = PriceStepTextBox.SelectionStart;
                string filter2 = filter1;

                if (filter2.Length > 15) filter2 = filter2.Substring(0, 15);
                bool isCintainsKoma = false;
                for (int x = 0; x < filter2.Length; x++)
                {
                    char chr = (char)filter2[x];
                    if (Char.IsDigit(chr) != true)
                    {
                        if (chr == '.' || chr == ',')
                        {
                            if (isCintainsKoma)
                            {
                                filter2 = filter2.Substring(0, x) + filter2.Substring(x + 1, filter2.Length - x - 1);
                                x--;
                            }

                            isCintainsKoma = true;
                        }
                        else
                        {
                            filter2 = filter2.Substring(0, x) + filter2.Substring(x + 1, filter2.Length - x - 1);
                            x--;
                        }
                    }
                }

                if (filter2 != filter1)
                {
                    PriceStepTextBox.Text = filter2;
                    PriceStepTextBox.SelectionStart = frt - (filter1.Length - filter2.Length);
                }
            }
            catch { }
        }

        #endregion

        #region Load Chart
        int volumeRazrdn = 0;
        double max_width_bar;//мах допустимая ширина бара. зависит от к-ва цыфр в мах об-ме во всём графике
        int N_fb, N_lb;//номера первого и последнего баров, видимых в бордере и номера первого и последнего баров добавленных в канвас-график
        int N_visibl_prc_label = 0;//номер ячейки в массиве transfrm_steps, испол в Mod_Bars_У, Create_Price_Axis, Apdate_У_1
        double chart_mas_y = 12;//переменн - высота 1-го тика в пикселях, испол в ф-циях Draw_Cluster, Create_Price_Axis, и Mod_Bars_У, Click_12, Chart_apdate, возвращается в исходн полож в Click_12
        double bufer_x = -1;//расстояние му барами, испол в ф-циях Apdate_X_1 и Mod_Bars_Х, возвращается в исходн полож в Click_12
        bool pozizionir = false;//нужно или нет позиционировать график. испол в Mod_Bars_Х_1, Click_12, UpdateXY_1, Chart_apdate, Pozitionir_2, Window_SizeChanged
        bool button_press = false; //идентифицирует что идёт выполнение Click_12
        bool re_posit = false;//указывает изменился ли только интервал или и какой-нить комбобокс тоже изменили перед Click_12. если тока интервал то расположение графика не меняем при перезагрузке графика        
        DateTime? reposition_date = null;
        int cratnost_time_labels;//при interval =  1440 временные лейблы, номера ячеек которых кратны cratnost_time_labels добавляются во временную шкалу
        double bars_max_volume;
        bool isClusterChart = true;
        FileReader hstr = new FileReader();
        Tick summary_t = null;
        
        //real-time global vars
        public bool IsWorkRealTime = false;
        int _realTimeTickIndex = 0;
        public string ServerQuery ="";
        DateTime _lastHistoryTickDateTime;

        private void Load_сhart(object sender, RoutedEventArgs e)
        {
            hstr.stop_load = true;
            timer.Stop();

            if (button_press)
            {
                bgw_l.CancelAsync();
                button_press = false;
                return;
            }

            button_press = true;
            Clear_canvases();//должно быть до DoEvents чтобы было видно что очистились канвасы
            MenuSettings.IsEnabled = false;
            button12.Content = "Stop loading";
            label7.Content = "Loading...";
            Load_BorderChartContextMenu.Header = "Stop loading";
            DoEvents();

            DateTime? endHistoryDate;//по эту дату будем скачивать историю

            #region activate realtime

            if (end_date.Value.Date >= Vars.ServerDate.Value.Date)
            {
                end_date = new DateTime(end_date.Value.Year, end_date.Value.Month, end_date.Value.Day, 23, 59, 59); //должно быть до активации реалтайма
                endHistoryDate = Vars.ServerDate.Value.Date;
                if (start_date > endHistoryDate)
                {
                    start_date = endHistoryDate;
                    datePicker1.SelectedDate = start_date;
                }
                
                string serverQueryBuffer = ServerQuery;
                ServerQuery = instrument;
                lock (Vars.ServerQueries)
                {
                    if(serverQueryBuffer != ServerQuery)
                        if (Vars.ServerQueries.ContainsKey(serverQueryBuffer))
                            lock (Vars.main_windows)
                                if (!Vars.main_windows.Any(mw => mw.IsWorkRealTime && mw.ServerQuery == serverQueryBuffer))
                                    Vars.ServerQueries.Remove(serverQueryBuffer);

                    if (!Vars.ServerQueries.ContainsKey(ServerQuery))
                        Vars.ServerQueries.Add(ServerQuery, new ServerData { LastTickIndex = 0, TicksList = new List<Tick>() });
                }

                IsWorkRealTime = true;
            }
            else
            {
                IsWorkRealTime = false;
                endHistoryDate = end_date;
                lock (Vars.ServerQueries)
                    if (Vars.ServerQueries.ContainsKey(ServerQuery))
                        lock (Vars.main_windows)
                            if (!Vars.main_windows.Any(mw => mw.IsWorkRealTime && mw.ServerQuery == ServerQuery))
                                Vars.ServerQueries.Remove(ServerQuery);
            }

            #endregion

            if (re_posit && !pozizionir && price_canvases.Count > 0 && chart.Count > 0)
            {
                indx = Calculate_Vertical_Central_Cell();
                hiet = price_canvases[indx].price;
                int central_bar_number = Calculate_Horizontal_Central_Cell();
                reposition_date = chart[central_bar_number].time_of_bar;
            }

            #region обнуляем глобальные переменные, очищаем массивы
            
            isEventBusy = false;
            color_choice = false;
            isLineDeleted = false;
            cross_move = false;
            isMove = false;
            was_move = false;
            mas_Y = false;
            mas_X = false;
            GorizontalActivLine = null;
            VerticalActivLine = null;
            bars_max_volume = 0;
            volumeRazrdn = 0;
            N_lb = -1;
            summary_t = null;
            data_for_tick_chart_horisontal_histogram.Clear();//это нелзя очищать из vygruz_transitov()
            MarcetProfile.Clear();
            price_canvases.Clear();
            chart.Clear();//должно быть подальше от button_press = true чтобы в DrawChart() было время для остановки второго цыкла for (int i = strt - 1; i >= 0; i--) где создаются не видимые в бордере бары
            _lastHistoryTickDateTime = new DateTime();
            _realTimeTickIndex = 0;
            #endregion

            Set_vertical_histogram_type_buffer();
            hstr.stop_load = false;

            bgw_l.RunWorkerAsync(endHistoryDate);
        }

        private void Set_vertical_histogram_type_buffer()
        {
            if (vertical_gist)
            {
                vertical_histogram_type_buffer = vertical_histogram_type;
            }
        }

        private void Return_Click_12()
        {
            if (button12.Content.ToString() == "Stop loading")//т.е. не "Load chart", т.е. остановка не потому что изменили к.л. комбобокс, а потому что нажали "Stop loading" или из-за невозможности подобрать контракт автоматически или не получается скачать историю с сервера
            {
                button12.Content = "Reload chart";
                Load_BorderChartContextMenu.Header = "Reload chart";
            }

            button_press = false;
            MenuSettings.IsEnabled = true;
        }
        
        private void ReadHistory(object sender, DoWorkEventArgs e)
        {
            DateTime? curentFileDate = start_date;//т.к. start_date_1 ещё используется в AutoDetermineVolumeFilters()
            DateTime? endHistoryDate = (DateTime?)e.Argument;
            List<Tick> Ticks = new List<Tick>(10000);
            List<MinuteBar> Minutess = new List<MinuteBar>();
            while (curentFileDate <= endHistoryDate)
            {

                double lastTickID = hstr.ReadFile(Ticks, Minutess, instrument, curentFileDate, out _lastHistoryTickDateTime, price_step, rzrdn);
                if (hstr.stop_load)//if(bgw_l.CancellationPending)//
                    break;//e.Cancel = true;

                if (curentFileDate == endHistoryDate && IsWorkRealTime)
                    UnionHistoryRealTime(Ticks, lastTickID, curentFileDate);

                if (Ticks.Count > 0)
                {
                    summary_t = Ticks.Last();
                    Bike(Ticks);
                }
                else if (Minutess.Count > 0)
                    Bike_minutka(Minutess);

                Ticks.Clear();
                Minutess.Clear();
                if (hstr.stop_load)//if(bgw_l.CancellationPending)
                    return;//e.Cancel = true;

                curentFileDate = curentFileDate.Value.AddDays(1);
            }
        }

        private void UnionHistoryRealTime(List<Tick> Ticks, double lastTickID, DateTime? curentFileDate)
        {
            List<Tick> realTimeTicksList = null;
            lock (Vars.ServerQueries)
                if (Vars.ServerQueries.ContainsKey(ServerQuery))
                    realTimeTicksList = new List<Tick>(Vars.ServerQueries[ServerQuery].TicksList);

            if (realTimeTicksList != null && realTimeTicksList.Count > 0)
            {
                if (Ticks.Count > 0)//если за сегодняшний день закачался и исторический файл и реалтайм тики, то и там и там должен быть одинаковый summaryTick, в историческом массиве Ticks он последний, а в реалтаймовском массиве Vars.TicksList - он или первый или уже глубже
                {
                    if (lastTickID < realTimeTicksList[0].id)//если такого тика нет, то делаем перезакачку исторических тиков, т.к. на графике будет дырка между историей и реалтаймом
                    {
                        Ticks.Clear();
                        lastTickID = hstr.ReadFile(Ticks, null, instrument, curentFileDate, out _lastHistoryTickDateTime, price_step, rzrdn);
                        if (Ticks.Count == 0)//если только что исторические тики были, а после перезакачки их не стало, то делаем ещё одну закачку
                            lastTickID = hstr.ReadFile(Ticks, null, instrument, curentFileDate, out _lastHistoryTickDateTime, price_step, rzrdn);
                    }

                    if (Ticks.Count > 0)
                    {
                        int index = realTimeTicksList.FindIndex(tk => tk.id == lastTickID && tk.date.Value == _lastHistoryTickDateTime);
                        if (index >= 0)
                            _realTimeTickIndex = index + 1;
                        else//будет дырка между историей и реалтаймом - прийдётся сделать Reload
                        {
                            _realTimeTickIndex = realTimeTicksList.FindIndex(tk => tk.date.Value > _lastHistoryTickDateTime);
                            if (_realTimeTickIndex == -1)//т.к. на сервере id не обнуляется во время клиринга, а тока по воскр, то это условие никогда не сбудется
                                _realTimeTickIndex = realTimeTicksList.Count;//не может у всех реалтайм тиков id быть меньшим чем у последнего исторического тика
                        }
                    }
                }

                Ticks.AddRange(GetRealTimeTicks());
            }
        }

        private List<Tick> GetRealTimeTicks()
        {
            List<Tick> newTicks, newRealTimeTicks = new List<Tick>();
            lock (Vars.ServerQueries)
            {
                if (Vars.ServerQueries.ContainsKey(ServerQuery))
                {
                    if (Vars.ServerQueries[ServerQuery].TicksList.Count == _realTimeTickIndex)
                        return newRealTimeTicks;
                    else if (Vars.ServerQueries[ServerQuery].TicksList.Count < _realTimeTickIndex)//если в классе RealTimeLoader массив Vars.TicksList[_symbolRealTime] очистился
                        _realTimeTickIndex = 0;

                    int ticksListCount = Vars.ServerQueries[ServerQuery].TicksList.Count;
                    newTicks = Vars.ServerQueries[ServerQuery].TicksList.GetRange(_realTimeTickIndex, ticksListCount - _realTimeTickIndex);
                    _realTimeTickIndex = ticksListCount;
                }
                else return newRealTimeTicks;
            }

            foreach(Tick tk in newTicks)
            {
                if(tk.date > end_date)
                {
                    end_date = new DateTime(tk.date.Value.Year, tk.date.Value.Month, tk.date.Value.Day, 23, 59, 59);
                    datePicker2.SelectedDate = end_date;
                }

                if (tk.date.Value < _lastHistoryTickDateTime)//если комп вывели из спящего режима и нажали Load, то подгружаемые реалТайм тики могут быть раньше по времени чем прочитанные из истории
                    continue;
                
                Tick newTick = new Tick
                {
                    date = tk.date,
                    originalPrice = tk.originalPrice,
                    price = hstr.RoundPriceStep(instrument, tk.originalPrice, price_step, rzrdn),
                    volume = tk.volume,
                    side = tk.side,
                    id = tk.id
                };

                newRealTimeTicks.Add(newTick);
            }

            return newRealTimeTicks;
        }
        
        private void Bike_minutka(List<MinuteBar> minutes)
        {
                int s = 0;
                DateTime? initial_interv;
                if (chart.Count > 0)
                    initial_interv = chart.Last().time_of_bar.Value.AddMinutes(interval);
                else
                    initial_interv = set_init_interv(interval, minutes[0].date);

                start:

                if (hstr.stop_load) return;
                DateTime? initial_next_interval = initial_interv.Value.AddMinutes(interval);
                int f = minutes.FindIndex(s, delegate (MinuteBar tick) { return tick.date >= initial_next_interval; });//в тиковом массиве находим первый ближайший тик со след интервала, т.е. кот вишел за пределы текущего интервала
                if (f < 0) f = minutes.Count;//если такой тик не нашолся, значит массив уже весь обработан и след интервала уже не будет
                if (f > s)
                {
                    List<MinuteBar> interv_t = minutes.GetRange(s, f - s);//отбираем текущий интервал в отдельный массив
                    double price_open_bar = interv_t[0].price_open;
                    double price_close_bar = interv_t.Last().price_close;
                    int color = -6;//определяем цвет бара
                    if (price_open_bar > price_close_bar)
                        color = -5;

                    List<Cluster> bar = new List<Cluster>();
                    IEnumerable<IGrouping<double, Cluster>> gr_intrv = interv_t.SelectMany(mnts => mnts.minutka).GroupBy(it => it.price, it => it);

                    foreach (IGrouping<double, Cluster> group in gr_intrv)
                    {
                        double rating = group.Sum(gr => gr.volume);
                        double buys = group.Sum(gr => gr.buy);
                        double sells = group.Sum(gr => gr.sell);
                        bar.Add(new Cluster { price = group.Key, volume = rating, buy = buys, sell = sells });
                    }

                    bar.Sort(delegate (Cluster x, Cluster y) { return x.price.CompareTo(y.price); });//сортировка по цене
                    double max_prc = bar.Last().price;
                    double min_prc = bar[0].price;
                    double height_tick_count = Calculate_BarHeightPips(max_prc, min_prc);
                    CreateChartBar(initial_interv, bar, color, price_open_bar, price_close_bar, max_prc, min_prc, height_tick_count, chart);
                }
                else
                    set_gap(initial_interv);

                if (f < minutes.Count)
                {
                    initial_interv = initial_next_interval;
                    s = f;
                    goto start;//след итерация цыкла
                }
        }

        private void Bike(List<Tick> Ticki)
        {
            int s = 0;
            DateTime? initial_interv;
            if (chart.Count > 0)
                initial_interv = chart.Last().time_of_bar.Value.AddMinutes(interval);
            else
                initial_interv = set_init_interv(interval, Ticki[0].date);

            start:

            if (hstr.stop_load) return;//если нажимали StopLoading
            DateTime? initial_next_interval = initial_interv.Value.AddMinutes(interval);//определ начало следующ интервала
            int f = Ticki.FindIndex(s, delegate(Tick tick) { return tick.date >= initial_next_interval; });//в тиковом массиве находим первый ближайший тик со след интервала, т.е. кот вишел за пределы текущего интервала
            if (f < 0) f = Ticki.Count;//если такой тик не нашолся, значит в этой итерации массив уже весь будет обработан и след интервала уже не будет
            if (f > s)
            {
                List<Tick> interv_t = Ticki.GetRange(s, f - s);//отбираем текущий интервал в отдельный массив
                
                double price_open_bar = interv_t[0].price;
                double price_close_bar = interv_t.Last().price;
                int color = -5;//определяем цвет бара
                if (price_open_bar <= price_close_bar)
                    color = -6;

                List<Cluster> bar = CalculateBarPriceRating(interv_t);
                double max_prc = bar.Last().price;
                double min_prc = bar[0].price;
                double height_tick_count = Calculate_BarHeightPips(max_prc, min_prc);
                CreateChartBar(initial_interv, bar, color, price_open_bar, price_close_bar, max_prc, min_prc, height_tick_count, chart);
            }
            else
                set_gap(initial_interv);

            if (f < Ticki.Count)//если ещё не весь тиковый массив обработали то уходим на следующ итерацию цыкла
            {
                initial_interv = initial_next_interval;
                s = f;
                goto start;
            }
        }

        private List<Cluster> CalculateBarPriceRating(List<Tick> interv_t)
        {
            List<Cluster> bar = new List<Cluster>();
            IEnumerable<IGrouping<double, Tick>> gr_intrv = interv_t.GroupBy(it => it.price, it => it);
            foreach (IGrouping<double, Tick> group in gr_intrv)//просчитываем обьёмы внутри бара-кластера на всех ценах
            {
                bar.Add(new Cluster
                {
                    price = group.Key,
                    volume = group.Sum(gr => gr.volume),
                    buy = group.Where(tk => tk.side == 1).Sum(tk=>tk.volume),
                    sell = group.Where(tk => tk.side == -1).Sum(tk => tk.volume)
                });
            }

            bar.Sort(delegate(Cluster x, Cluster y) { return x.price.CompareTo(y.price); });//сортировка по цене

            return bar;
        }

        private double Calculate_BarHeightPips(double max_prc, double min_prc)
        {
            return Vars.MathRound((max_prc - min_prc) / price_step + 1);
        }

        private DateTime? set_init_interv(double intrvl, DateTime? Start_tick)
        {
            int hour_init_interv = Start_tick.Value.Hour;
            int day_init_interv = Start_tick.Value.Day;
            int month_init_interv = Start_tick.Value.Month;
            int year_init_interv = Start_tick.Value.Year;
            int sec = 0;
            int min_init_interv = Start_tick.Value.Minute;

            if (intrvl > 1 && intrvl < 60)
            {
                min_init_interv = Start_tick.Value.Minute / (int)intrvl;
                min_init_interv = min_init_interv * (int)intrvl;
                //min_init_interv = Convert.ToInt32(Math.Floor(Start_tick.Value.Minute / intrvl) * intrvl);
            }
            else if (intrvl == 60)
                min_init_interv = 0;
            else if (intrvl == 240)
            {
                min_init_interv = 0;
                hour_init_interv = Start_tick.Value.Hour / 4;
                hour_init_interv = hour_init_interv * 4;
            }
            else if (intrvl == 1440)
            {
                hour_init_interv =
                    min_init_interv = 0;
            }
            //else intrvl = 1 - оставляем как было

            DateTime return_date = new DateTime(year_init_interv, month_init_interv, day_init_interv, hour_init_interv, min_init_interv, sec);

            return return_date;
        }

        private void set_gap(DateTime? tek_time)
        {
            chart.Add(new Chart { bar_lows = 9999999, bar_lows_double = 9999999, Bars = new List<Cluster>(), time_of_bar = tek_time, color = -1 });
        }

        //DrawChart

        private void RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (hstr.stop_load)//e.Cancelled)
            {
                label7.Content = "";
                Return_Click_12();
                return;
            }

            if (chart.Count > 0)
                DrawChart(true);
            else
            {
                Return_Click_12();

                if (!IsWorkRealTime)
                {
                    label7.Content = "";//тока c истроией, a в реалтайме оставляем "Loading..."

                    canvas_chart.Margin = new Thickness(0, 0, 0, 0); //задаем  положение для canvas1-график
                    Label new_label = new Label();
                    new_label.Content = "No data to display";
                    new_label.FontSize = 20;
                    new_label.Foreground = Brushes.Gray;
                    Canvas.SetTop(new_label, border_chart.Height / 2 - 7);
                    Canvas.SetLeft(new_label, border_chart.Width / 2 - 90);
                    canvas_chart.Children.Add(new_label);
                }   
                else timer.Start();
            }
        }

        private void DrawChart(bool isRunWorkerCompleted)
        {
            Create_Price_Axis();//создаём временную шкалу

            double mwb;
            if (!(price_canvases.Count >= 3000 && interval == 1440 && chart.Count >= 100))
                isClusterChart = true;
            else//если инструмент SPX.XO или interval = 1440 и график очень высокий то кластера не заполняем
                isClusterChart = false;

            if (isClusterChart)
                mwb = Calculate_mwb(chart);
            else
            {
                mwb = 30;
                bars_max_volume = chart.SelectMany(ch => ch.Bars).Max(br => br.volume);
            }

            if (interval == 1440)
                cratnost_time_labels = calculate_cratnost_time_labels(mwb);
            else if (interval == 5)
                cratnost_time_labels = 30;//выводим тока часовики и 30-ти минутки
            else if (interval == 1)
                cratnost_time_labels = 5;//выводим всё бары, где минуты кратны 5

            int strt;
            if (re_posit && !pozizionir)// && hiet != -17)
            {
                int ind_5 = Find_N_reposition_bar();
                if (ind_5 == -1)
                    strt = chart.Count - Convert.ToInt32(border_chart.Width);//создаём бары, чтобы хватило на 2 бордера
                else strt = ind_5 - Convert.ToInt32(0.75 * border_chart.Width);//от центрального бара отступаем назад на 1,5 бордера и создаём все бары от этой точки и до последнего
            }
            else
                strt = chart.Count - Convert.ToInt32(border_chart.Width);//создаём бары, чтобы хватило на 2 бордера

            //должно быть до CreateClusterBar() - там используется max_width_bar
            set_buferX_max_width_bar(mwb);

            if (strt < 0) strt = 0;

            for (int i = strt; i < chart.Count; i++)
            {
                string N_cell_of_current_bar = i.ToString();
                CreateClusterBar(chart[i], N_cell_of_current_bar);
                CreateTimeScaleLable(chart[i], N_cell_of_current_bar);
            }

            //должно быть до edid_chart(), т.е. до Positionirov()
            if (vertical_gist)
                Calculate_vertical_gistogramm(strt);

            SetPriceCursor(false);

            if (!re_posit)//если это не перерисовка прошлого графика
            {
                re_posit = true;
                pozizionir = true;
                Positionirov(true, false);
                button15.Foreground = Brushes.Red;
            }
            else
                no_change_chart_pozit(true, false);//если это перерисовка прошлого графика и новый график должен нарисоваться на том же месте где и прошлый

            DrawVerticalLines();
            DrawHorizontalLines();

            if (isGist)
                Calculate_hstgrm();

            Visibl_Hidden_canvases(true);//!!!!!! должно быть до DoEvents();
            DoEvents();

            if (hstr.stop_load)//если "Stop loading" таки нажимали
            {
                Clear_canvases();
                Visibl_Hidden_canvases(false);//!!!!!! должно быть после Clear_canvases();
                Return_Click_12();
                label7.Content = "";
                return;
            }

            button_press = false;//!!!!!! должно быть после DoEvents() на случай если нажали "Stop loading" и тот поток стоял в ожидании очереди, а после DoEvents() он начнёт выполняться и в Load_chart(), он должен попасть в условие if (button_press), пэ button_press до DoEvents() должно быть = true

            Visibl_Hidden_canvases(false);//!!!!!! должно быть после if(hstr.stop_load) и до   DoEvents();

            if (bufer_x >= max_width_bar + 1 && chart_mas_y >= 12)
                button14.Content = "-";
            else button14.Content = "+";

            if (isRunWorkerCompleted)
            {
                button12.Content = "Reload chart";//!!!!!! должно быть до DoEvents();
                Load_BorderChartContextMenu.Header = "Reload chart";
                if (IsWorkRealTime)
                    timer.Start();
            }

            DoEvents();

            if (strt > 0)
            {
                int bufer_i = strt - 1;
                for (int i = strt - 1; i >= 0; i--)
                {
                    if (hstr.stop_load || button_press)//если нажали "Reload chart"
                        break;

                    string N_cell_of_current_bar = i.ToString();

                    lock (chart)
                    {
                        CreateClusterBar(chart[i], N_cell_of_current_bar);
                        CreateTimeScaleLable(chart[i], N_cell_of_current_bar);
                        if (vertical_gist)
                            Create_vertical_gistigramm_bar(chart[i], SearchPreviosBar(N_cell_of_current_bar, chart));
                    }

                    if (isRunWorkerCompleted && bufer_i - i >= 500)
                    {
                        DoEvents();
                        bufer_i = i;
                    }
                }
            }

            MenuSettings.IsEnabled = true;
            //if (!IsWorkRealTime)//тока c истроией, a в реалтайме оставляем "Loading..."
                label7.Content = "";
        }

        private double Calculate_mwb(List<Chart> chart_list)
        {
            double mwb = 1;            
            int volRzrdn = 0;
            foreach(Chart chart in chart_list)
                foreach(Cluster cluster in chart.Bars)
                {
                    string volumeString = ((decimal)cluster.volume).ToString(Vars.FormatInfo);
                    string[] volSpl = volumeString.Split('.');
                    if (volSpl[0].Length > mwb)
                        mwb = volSpl[0].Length;
                      
                    if (volSpl.Length > 1 && volSpl[1].Length > volRzrdn)
                        volRzrdn = volSpl[1].Length;
                }

            if (volRzrdn > volumeRazrdn)
                volumeRazrdn = volRzrdn;

            double totalVolume = 0;
            List<double> allClustersVolume = new List<double>();
            foreach (Chart chart in chart)
                foreach (Cluster cluster in chart.Bars)
                {
                    totalVolume += cluster.volume;
                    allClustersVolume.Add(cluster.volume);
                }

            allClustersVolume.Sort();
            double currentTotalVolume = 0;
            for(int n= allClustersVolume.Count - 1; n>= 0; n--)
            {
                currentTotalVolume += allClustersVolume[n];
                if(currentTotalVolume >= totalVolume*0.02)
                {
                    if (bars_max_volume < allClustersVolume[n])
                        bars_max_volume = allClustersVolume[n];

                    break;
                }
            }

            return (mwb + volRzrdn) * 7 + 1;
        }

        private int calculate_cratnost_time_labels(double mwb)
        {
            return Convert.ToInt32(Math.Ceiling(distance_between_visible_time_labels / (mwb + 1)));
        }
   
        private int Find_N_reposition_bar()
        {
            int ind_5 = -1;
            if (reposition_date != null)
                ind_5 = chart.FindLastIndex(delegate(Chart ch) { return reposition_date >= ch.time_of_bar && reposition_date.Value < ch.time_of_bar.Value.AddMinutes(interval); });

            return ind_5;
        }

        private double Calculate_N_cel_in_Price_scale_list(double current_bar_max_price)
        {
            //!!!!!!!!!   сдесь округляться нельзя, т.к. тики с ценой более дробной чем шаг цены будут рисоваться с округлённой ценой
            return (price_canvases[0].price - current_bar_max_price) / price_step;
        }

        private double Calculate_candle_body_N_cell(Chart current_bar)
        {
            if (current_bar.color == -5)
                return Vars.MathRound((price_canvases[0].price - current_bar.open_price) / price_step);
            else
                return Vars.MathRound((price_canvases[0].price - current_bar.close_price) / price_step);
        }

        private void set_buferX_max_width_bar(double mwb)
        {
            if (bufer_x == -1 || (bufer_x >= max_width_bar + 1 && chart_mas_y >= 12 && isClusterChart))//если до перерисовки графика лейблы в кластерах были видимы
            {
                max_width_bar = mwb;
                bufer_x = max_width_bar + 1;//расстояние му барами в пикселях
            }
            else
            {
                max_width_bar = mwb;
                if (bufer_x > max_width_bar + 1)
                {
                    bufer_x = max_width_bar + 1;
                }

                if (bufer_x == 3)
                    bufer_x = 4;
            }
        }

        private void no_change_chart_pozit(bool history, bool isNeed_redraw_vertical_lines)
        {
            if (pozizionir)
                Positionirov(history, isNeed_redraw_vertical_lines);
            else
            {
                int ind_5 = Find_N_reposition_bar();
                reposition_date = null;

                if (ind_5 == -1)
                {
                    Positionirov(true, isNeed_redraw_vertical_lines);
                    return;
                }

                int ghyt = Convert.ToInt32(Math.Ceiling(border_chart.Width / bufer_x / 2));
                N_fb = ind_5 - ghyt;
                if (N_fb < 0) N_fb = 0;
                N_lb = ind_5 + ghyt;
                if (N_lb > chart.Count - 1) N_lb = chart.Count - 1;
                edit_chart(N_fb, N_lb, N_fb, true);
                double pozt_x = Math.Round(border_chart.Width / 2 - Canvas.GetLeft(chart[ind_5].chart_b), MidpointRounding.AwayFromZero);
                visibl_first_date_label();
                int index_5 = price_canvases.FindIndex(pc => pc.price == hiet);
                pos_y = Math.Round(border_chart.Height / 2 - index_5 * chart_mas_y, MidpointRounding.AwayFromZero);
                pos_y = vverx_vniz(pos_y);

                canvas_chart.Margin = new Thickness(pozt_x, pos_y, 0, 0); //задаем  положение для canvas1-график      
                canvas_prices.Margin = new Thickness(0, pos_y, 0, 0); //задаем  положение для ценовой шкалы
                canvas_times.Margin = new Thickness(pozt_x, 0, 0, 0); //задаем  положение для временной шкалы
                LinesCanvas.Margin = new Thickness(0, pos_y, 0, 0);
                canvas_vertical_line.Margin = new Thickness(pozt_x, 0, 0, 0);
                if (isGist)
                    canvas_gistogramm.Margin = new Thickness(0, pos_y, 0, 0);
                if (vertical_gist)
                    canv_vert_gstgr.Margin = new Thickness(pozt_x, border_chart.Height - VerticalHistohramBarMaxHeight, 0, 0);

                PriceCursorSetLeft(pozt_x);
            }
        }

        private void Visibl_Hidden_canvases(bool hidden)
        {
            if (hidden)
            {
                canvas_chart.Visibility = Visibility.Hidden;
                canvas_prices.Visibility = Visibility.Hidden;
                canvas_times.Visibility = Visibility.Hidden;
                LinesCanvas.Visibility = Visibility.Hidden;
                canvas_vertical_line.Visibility = Visibility.Hidden;
                canvas_gistogramm.Visibility = Visibility.Hidden;
                canv_vert_gstgr.Visibility = Visibility.Hidden;
            }
            else
            {
                canvas_chart.Visibility = Visibility.Visible;
                canvas_prices.Visibility = Visibility.Visible;
                canvas_times.Visibility = Visibility.Visible;
                LinesCanvas.Visibility = Visibility.Visible;
                canvas_vertical_line.Visibility = Visibility.Visible;
                canvas_gistogramm.Visibility = Visibility.Visible;
                canv_vert_gstgr.Visibility = Visibility.Visible;
            }
        }

        #endregion

        #region realtime

        private void send_to_server(object sender, EventArgs e)//ф-ция вызывается таймерom
        {
            try
            {
                if (!Vars.ServerQueries.ContainsKey(ServerQuery))//Данный инструмент в реалтайме не качается
                {
                    label7.Content = "";//гасим "Loading..." и
                    timer.Stop();//и останавливаем таймер
                    return;
                }

                List<Tick> newTicks = GetRealTimeTicks();

                if (newTicks.Count > 0)
                {
                    summary_t = newTicks.Last();
                    lock (chart)
                        Chart_Update(newTicks);
                }

                lock (Vars.main_windows)
                    foreach (ChartWindow chartWindow in Vars.main_windows)
                        if (Vars.IsOnConnection) chartWindow.Background = Brushes.Black;
                        else chartWindow.Background = Brushes.Red;
            }
            catch { }
        }

        private void Chart_Update(List<Tick> new_ticks)//дорисовываем график из поступивших реалтаймтиков
        {
            if (chart.Count > 0)//если какиe-то бары уже были просчитаны и нарисованы
            {
                double max = new_ticks.Max(rt => rt.price);//мин и мах цены в новый порци реалтаймтиков
                double minm = new_ticks.Min(rt => rt.price);
                if (max > price_canvases[0].price - 100 * price_step || minm < price_canvases.Last().price + 100 * price_step)
                    Add_Price_Axis(max, minm);//дорисовываем ценовую шкалу, если реалтаймтики выходят за рамки имеющейся ценовой шкалы

                int chart_count_bufer = chart.Count;
                double mwb= RT_ChartUpdate(new_ticks, chart_count_bufer);
               
                realtime_edit_chart(mwb, chart_count_bufer);//выводим новые бары на график
                set_activ_lines_new_positions();
                SetPriceCursor(true);//устанавливаем курсор текущей цены и его вертикальн коорд. А горизонтальная  устанавливается в realtime_ed_chart, no_change_chart_pozit(), Positionirov, UpdateXY_1, Expand, Mod_Bars_X
            }
            else//если из истории ни одного бара ещё не просчитали и не нарисовали
            {
                Bike(new_ticks);
                if (chart.Count > 0)// chart мог и не наполниться, при интервале == 0,1 все тики могли собраться в транзитных массивах, если не было бид-аск-афтер, или большой влюм-фильтр
                    DrawChart(false);
            }
        }

        private double RT_ChartUpdate(List<Tick> new_ticks, int chart_count_bufer)
        {
            DateTime? initial_interv = chart.Last().time_of_bar.Value.AddMinutes(interval), initial_next_interval = null;
            double mwb = 0;

            bool isPererisovka_last_bar = false;
            if (new_ticks[0].date < initial_interv)//если поступившие реалтайм-тики смешиваются с прошлим интервалом, т.е. уже нарисованным баром, то
            {
                isPererisovka_last_bar = true;
                initial_next_interval = initial_interv;
                initial_interv = chart.Last().time_of_bar;
            }
            else//если поступившие реалтайм-тики не смешиваются с прошлим интервалом
                initial_next_interval = initial_interv.Value.AddMinutes(interval);
                
            RT_Bike(new_ticks, initial_interv, initial_next_interval);

            //должно быть до realtime_ed_chart()
            if (vertical_gist)
            {
                int chart_count_bufer_for_sent = chart_count_bufer;
                if (isPererisovka_last_bar)
                    chart_count_bufer_for_sent = chart_count_bufer - 1;

                double[] result_array = Calculate_Max_Veriical_Histogramm_for_BarChart(chart_count_bufer_for_sent);
                realtime_update_vertical_histogram(result_array, chart_count_bufer, isPererisovka_last_bar);
            }

            double bars_max_volume_before_update = bars_max_volume;
            if (isClusterChart)
            {
                if(isPererisovka_last_bar)
                    mwb = Calculate_mwb(chart.GetRange(chart_count_bufer - 1, chart.Count - chart_count_bufer + 1));                    
                else
                    mwb = Calculate_mwb(chart.GetRange(chart_count_bufer, chart.Count - chart_count_bufer));
            }

            if (!isClusterChart)//если мы не вызывали метод Calculate_mwb() двумя строчками выше, значит мы ещё не переопределяли bars_max_volume, пэ делаем это сейчас
            {
                double max_volume = chart.GetRange(chart_count_bufer - 1, chart.Count - chart_count_bufer + 1).SelectMany(ch => ch.Bars).Max(br => br.volume);
                if (bars_max_volume < max_volume)
                    bars_max_volume = max_volume;
            }

            if (max_width_bar < mwb)
            {
                for (int i = 0; i < chart.Count; i++)
                {
                    if (chart[i].bar_highs == 0) continue;
                    chart[i].filters.Width = mwb - 1;
                    for (int n = 0; n < chart[i].Bars.Count; n++)//          даже если bars_max_volume только что увеличилось, то сдесь стоит уже обновлённое его значение, не смотря на то что мы не проверяли - увеличилось ли оно, или нет
                        if (n < chart[i].canv_filters.Count)
                            chart[i].canv_filters[n].fltr.Width = chart[i].Bars[n].volume / bars_max_volume * (mwb - 1);// !!!!!!!!! важное отличие - сдесь не max_width_bar, а mwb
                }

                if (bars_max_volume_before_update < bars_max_volume)//если только max_width_bar < mwb, то перерисовывать кластер профайл нет смысла, даже с изменившимся max_width_bar кластерпрофайл не изменяет свои размеры
                {//кластер профайл есть смысл перерисовывать тока если bufer_bars_max_volume < bars_max_volume, тока тогда он изменяет свои размеры
                    if (bufer_x > 8 && !(isClusterChart && bufer_x >= max_width_bar + 1 && chart_mas_y >= 12))
                        RenderTransformClusterProfile(mwb);// !!!!!!!!! важное отличие - сдесь не max_width_bar, а mwb
                }
            }
            else if (bars_max_volume_before_update < bars_max_volume)
            {
                for (int i = 0; i < chart.Count; i++)
                    if (chart[i].bar_highs != 0)
                        for (int n = 0; n < chart[i].Bars.Count; n++)
                            if(n < chart[i].canv_filters.Count)
                                chart[i].canv_filters[n].fltr.Width = chart[i].Bars[n].volume / bars_max_volume * (max_width_bar - 1);// !!!!!!!!! важное отличие - сдесь max_width_bar, а не mwb

                if (bufer_x > 8 && !(isClusterChart && bufer_x >= max_width_bar + 1 && chart_mas_y >= 12))
                    RenderTransformClusterProfile(max_width_bar);// !!!!!!!!! важное отличие - сдесь max_width_bar, а не mwb
            }

            if (isGist)
                real_time_updt_gsst(new_ticks);

            return mwb;
        }

        private void RT_Bike(List<Tick> RTTicks, DateTime? initial_interv, DateTime? initial_next_interval)
        {
            int s = 0;

            start:
            
            int f = RTTicks.FindIndex(s, delegate(Tick tick) { return tick.date >= initial_next_interval; });
            if (f < 0)  f = RTTicks.Count;
            if (f > s)
            {               
                List<Tick> interv_t = RTTicks.GetRange(s, f - s);//отбираем интервал
                List<Cluster> bar = CalculateBarPriceRating(interv_t);

                if (initial_interv > chart.Last().time_of_bar)//если рисуем новый бар
                {
                    double max_bar = bar.Last().price;
                    double min_bar = bar[0].price;
                    double height_tick_count = Calculate_BarHeightPips(max_bar, min_bar);
                    double price_open_bar = interv_t[0].price;
                    double price_close_bar = interv_t.Last().price;
                    int color = -5;
                    if (price_open_bar <= price_close_bar)
                        color = -6;//определяем цвет бара

                    CreateChartBar(initial_interv, bar, color, price_open_bar, price_close_bar, max_bar, min_bar, height_tick_count, chart);
                    string N_cell_of_current_bar = (chart.Count - 1).ToString();
                    CreateClusterBar(chart.Last(), N_cell_of_current_bar);
                    CreateTimeScaleLable(chart.Last(), N_cell_of_current_bar);
                }
                else//initial_interv = chart.Last().time_of_bar - перерисовываем уже нарисованный бар - во всех массивах  изменяем данные об этом баре
                {
                    chart.Last().Bars = RecalculateBarCluster(bar, chart.Last().Bars);
                    double max_bar = chart.Last().Bars.Last().price;
                    double min_bar = chart.Last().Bars[0].price;
                    double height_tick_count = Calculate_BarHeightPips(max_bar, min_bar);
                    chart.Last().bar_lows = min_bar;
                    chart.Last().bar_highs = max_bar;
                    chart.Last().bar_lows_double = min_bar;
                    chart.Last().bar_highs_double = max_bar;


                    chart.Last().bar_height_pips_count = height_tick_count;
                    chart.Last().N_cel_in_Price_scale_list = Vars.MathRound(Calculate_N_cel_in_Price_scale_list(max_bar));
                    double price_close_bar = interv_t.Last().price;
                    chart.Last().close_price = price_close_bar;

                    if (chart.Last().open_price > price_close_bar)
                    {
                        chart.Last().color = -5;
                        if (chart.Last().chart_b.Background.ToString() != "#00FFFFFF")//Transparent  - если не кластерпрофайл
                        {
                            if (bufer_x <= 8)// candle
                                chart.Last().chart_b.Background = Brushes.Gray;
                            else
                                chart.Last().chart_b.Background = (Brush)bc.ConvertFrom("#FF4D4D55");
                        }
                    }
                    else
                    {
                        chart.Last().color = -6;
                        if (chart.Last().chart_b.Background.ToString() != "#00FFFFFF")//Transparent  - если не кластерпрофайл
                            chart.Last().chart_b.Background = Brushes.Black;
                    }

                    //должно быть после определения цвета
                    chart.Last().candle_body_N_cell = Calculate_candle_body_N_cell(chart.Last());
                    chart.Last().candle_body_height_pips = Math.Round(Math.Abs(chart.Last().open_price - chart.Last().close_price) / price_step, MidpointRounding.AwayFromZero);

                    if (chart.Last().cnv_lbls != null)
                        chart.Last().cnv_lbls.Height = height_tick_count * 12;

                    if (chart.Last().filters != null && chart.Last().canv_filters.Count > 0)
                        chart.Last().filters.Height = height_tick_count * 12 - 1;

                    chart.Last().cnv_lbls.Children.Clear();//очищаем из массивов всё что касается наполнения бара контентом(лейблами и цветовыми фильтрами)
                    chart.Last().volume_labels.Clear();
                    if (chart.Last().canv_filters != null)
                    {
                        chart.Last().filters.Children.Clear();
                        chart.Last().canv_filters.Clear();
                    }

                    bar_content(chart.Last(), (chart.Count - 1).ToString(), null);

                    if (chart.Last().chart_b.Parent != null)//если пересчитываемый бар видим в бордере
                    {
                        double body_Height, new_position_Y;
                        if (bufer_x <= 8)//если выводим свечи
                        {
                            chart.Last().shadow.Height = Calculate_candle_shadow_height(chart.Last());
                            double posit_Y = Calculate_candle_shadow_position_top(chart.Last());
                            Canvas.SetTop(chart.Last().shadow, posit_Y);
                            body_Height = Calculate_candle_body_height(chart.Last());
                            new_position_Y = Calculate_candle_body_position_top(chart.Last());
                        }
                        else//если свечи не выводим
                        {
                            body_Height = Calculate_bar_height(chart.Last());//вычисляем новую высоту бара в пикселях
                            new_position_Y = Calculate_bar_position_top(chart.Last().N_cel_in_Price_scale_list);//вычисляем новую координату бара по вертикали
                        }

                        /*if (bufer_x > 8)// && chart_mas_y >= 3)//eсли выводим бары
                        {
                            body_Height = Calculate_bar_height(current_bar);
                            position_y = Calculate_bar_position_top(current_bar.N_cel_in_Price_scale_list);//координата У текущего бара
                            current_bar.shadow.Visibility = Visibility.Hidden;
                        }
                        else//если выводим свечи
                        {
                            current_bar.shadow.Height = Calculate_candle_shadow_height(current_bar);
                            double posit_Y = Calculate_candle_shadow_position_top(current_bar);
                            Canvas.SetTop(current_bar.shadow, posit_Y);
                            Canvas.SetLeft(current_bar.shadow, Math.Floor(positions_X + bar_width / 2));
                            body_Height = Calculate_candle_body_height(current_bar);
                            position_y = Calculate_candle_body_position_top(current_bar);
                            current_bar.shadow.Visibility = Visibility.Visible;
                        }*/

                        chart.Last().chart_b.Height = body_Height;
                        Canvas.SetTop(chart.Last().chart_b, new_position_Y);//изменяем расположение этого бара в соответствиe с новой вертикальной координатой

                        if (chart.Last().canv_filters != null && chart.Last().canv_filters.Count > 0 && !(isClusterChart && bufer_x >= max_width_bar + 1 && chart_mas_y >= 12))
                        {
                            if (bufer_x > 8)
                            {
                                double bar_width = bufer_x - 1;
                                //if (bufer_x == 1) bar_width = 1;
                                RenderTransformCurrentBarClusterProfile(chart.Last(), bar_width, max_width_bar);
                            }
                        }
                    }
                }
            }
            else//f = s
            {
                set_gap(initial_interv);
                string N_cell_of_current_bar = (chart.Count - 1).ToString();
                CreateClusterBar(chart.Last(), N_cell_of_current_bar);
                CreateTimeScaleLable(chart.Last(), N_cell_of_current_bar);
            }

            if (f < RTTicks.Count)
            {
                s = f;
                initial_interv = initial_next_interval;
                initial_next_interval = initial_interv.Value.AddMinutes(interval);
                goto start;
            }
        }

        private List<Cluster> RecalculateBarCluster(List<Cluster> newTicksCluster, List<Cluster> oldCluster)
        {
            newTicksCluster.AddRange(new List<Cluster>(oldCluster));//смешиваем старый кластер перерисовывемого бара с кластером, просчитанным из новых тиков, входящих в данный бар
            oldCluster.Clear();
            IEnumerable<IGrouping<double, Cluster>> clustersGroups = newTicksCluster.GroupBy(cl => cl.price, cl => cl);//перерасчитываем обьединённый кластер
            foreach (IGrouping<double, Cluster> currentGroup in clustersGroups)
            {
                Cluster newCluster = new Cluster();
                newCluster.price = currentGroup.Key;
                newCluster.volume = currentGroup.Sum(cl => cl.volume);
                newCluster.buy = currentGroup.Sum(cl => cl.buy);
                newCluster.sell = currentGroup.Sum(cl => cl.sell);
                oldCluster.Add(newCluster);
            }

            oldCluster.Sort(delegate (Cluster x, Cluster y) { return x.price.CompareTo(y.price); });//сортировка по цене


            return oldCluster;
        }

        private void realtime_edit_chart(double mwb, int chart_count_bufer)
        {
            if (max_width_bar < mwb && bufer_x >= max_width_bar + 1 && chart_mas_y >= 12)//если у мах об-ма увеличилась разрядность, то изменяется мах ширина бара и если лейблы с объёмами в данный момент видимы(не скрыты)
            {
                max_width_bar = mwb;//запоминаем глобально новую мах ширину баров

                ResizeClusterLablesWidth();

                if (pozizionir)
                {
                    bufer_x = mwb + 1;
                    masshtab_chart_X(true, false, false);
                    masshtab_X_vertic_gistgrm(N_fb, N_lb);
                    Positionirov(false, true);
                }
                else//увеличиваем ширину баров и рздвигаем график от центра в стороны и дорисовываем новые бары, если после расширения последний бар так и остался попадаемым в бордер по горизонтали
                {
                    int indx_5 = Calculate_Horizontal_Central_Cell();//определ номер центрального бара по горизонтали

                    bufer_x = mwb + 1;

                    bool vv_vn = remove_sprava_sleva(indx_5, false, false, false);
                    masshtab_X_vertic_gistgrm(N_fb, N_lb);

                    double pozt_y = canvas_chart.Margin.Top;
                    if (vv_vn)
                    {
                        pozt_y = vverx_vniz(pozt_y);

                        canvas_prices.Margin = new Thickness(0, pozt_y, 0, 0);
                        LinesCanvas.Margin = new Thickness(0, pozt_y, 0, 0);
                        if (isGist)
                            canvas_gistogramm.Margin = new Thickness(0, pozt_y, 0, 0);
                    }

                    double pozt_x = Math.Round(border_chart.Width / 2 - Canvas.GetLeft(chart[indx_5].chart_b), MidpointRounding.AwayFromZero);

                    if (chart[chart_count_bufer - 1].chart_b.Parent != null && chart_count_bufer < chart.Count)//если последний бар выведен в бордер, или можно if(N_lb == chart_count_bufer - 1) и появились новые бары, то дорисовываем их
                        add_sprava(pozt_x, false);

                    canvas_chart.Margin = new Thickness(pozt_x, pozt_y, 0, 0);
                    canvas_times.Margin = new Thickness(pozt_x, 0, 0, 0);
                    canvas_vertical_line.Margin = new Thickness(pozt_x, 0, 0, 0);
                    if (vertical_gist)
                        canv_vert_gstgr.Margin = new Thickness(pozt_x, border_chart.Height - VerticalHistohramBarMaxHeight, 0, 0);

                    PriceCursorSetLeft(pozt_x);
                }

                set_new_indx();//если в данный момент, во время реалтайм-обновления графика выполняется поток масштабирующий или передвигающий график, то мы изменяем данные  об обновлённом графике, которые использует те потоки
            }
            else //если лейблы с объёмами в данный момент не видимы(скрыты), то ширину видимых баров не меняем а только дорисовываем новые бары
            {
                if (max_width_bar < mwb)
                {
                    max_width_bar = mwb;//запоминаем глобально новую мах ширину баров  
                    ResizeClusterLablesWidth();
                }

                if (pozizionir)
                {
                    Positionirov(false, true);
                    set_new_indx();
                }
                else if (chart[chart_count_bufer - 1].chart_b.Parent != null && chart_count_bufer < chart.Count)//если пересчитываемый последний бар видим в бордере и за ним появились новые бары то
                {
                    add_sprava(canvas_chart.Margin.Left, false);//дорисовываем вновь поступившие бары
                    PriceCursorSetLeft(canvas_chart.Margin.Left);
                    set_new_indx();
                }
            } 
        }

        private void ResizeClusterLablesWidth()
        {
            if (isClusterChart)
                foreach (Chart current_bar in chart)
                    current_bar.cnv_lbls.Width = max_width_bar - 2;
        }

        private void set_new_indx()
        {
            pos_y = canvas_chart.Margin.Top;
            pos_x = canvas_chart.Margin.Left;
            if (mas_X)
                indx = Calculate_Horizontal_Central_Cell();
            else if (mas_Y)
            {
                indx = Calculate_Vertical_Central_Cell(); 
                int rng = N_lb - N_fb + 1;
                mx = chart.GetRange(N_fb, rng).Max(ch => ch.bar_highs_double);//мах цена видимого графика
                hiet = Math.Round((mx - chart.GetRange(N_fb, rng).Min(ch => ch.bar_lows_double)) / price_step + 1, MidpointRounding.AwayFromZero);//высота видимого графика в тиках
            }
        }

        #endregion

        #region CreateBar

        private void CreateChartBar(DateTime? bar_time, List<Cluster> bar, int color, double price_open_bar, double price_close_bar,
            double max_bar, double min_bar, double bar_height_pips_count, List<Chart> current_chart_list)
        {
            Chart new_chart = new Chart
            {
                bar_highs = max_bar,
                bar_lows = min_bar,
                color = color,
                time_of_bar = bar_time,
                Bars = bar,
                open_price = price_open_bar,
                close_price = price_close_bar,
                bar_highs_double = max_bar,
                bar_lows_double = min_bar,
                bar_height_pips_count = bar_height_pips_count
            };

                new_chart.candle_body_height_pips = Math.Round(Math.Abs(price_open_bar - price_close_bar) / price_step, MidpointRounding.AwayFromZero);

            if (current_chart_list != null)
                current_chart_list.Add(new_chart);//сохраняем бар в массив для графика
        }

        private void CreateClusterBar(Chart current_bar, string N_cell_of_current_bar) 
        {
            Border new_bar = new Border();
            current_bar.chart_b = new_bar;
                current_bar.shadow = new Rectangle();

            if (current_bar.bar_highs == 0)
                return;

            current_bar.N_cel_in_Price_scale_list = Calculate_N_cel_in_Price_scale_list(current_bar.bar_highs);
            new_bar.BorderThickness = new Thickness(1);
            int color = current_bar.color;

            new_bar.BorderBrush = Brushes.LightGray;// (Brush)bc.ConvertFrom("#FFB27B00");
            if (color == -6)
                new_bar.Background = Brushes.Black;
            else
                new_bar.Background = (Brush)bc.ConvertFrom("#FF4D4D55");

            current_bar.candle_body_N_cell = Calculate_candle_body_N_cell(current_bar);
            current_bar.shadow.Width = 1;
            current_bar.shadow.Fill = Brushes.Gray;
            current_bar.shadow.Fill.Freeze();

            new_bar.ClipToBounds = true;
            Canvas in_border = new Canvas();
            new_bar.Child = in_border;
            if (isClusterChart)
            {
                Canvas for_labels = new Canvas();
                current_bar.cnv_lbls = for_labels;
                for_labels.ClipToBounds = true;
                for_labels.Width = max_width_bar - 2;
                for_labels.Height = current_bar.bar_height_pips_count * 12;
                Canvas.SetRight(for_labels, 0);
                Canvas.SetZIndex(for_labels, 1);
                in_border.Children.Add(for_labels);
                current_bar.volume_labels = new List<Label>();
            }

            bar_content(current_bar, N_cell_of_current_bar, in_border);
            
            new_bar.Background.Freeze();
            new_bar.BorderBrush.Freeze();
        }

        private void CreateTimeScaleLable(Chart current_bar, string N_cell_of_current_bar)
        {
            int i = Convert.ToInt32(N_cell_of_current_bar);
            
            DateTime? previos_bar_time = null;
            if (i > 0) previos_bar_time = chart[i - 1].time_of_bar;

            if (interval < 1440)
            {
                bar_time(current_bar, i);
                bar_date(previos_bar_time, current_bar);
            }
            else//если интервал Day
            {
                DateTime? for_label = current_bar.time_of_bar;

                bar_time_1440(current_bar, for_label, i);
                bar_date_1440(previos_bar_time, current_bar, for_label);
            }
        }

        private void bar_time(Chart current_bar, int i)
        {
            if (interval > 5)
            {
                if (current_bar.time_of_bar.Value.Minute != 0)//выводим тока часовики
                    return;
            }
            else//interval == 1 || interval == 5
                if (!(current_bar.time_of_bar.Value.Second == 0 && current_bar.time_of_bar.Value.Minute % cratnost_time_labels == 0))
                return;

            Canvas new_label_1 = new Canvas();

            //выводим во временную шкалу время текущего бара
            Label time_label = new Label();
            string bartime = current_bar.time_of_bar.Value.ToString("HH:mm");

            time_label.Content = bartime;
            time_label.Background = Brushes.Transparent;
            time_label.Foreground = Brushes.LightGray;//(Brush)bc.ConvertFrom("#FFA66C06");//
            Canvas.SetLeft(time_label, -3);
            Canvas.SetTop(time_label, -5);
            new_label_1.Children.Add(time_label);

            Rectangle nasech = new Rectangle();
            Canvas.SetLeft(nasech, otstup - 4);
            nasech.Width = 1;
            nasech.Height = 3;
            nasech.Fill = Brushes.LightGray;//(Brush)bc.ConvertFrom("#FFA66C06");//
            Canvas.SetZIndex(nasech, 1);
            new_label_1.Children.Add(nasech);

            current_bar.time_labels = new_label_1;//сохраняем в массив
        }

        private void bar_date(DateTime? previos_bar_time, Chart current_bar)
        {
            //дату выводим если это первый тик (past_time == null) или если наступил новый день
            if (!(previos_bar_time == null || current_bar.time_of_bar.Value.Date > previos_bar_time.Value.Date))
                return;

            Label new_label_12 = new Label();

            string dtf = current_bar.time_of_bar.Value.Day.ToString();
            if (current_bar.time_of_bar.Value.Day < 10) dtf = "0" + dtf;
            string mnf = current_bar.time_of_bar.Value.Month.ToString();
            if (current_bar.time_of_bar.Value.Month < 10) mnf = "0" + mnf;
            dtf = dtf + "/" + mnf;
            dtf = "  " + dtf;
            new_label_12.Content = dtf;
            new_label_12.Foreground = Brushes.LightGray;//(Brush)bc.ConvertFrom("#FFA66C06");//
            Canvas.SetTop(new_label_12, 10);

            current_bar.date_Labels = new_label_12;//сохраняем в массив
        }

        private void bar_time_1440(Chart current_bar, DateTime? for_label, int i)
        {
            if (i % cratnost_time_labels != 0)
                return;

            Canvas new_label_1 = new Canvas();

            Label date_label = new Label();
            string dtf = for_label.Value.Day.ToString();
            if (for_label.Value.Day < 10) dtf = "0" + dtf;
            string mnf = for_label.Value.Month.ToString();
            if (for_label.Value.Month < 10) mnf = "0" + mnf;
            dtf = dtf + "/" + mnf;
            date_label.Content = dtf;
            date_label.Background = Brushes.Transparent;
            date_label.Foreground = Brushes.LightGray;//(Brush)bc.ConvertFrom("#FFA66C06");//
            Canvas.SetTop(date_label, -5);
            Canvas.SetLeft(date_label, -3);
            new_label_1.Children.Add(date_label);

            Rectangle nasech = new Rectangle();
            Canvas.SetLeft(nasech, otstup - 4);
            nasech.Width = 1;
            nasech.Height = 3;
            Canvas.SetZIndex(nasech, 1);
            nasech.Fill = Brushes.LightGray;//(Brush)bc.ConvertFrom("#FFA66C06");//
            new_label_1.Children.Add(nasech);

            current_bar.time_labels = new_label_1;//сохраняем в массив
        }

        private void bar_date_1440(DateTime? previos_bar_time, Chart current_bar, DateTime? for_label)
        {
            if (!(previos_bar_time == null || for_label.Value.Year > previos_bar_time.Value.Year))
                return;

            Label new_label_12 = new Label();

            new_label_12.Content = " " + for_label.Value.Year.ToString();
            new_label_12.Foreground = Brushes.LightGray;//(Brush)bc.ConvertFrom("#FFA66C06");//
            Canvas.SetTop(new_label_12, 10);

            current_bar.date_Labels = new_label_12;
        }

        private void bar_content(Chart current_bar, string N_cell_of_current_bar, Canvas in_border)
        {
            double m_v_b = 0;
            if (max_volume_in_bar)//если нужно выделять мах объём в баре, то определяем его
            {
                m_v_b = current_bar.Bars.Max(b => b.volume);
            }

            AutoDetermineVolumeFilters(N_cell_of_current_bar);
            double[] filtra = DefineVolumeFilters();

            for (int i = 0; i < current_bar.Bars.Count; i++)
            {
                double N_claster = Vars.MathRound((current_bar.bar_highs - current_bar.Bars[i].price) / price_step);
                double current_volume = current_bar.Bars[i].volume;
                string si = i.ToString();
                SetVolumeLabel(current_bar, N_claster, current_volume, si);
                Set_color_filter(current_bar, N_claster, current_volume, si, filtra[0], filtra[1], filtra[2], m_v_b);
            }
        }

        private void SetVolumeLabel(Chart current_bar, double N_claster, double current_volume, string si)
        {
            if (!isClusterChart) return;

            int i = Convert.ToInt32(si);
            Label label_volume = new Label();

            current_bar.cnv_lbls.Children.Add(label_volume);
            current_bar.volume_labels.Add(label_volume);

            Canvas.SetTop(label_volume, N_claster * 12 - 3);
            Canvas.SetRight(label_volume, 0);
            label_volume.Foreground = Brushes.White;
            label_volume.Content = volumeToString(current_volume);// ((decimal)current_volume).ToString();
            label_volume.Padding = new Thickness(0, 0, 1, 0);
            label_volume.Height = 14;
            label_volume.Width = 80;
            label_volume.HorizontalContentAlignment = HorizontalAlignment.Right;
        }

        private string volumeToString(double volume)
        {
            string labelContent = ((decimal)volume).ToString();
            if (volumeRazrdn > 0)
            {
                labelContent = labelContent.Replace('.', ',');
                if (!labelContent.Contains(','))
                    labelContent += ",";

                string[] splitLine = labelContent.Split(',');
                for (int z = 0; z < volumeRazrdn - splitLine[splitLine.Length - 1].Length; z++)
                    labelContent += "0";
            }

            return labelContent;
        }

        private void Set_color_filter(Chart current_bar, double N_claster, double current_volume, string si, double filter_1, double filter_2, double filter_3, double m_v_b)
        {
            int i = Convert.ToInt32(si);

            if (filter_1 > 0 && current_volume >= filter_1)
            {
                Rectangle filter = set_canvas_for_filters(current_bar, N_claster, null, current_volume, current_bar.Bars[i].price, i);
                filter.Fill = (Brush)bc.ConvertFrom("#FF0000FF");
                filter.Fill.Freeze();

                if (isClusterChart)
                {
                    current_bar.volume_labels[i].Background = (Brush)bc.ConvertFrom("#FF0000FF");
                    current_bar.volume_labels[i].Background.Freeze();
                }
            }
            else if (filter_2 > 0 && current_volume >= filter_2)
            {
                Rectangle filter = set_canvas_for_filters(current_bar, N_claster, null, current_volume, current_bar.Bars[i].price, i);
                filter.Fill = (Brush)bc.ConvertFrom("#FF1564D8");
                filter.Fill.Freeze();

                if (isClusterChart)
                {
                    current_bar.volume_labels[i].Background = (Brush)bc.ConvertFrom("#FF1564D8");
                    current_bar.volume_labels[i].Background.Freeze();
                }
            }
            else if (filter_3 > 0 && current_volume >= filter_3)
            {
                Rectangle filter = set_canvas_for_filters(current_bar, N_claster, null, current_volume, current_bar.Bars[i].price, i);
                filter.Fill = (Brush)bc.ConvertFrom("#FF00C0DE");
                filter.Fill.Freeze();

                if (isClusterChart)
                {
                    current_bar.volume_labels[i].Background = (Brush)bc.ConvertFrom("#FF00C0DE");// ("#FF0000FF");
                    current_bar.volume_labels[i].Background.Freeze();
                }
            }
            else if (max_volume_in_bar && m_v_b == current_volume)
            {
                Rectangle filter = set_canvas_for_filters(current_bar, N_claster, null, current_volume, current_bar.Bars[i].price, i);
                filter.Fill = (Brush)bc.ConvertFrom("#FF8000FF");
                filter.Fill.Freeze();

                if (isClusterChart)
                {
                    current_bar.volume_labels[i].Background = (Brush)bc.ConvertFrom("#FF8000FF");
                    current_bar.volume_labels[i].Background.Freeze();
                }
            }
            else
            {
                if (isClusterChart)
                {
                    Brush current_bar_background;
                    if (current_bar.color == -5)
                        current_bar_background = (Brush)bc.ConvertFrom("#FF4D4D55"); //Brushes.Gray;// (Brush)bc.ConvertFrom("#FFB27B00");
                    else
                        current_bar_background = Brushes.Black;

                    current_bar.volume_labels[i].Background = current_bar_background;
                    current_bar.volume_labels[i].Background.Freeze();
                }
                
                    Rectangle filter = set_canvas_for_filters(current_bar, N_claster, null, current_volume, current_bar.Bars[i].price, i);
                    filter.Fill = Brushes.Gray;// (Brush)bc.ConvertFrom("#FFB27B00");
                    filter.Fill.Freeze();
            }
        }

        private Rectangle set_canvas_for_filters(Chart current_bar, double N_claster, Canvas in_border, double current_volume, double current_price, int N_cell)
        {
            if (current_bar.canv_filters == null)
            {
                if (in_border == null)
                    in_border = (Canvas)current_bar.chart_b.Child;

                Canvas for_filters = new Canvas();
                for_filters.Width = max_width_bar - 1;
                for_filters.Height = current_bar.bar_height_pips_count * 12 - 1;
                current_bar.filters = for_filters;
                in_border.Children.Add(for_filters);
                current_bar.canv_filters = new List<ClusterProfile>();
            }

            Rectangle filter = new Rectangle();
            
            filter.Width = current_volume / bars_max_volume * (max_width_bar - 1);
            filter.Height = 10;
            Canvas.SetTop(filter, N_claster * 12 + 1);

            filter.SnapsToDevicePixels = true;
            string cntnt = ((decimal)current_volume).ToString(Vars.FormatInfo);

            filter.ToolTip = cntnt;
            current_bar.filters.Children.Add(filter);
            current_bar.canv_filters.Add(new ClusterProfile { fltr = filter, price = current_price });

            return filter;
        }
        
        private double[] DefineVolumeFilters()
        {
            double filter_1 = 0, filter_2 = 0, filter_3 = 0;

                if (isAutoDetermineVolumeFilters_for_Clusres)
                {
                    filter_1 = auto_volume_filter_for_cluster_1;
                    filter_2 = auto_volume_filter_for_cluster_2;
                    filter_3 = auto_volume_filter_for_cluster_3;
                }
                else
                {
                    filter_1 = volume_filter_for_cluster_1;
                    filter_2 = volume_filter_for_cluster_2;
                    filter_3 = volume_filter_for_cluster_3;
                }

            return new double[] { filter_1, filter_2, filter_3};
        }

        #endregion

        #region EditChart

        //Edit Price Scale
        private void Create_Price_Axis()//рисуем ценовую(вертикальную) шкалу
        {
            int kfc = transfrm_steps[0];
            double current_max = chart.Max(ch => ch.bar_highs_double);

            double max_price_scale = CalculateNewMaxPriceScale(current_max, kfc);

            double current_min = chart.Min(ch => ch.bar_lows_double);
            double min_price_scale = CalculateNewMinPriceScale(current_min, max_price_scale, kfc);

            long hit = (long)((max_price_scale - min_price_scale) / price_step) + 1;//просчитыв высоту ценовой шкалы в тиках
            double current_price = Vars.MathRound(max_price_scale + price_step, rzrdn);
            kfc = transfrm_steps[N_visibl_prc_label];

            for (int x = 0; x < hit; x++)//цыкл соответственно к-ву тиков от мах до мин
            {
                current_price = Vars.MathRound(current_price - price_step, rzrdn);//вычисляем текущую цену
                price_canvases.Add(new PriceScale { price = current_price});

                if (!(x % transfrm_steps[0] == 0 || x % transfrm_steps[1] == 0))
                    continue;

                Label new_label = new Label();
                price_canvases.Last().price_label = new_label;
                Canvas.SetTop(new_label, PriceScaleLabelSetTop(x.ToString()));
                string sx = x.ToString();
                CreateDrawCurrentPriceLabel(new_label, current_price, sx, kfc);
            }
        }

        private void Add_Price_Axis(double max, double minm)
        {
            double bufer_max = price_canvases[0].price, bufer_min = price_canvases.Last().price;

            if (max > bufer_max - 100 * price_step)
            {
                indx = Calculate_Vertical_Central_Cell();

                int kfc = transfrm_steps[0];

                double new_max_price_scale = CalculateNewMaxPriceScale(max, kfc);

                int hit = Convert.ToInt32((new_max_price_scale - bufer_max) / price_step);//просчитыв высоту ценовой шкалы в тиках
                kfc = transfrm_steps[N_visibl_prc_label];
                double current_price = bufer_max;

                for (int x = hit - 1; x >= 0; x--)
                {
                    current_price = Vars.MathRound(current_price + price_step, rzrdn);
                    price_canvases.Insert(0, new PriceScale { price = current_price });

                    if (!(x % transfrm_steps[0] == 0 || x % transfrm_steps[1] == 0))
                        continue;

                    Label new_label = new Label();
                    price_canvases[0].price_label = new_label;
                    string sx = x.ToString();
                    CreateDrawCurrentPriceLabel(new_label, current_price, sx, kfc);
                }

                NewMaxPriseScaleReDrawChart(hit);
            }

            if (minm < bufer_min + 100 * price_step && bufer_min > 0)
            {
                int kfc = transfrm_steps[0];

                double new_min_price_scale = CalculateNewMinPriceScale(minm, price_canvases[0].price, kfc);

                int hit = Convert.ToInt32((bufer_min - new_min_price_scale) / price_step);
                int cicles = price_canvases.Count + hit;
                kfc = transfrm_steps[N_visibl_prc_label];
                double current_price = bufer_min;

                for (int x = price_canvases.Count; x < cicles; x++)
                {
                    current_price = Vars.MathRound(current_price - price_step, rzrdn);
                    price_canvases.Add(new PriceScale { price = current_price });

                    if (!(x % transfrm_steps[0] == 0 || x % transfrm_steps[1] == 0))
                        continue;

                    Label new_label = new Label();
                    price_canvases.Last().price_label = new_label;
                    Canvas.SetTop(new_label, PriceScaleLabelSetTop(x.ToString()));
                    string sx = x.ToString();
                    CreateDrawCurrentPriceLabel(new_label, current_price, sx, kfc);
                }
            }

            DrawHorizontalLines();//дорисовываем горизонтальные линии, если в массиве для линий есть такие, до кот раньше ценовая шкала не доходила, а теперь дошла
        }

        private double CalculateNewMaxPriceScale(double current_max, int kfc)
        {
            int rew = (kfc + 3) * 100;

            double max_price_scale = current_max + rew * price_step;
            int koef = 1;
            if (rzrdn > 0)
                koef = Convert.ToInt32(Math.Pow(10, rzrdn));

            int hai;
            if (kfc == 4)
                hai = Convert.ToInt32(price_step * 200 * koef);
            else hai = Convert.ToInt32(price_step * 500 * koef);

            for (int i = 0; i < rew; i++)
            {
                max_price_scale = Vars.MathRound(max_price_scale - price_step, rzrdn);//мах цена на ценовой шкале должна быть кратной 400-тa или 500-та price_step, чтобы начиная от неё красиво скрывались ценовые лейблы при вертикальном масштабировании 
                double m = max_price_scale * koef;
                long mpc = (long)m;
                if (mpc % hai == 0)//в цыкле находим эту цену
                    break;
            }

            return max_price_scale;
        }

        private double CalculateNewMinPriceScale(double current_min, double max_price_scale, int kfc)
        {
            double min_price_scale = Vars.MathRound(current_min - price_step * 300, rzrdn);//мин ценовой шкалы на 300 тиков ниже от минимума графика
            min_price_scale = Vars.MathRound(max_price_scale - Math.Ceiling((max_price_scale - min_price_scale) / price_step / (kfc * 100)) * kfc * 100 * price_step, rzrdn);//минимум шкалы тоже делаем кратным 400 или 500 price_step
            if (min_price_scale < 0)
                min_price_scale = 0;

            return min_price_scale;
        }

        private void CreateDrawCurrentPriceLabel(Label new_label, double current_price, string sx, int kfc)
        {
            int x = Convert.ToInt32(sx);

            if (x % kfc != 0)
                new_label.Visibility = Visibility.Hidden;

            string labelContent = doubleToString(current_price, false);
            new_label.Content = labelContent;
            Canvas.SetLeft(new_label, -1);
            new_label.Background = Brushes.Transparent;
            new_label.Foreground = Brushes.LightGray;
            new_label.Padding = new Thickness(0);

            canvas_prices.Children.Add(new_label); //текущую цену добавляем в канвас - рисуем ценовую(вертикальную) шкалу 
        }

        private string doubleToString(double current_price, bool isPriceCursor)
        {
            string labelContent = "";
            if (!isPriceCursor)
                labelContent = "-";
            labelContent += ((decimal)current_price).ToString();
            if (rzrdn > 0)
            {
                labelContent = labelContent.Replace('.', ',');
                if (!labelContent.Contains(','))
                    labelContent += ",";

                string[] splitLine = labelContent.Split(',');
                for (int z = 0; z < rzrdn - splitLine[splitLine.Length - 1].Length; z++)
                    labelContent += "0";
            }

            return labelContent;
        }

        private void NewMaxPriseScaleReDrawChart(int hit)
        {
            for (int i = 0; i < price_canvases.Count; i++)//задаём новые координыты ценовой шкале
                if (price_canvases[i].price_label != null)
                    Canvas.SetTop(price_canvases[i].price_label, PriceScaleLabelSetTop(i.ToString()));

            PriceCursor.N_cell += hit;
            PriceCursorSetTop();//указатель текущей цены 

            if (GorizontalActivLine != null)
                GorizontalActivLine.N_cell += hit;

            foreach (ChartLines current_line in lines_Y)
                if (current_line.line_body.Parent != null)
                    current_line.N_cell += hit;

            Resize_horizontal_line();//меняем кординаты линии

            foreach (Chart current_bar in chart)
            {
                current_bar.N_cel_in_Price_scale_list += hit;
                current_bar.candle_body_N_cell += hit;
            }

            //меняем кординаты баров
            for (int z = N_fb; z <= N_lb; z++)
            {
                if (chart[z].bar_highs == 0) continue;//если это геп

                double position_y;

                    if (bufer_x > 8 &&chart_mas_y >= 3)
                        position_y = Calculate_bar_position_top(chart[z].N_cel_in_Price_scale_list);
                    else
                    {
                        double posit_Y = Calculate_candle_shadow_position_top(chart[z]);
                        Canvas.SetTop(chart[z].shadow, posit_Y);
                        position_y = Calculate_candle_body_position_top(chart[z]);
                    }

                Canvas.SetTop(chart[z].chart_b, position_y);
            }

            foreach (Histogramma current_bar in MarcetProfile)
                current_bar.N_cel_in_Price_scale_list += hit;

            Vertical_transform_horizontal_histogram();

            indx += hit;

            pos_y = Math.Round(border_chart.Height / 2 - Canvas.GetTop(price_canvases[indx].price_label), MidpointRounding.AwayFromZero);
            pos_x = canvas_chart.Margin.Left;
            canvas_chart.Margin = new Thickness(pos_x, pos_y, 0, 0); //задаем  положение для canvas-график в бордере
            canvas_prices.Margin = new Thickness(0, pos_y, 0, 0); //задаем  положение для какнваса-ценовой_шкалы в бордере
            LinesCanvas.Margin = new Thickness(0, pos_y, 0, 0);
            canvas_gistogramm.Margin = new Thickness(0, pos_y, 0, 0);
        }


        //Edit Bars, TimeScale, VerticalHistogram
        private void edit_chart(int strt, int nd, int N_first_edit_bar, bool isNeed_draw_vertcal_gstgrm)
        {
            try
            {
                //Stopwatch sw = new Stopwatch();//чтобы замерять время выполнения какого-либо участка кода
                //sw.Start();
                //sw.Stop();
                //MessageBox.Show(sw.ElapsedMilliseconds.ToString());
                //sw.Reset();

                double positions_X, bufer_gh, bufer_df;
                if (canvas_chart.Children.Count == 0)//если нарисованного графика нет, то задаём начальные координаты
                {
                    positions_X = -bufer_x;
                    bufer_df = 100000;
                    bufer_gh = 100000;
                }
                else//если есть нарисованный график
                {
                    if (chart[strt - 1].chart_b.Parent == null)
                        strt = chart.FindLastIndex(strt - 1, ch => ch.chart_b.Parent != null) + 1;

                    if (strt > 0)
                    {
                        positions_X = Canvas.GetLeft(chart[strt - 1].chart_b);
                        int f = chart.FindLastIndex(strt - 1, strt - N_first_edit_bar, delegate (Chart ch) { return ch.time_labels != null && ch.time_labels.Visibility == Visibility.Visible; });
                        if (f >= 0)
                            bufer_df = Canvas.GetLeft(chart[f].time_labels);
                        else bufer_df = 100000;

                        f = chart.FindLastIndex(strt - 1, strt - N_first_edit_bar, delegate (Chart ch) { return ch.date_Labels != null && ch.date_Labels.Visibility == Visibility.Visible; });
                        if (f >= 0)
                            bufer_gh = Canvas.GetLeft(chart[f].date_Labels);
                        else bufer_gh = 100000;
                    }
                    else
                    {
                        positions_X = -bufer_x;
                        bufer_df = 100000;
                        bufer_gh = 100000;
                    }
                }

                double bar_width = bufer_x - 1;
                //if (bufer_x == 1) bar_width = 1;

                double posit_time_label = Math.Round(positions_X - otstup + 4 + bar_width / 2, MidpointRounding.AwayFromZero), posit_date_label = Math.Round(positions_X - otstup + 1 + bar_width / 2, MidpointRounding.AwayFromZero);

                for (int i = strt; i <= nd; i++)//выводим(рисуем) новые бары
                {
                    positions_X += bufer_x;
                    set_bar(chart[i], positions_X, bar_width);

                    if(chart[i].shadow.Parent != null)
                        canvas_chart.Children.Remove(chart[i].shadow);

                    canvas_chart.Children.Add(chart[i].shadow);

                    if (chart[i].chart_b.Parent != null)
                        canvas_chart.Children.Remove(chart[i].chart_b);

                    canvas_chart.Children.Add(chart[i].chart_b);

                    posit_time_label += bufer_x;
                    posit_date_label += bufer_x;

                    if (vertical_gist && isNeed_draw_vertcal_gstgrm)
                    {
                        if (vertical_histogram_type_buffer == VertcalHistogramType.CumulativeDelta)
                        {
                            VerticalHistogramLineSetLeft(i.ToString(), positions_X, bar_width);
                            if (chart[i].VerticalHistogramLine.Parent != null)
                                canv_vert_gstgr.Children.Remove(chart[i].VerticalHistogramLine);

                            canv_vert_gstgr.Children.Add(chart[i].VerticalHistogramLine);
                        }
                        else
                        {
                            chart[i].total_vol_gist.Width = bar_width;
                            Canvas.SetLeft(chart[i].total_vol_gist, positions_X);
                            if (chart[i].total_vol_gist.Parent != null)//когда после закрытия цветового меню мыша залипает на масштабировании то сдесь вылетает исключение
                                canv_vert_gstgr.Children.Remove(chart[i].total_vol_gist);

                            canv_vert_gstgr.Children.Add(chart[i].total_vol_gist);
                        }
                    }

                    if (chart[i].time_labels != null)
                    {
                        Canvas.SetLeft(chart[i].time_labels, posit_time_label);
                        canvas_times.Children.Add(chart[i].time_labels);
                        bufer_df = visiblity_hidden_time_label(chart[i], posit_time_label, bufer_df, false);
                    }

                    if (chart[i].date_Labels != null)
                    {
                        Canvas.SetLeft(chart[i].date_Labels, posit_date_label);
                        canvas_times.Children.Add(chart[i].date_Labels);
                        string si = i.ToString();
                        bufer_gh = visiblity_hidden_date_label(chart[i].date_Labels, posit_date_label, bufer_gh, false, chart[i].time_of_bar, si);
                    }
                }
            }
            catch { }
        }
        
        private void set_bar(Chart current_bar, double positions_X, double bar_width)
        {
            if (current_bar.bar_highs != 0)//если это не геп
            {
                double[] position_y_body_Height = set_bar_or_candle(current_bar, positions_X, bar_width);
                set_filters(current_bar, bar_width);
                Canvas.SetTop(current_bar.chart_b, position_y_body_Height[0]);
                current_bar.chart_b.Height = position_y_body_Height[1];
                current_bar.chart_b.Width = bar_width;//указываем ширину бара в пикселях
                set_cluster_chart_lables_visiblity(current_bar);
            }

            Canvas.SetLeft(current_bar.chart_b, positions_X);
        }

        private void VerticalHistogramLineSetLeft(string iString, double positions_X, double bar_width)
        {
            int i = Convert.ToInt32(iString);
            if (chart[i].VerticalHistogramLine == null) return;
            chart[i].VerticalHistogramLine.X2 = Math.Floor(positions_X + bar_width / 2);
            int n_cell = -1;
            if (i > 0)
                n_cell = chart.FindLastIndex(i - 1, delegate (Chart bar) { return bar.bar_highs > 0; });


            if (n_cell >= 0)
                chart[i].VerticalHistogramLine.X1 = chart[i].VerticalHistogramLine.X2 - bufer_x * (i - n_cell);
            else
                chart[i].VerticalHistogramLine.X1 = chart[i].VerticalHistogramLine.X2;
        }

        private void set_cluster_chart_lables_visiblity(Chart current_bar)
        {
            if (current_bar.cnv_lbls != null)//if(isClusterChart && isDrawLabels_in_Bar)
            {
                if (chart_mas_y >= 12 && bufer_x >= max_width_bar + 1)
                    current_bar.cnv_lbls.Visibility = Visibility.Visible;//скрываем или выводим лейблы внутри баров
                else current_bar.cnv_lbls.Visibility = Visibility.Hidden;
            }
        }

        private double[] set_bar_or_candle(Chart current_bar, double positions_X, double bar_width)
        {
            double body_Height, position_y;

            if (bufer_x > 8)// && chart_mas_y >= 3)//eсли выводим бары
            {
                body_Height = Calculate_bar_height(current_bar);
                position_y = Calculate_bar_position_top(current_bar.N_cel_in_Price_scale_list);//координата У текущего бара
                current_bar.shadow.Visibility = Visibility.Hidden;
            }
            else//если выводим свечи
            {
                current_bar.shadow.Height = Calculate_candle_shadow_height(current_bar);
                double posit_Y = Calculate_candle_shadow_position_top(current_bar);
                Canvas.SetTop(current_bar.shadow, posit_Y);
                Canvas.SetLeft(current_bar.shadow, Math.Floor(positions_X + bar_width / 2));
                body_Height = Calculate_candle_body_height(current_bar);
                position_y = Calculate_candle_body_position_top(current_bar);
                current_bar.shadow.Visibility = Visibility.Visible;
            }

            return new double[] { position_y, body_Height};
        }

        private void set_filters(Chart current_bar, double bar_width)
        {
            if (current_bar.canv_filters != null && current_bar.canv_filters.Count > 0)//если в баре есть цветовые фильтрa
            {
                if (bufer_x <= 8 || (isClusterChart && bufer_x >= max_width_bar + 1 && chart_mas_y >= 12))//  candle - ClusterChart
                {
                    current_bar.chart_b.BorderThickness = new Thickness(1);
                    current_bar.filters.Visibility = Visibility.Hidden;
                    current_bar.chart_b.BorderBrush = Brushes.LightGray;
                    if (current_bar.color == -6)
                        current_bar.chart_b.Background = Brushes.Black;
                    else if (bufer_x <= 8)//свечи
                        current_bar.chart_b.Background = Brushes.Gray;
                    else//бары
                        current_bar.chart_b.Background = (Brush)bc.ConvertFrom("#FF4D4D55");
                }
                else//если выводим ClusterProfile
                {
                    current_bar.chart_b.BorderBrush = (Brush)bc.ConvertFrom("#FF4D4D55");
                    current_bar.chart_b.BorderThickness = new Thickness(1, 0.4, 0.3, 0.2);
                    current_bar.chart_b.Background = Brushes.Transparent;
                    current_bar.filters.Visibility = Visibility.Visible;
                    RenderTransformCurrentBarClusterProfile(current_bar, bar_width, max_width_bar);
                }
            }
            //else if (current_bar.color == -6)
                //current_bar.chart_b.Background = Brushes.Black;
            else if (current_bar.color == -5)
            {
                if (bufer_x <= 8 || chart_mas_y < 3)
                    current_bar.chart_b.Background = Brushes.Gray;
                else
                    current_bar.chart_b.Background = (Brush)bc.ConvertFrom("#FF4D4D55");
            }
        }

        private double Calculate_candle_shadow_height(Chart current_bar)
        {
            return (current_bar.bar_height_pips_count - 1) * chart_mas_y;
        }

        private double Calculate_candle_shadow_position_top(Chart current_bar)
        {
            return Math.Round((price_canvases[0].price - current_bar.bar_highs) / price_step * chart_mas_y + 6, MidpointRounding.AwayFromZero);
        }

        private double Calculate_candle_body_height(Chart current_bar)
        {
            return Math.Round(current_bar.candle_body_height_pips * chart_mas_y, MidpointRounding.AwayFromZero) + 1;
        }

        private double Calculate_candle_body_position_top(Chart current_bar)
        {
            return Math.Round(current_bar.candle_body_N_cell * chart_mas_y + 6, MidpointRounding.AwayFromZero);
        }

        private double Calculate_bar_height(Chart current_bar)
        {
            return Math.Round(current_bar.bar_height_pips_count * chart_mas_y, MidpointRounding.AwayFromZero) + 1;
        }

        private double Calculate_bar_position_top(double N_cell)
        {
            double position_top = Calculate_bar_position_top_1(N_cell);
            return Math.Round(position_top, MidpointRounding.AwayFromZero);
        }

        private double Calculate_bar_position_top_1(double N_cell)
        {
            return N_cell * chart_mas_y + 6 - chart_mas_y / 2;
        }

        private double visiblity_hidden_time_label(Chart current_bar, double posit_time_label, double bufer_df, bool revers)
        {
            if (interval == 1440)//выводим даты
            {
                if (bufer_df == 100000 || (!revers && posit_time_label - bufer_df >= distance_between_visible_time_labels) || (revers && bufer_df - posit_time_label >= distance_between_visible_time_labels))
                {
                    current_bar.time_labels.Visibility = Visibility.Visible;
                    bufer_df = posit_time_label;
                }
                else current_bar.time_labels.Visibility = Visibility.Hidden;
            }
            else if (bufer_df == 100000 || (!revers && posit_time_label - bufer_df >= distance_between_visible_time_labels) || (revers && bufer_df - posit_time_label >= distance_between_visible_time_labels))
            {
                if (interval > 5)
                {
                    current_bar.time_labels.Visibility = Visibility.Visible;
                    bufer_df = posit_time_label;
                }
                else if (interval == 5)
                {
                    if (bufer_x < 20)
                    {
                        if (current_bar.time_of_bar.Value.Minute == 0)
                        {
                            current_bar.time_labels.Visibility = Visibility.Visible;
                            bufer_df = posit_time_label;
                        }
                        else current_bar.time_labels.Visibility = Visibility.Hidden;
                    }
                    else //if (chart[i].time_of_bar.Value.Minute % 30 == 0)
                    {
                        current_bar.time_labels.Visibility = Visibility.Visible;
                        bufer_df = posit_time_label;
                    }
                }
                else// if (interval == 1)
                {
                    if (bufer_x < 4)//interval=1, 30-ти минутки скрываем, чтобы не скрывались часовики
                    {
                        if (current_bar.time_of_bar.Value.Minute == 0)
                        {
                            current_bar.time_labels.Visibility = Visibility.Visible;
                            bufer_df = posit_time_label;
                        }
                        else current_bar.time_labels.Visibility = Visibility.Hidden;
                    }
                    else if (bufer_x < 7)
                    {
                        if (current_bar.time_of_bar.Value.Minute % 30 == 0)
                        {
                            current_bar.time_labels.Visibility = Visibility.Visible;
                            bufer_df = posit_time_label;
                        }
                        else current_bar.time_labels.Visibility = Visibility.Hidden;
                    }
                    else if (bufer_x < 12)
                    {
                        if (current_bar.time_of_bar.Value.Minute % 15 == 0)
                        {
                            current_bar.time_labels.Visibility = Visibility.Visible;
                            bufer_df = posit_time_label;
                        }
                        else current_bar.time_labels.Visibility = Visibility.Hidden;
                    }
                    else if (bufer_x < 22)
                    {
                        if (current_bar.time_of_bar.Value.Minute % 10 == 0)//30-ти минутки выводим, т.к. всё помещается и 30-ти минутки и часовики
                        {
                            current_bar.time_labels.Visibility = Visibility.Visible;
                            bufer_df = posit_time_label;
                        }
                        else current_bar.time_labels.Visibility = Visibility.Hidden;
                    }
                    else //if (chart[i].time_of_bar.Value.Minute % 5 == 0)
                    {
                        current_bar.time_labels.Visibility = Visibility.Visible;
                        bufer_df = posit_time_label;
                    }
                }
            }
            else current_bar.time_labels.Visibility = Visibility.Hidden;

            return bufer_df;
        }

        private double visiblity_hidden_date_label(Label current_date_label, double posit_date_label, double bufer_gh, bool revers, DateTime? time_of_bar, string si)
        {
            int i = Convert.ToInt32(si);

            double rasstoyanie = 41;

            if (interval == 1440)
            {
                //выводим год
                if (i == 0)// если это первый бар, то год возле него возможно будем скрывать, если рядом идёт бар от 1-2-3 января, т.е. с увеличившимся годом, чтобы вывелся(не скрылся) год возле этого январского бара со следующего года
                {
                    if (!revers)
                    {
                        int id = Convert.ToInt32(rasstoyanie / bufer_x) + Convert.ToInt32(rasstoyanie / bufer_x / 7) * 2;//определяем размер проверяемого диапазона диапазона на наличие увеличения года(2-3 недели, т.е. 15-20 баров)
                        if (id > chart.Count - 1) id = chart.Count - 1;
                        if (chart.GetRange(1, id).FindIndex(delegate(Chart ch) { return ch.date_Labels != null; }) == -1)//если увеличения года нет то не скрывем лейбл-год у первого бара
                        {
                            current_date_label.Visibility = Visibility.Visible;
                            bufer_gh = posit_date_label;
                        }
                        else//усли увеличение года есть, то скрывем лейбл-год у первого бара
                            current_date_label.Visibility = Visibility.Hidden;
                    }
                    else if (bufer_gh - posit_date_label >= rasstoyanie)
                    {
                        current_date_label.Visibility = Visibility.Visible;
                        bufer_gh = posit_date_label;
                    }
                    else current_date_label.Visibility = Visibility.Hidden;
                }
                else//если это не первый бар то по любому выводим его лейбл с годом
                {
                    current_date_label.Visibility = Visibility.Visible;
                    bufer_gh = posit_date_label;
                }
            }
            else if (!revers)
            {
                if (!(bufer_gh == 100000 || posit_date_label - bufer_gh >= rasstoyanie))
                    current_date_label.Visibility = Visibility.Hidden;
                else
                {
                    bool isPredicate = false;
                    if (i == 0)// && (time_of_bar.Value.Hour > start_session_hour || (time_of_bar.Value.Hour == start_session_hour && time_of_bar.Value.Minute >= start_session_min)))
                        isPredicate = true;

                    double pixels_count_to_next_monday_date_label;

                    if (isPredicate)
                    {
                        int N_cell_of_next_day = chart.FindIndex(i, delegate(Chart bar) { return bar.time_of_bar.Value.Date > time_of_bar.Value.Date; });

                        if (N_cell_of_next_day < 0)
                            pixels_count_to_next_monday_date_label = rasstoyanie;
                        else
                            pixels_count_to_next_monday_date_label = (N_cell_of_next_day - i) * bufer_x;
                    }
                    else
                        pixels_count_to_next_monday_date_label = rasstoyanie;

                    if (pixels_count_to_next_monday_date_label >= rasstoyanie)
                    {
                        current_date_label.Visibility = Visibility.Visible;
                        bufer_gh = posit_date_label;
                    }
                    else 
                        current_date_label.Visibility = Visibility.Hidden;
                }
            }
            else if (bufer_gh == 100000 || bufer_gh - posit_date_label >= rasstoyanie)
            {
                current_date_label.Visibility = Visibility.Visible;
                bufer_gh = posit_date_label;
            }
            else 
                current_date_label.Visibility = Visibility.Hidden;

            return bufer_gh;
        }

        private void revers_edit_chart(int strt, int nd, int N_last_edit_bar, bool isNeed_draw_vertcal_gstgrm)
        {
            try
            {
                double positions_X, bufer_df, bufer_gh;

                if (chart[strt + 1].chart_b.Parent == null)
                    strt = chart.FindIndex(strt + 1, ch => ch.chart_b.Parent != null) - 1;

                if (strt > -1)
                {
                    positions_X = Canvas.GetLeft(chart[strt + 1].chart_b);
                    int f = chart.FindIndex(strt + 1, N_last_edit_bar - strt, delegate (Chart ch) { return ch.time_labels != null && ch.time_labels.Visibility == Visibility.Visible; });
                    if (f >= 0)
                        bufer_df = Canvas.GetLeft(chart[f].time_labels);
                    else bufer_df = 100000;

                    f = chart.FindIndex(strt + 1, N_last_edit_bar - strt, delegate (Chart ch) { return ch.date_Labels != null && ch.date_Labels.Visibility == Visibility.Visible; });
                    if (f >= 0)
                        bufer_gh = Canvas.GetLeft(chart[f].date_Labels);
                    else bufer_gh = 100000;
                }
                else
                {
                    positions_X = bufer_x;
                    bufer_df = 100000;
                    bufer_gh = 100000;
                }

                double bar_width = bufer_x - 1;
                //if (bufer_x == 1) bar_width = 1;

                double posit_time_label = Math.Round(positions_X - otstup + 4 + bar_width / 2, MidpointRounding.AwayFromZero), posit_date_label = Math.Round(positions_X - otstup + 1 + bar_width / 2, MidpointRounding.AwayFromZero);

                for (int i = strt; i >= nd; i--)
                {
                    positions_X -= bufer_x;

                    set_bar(chart[i], positions_X, bar_width);

                    canvas_chart.Children.Insert(0, chart[i].chart_b);

                        canvas_chart.Children.Insert(0, chart[i].shadow);

                    posit_time_label -= bufer_x;
                    posit_date_label -= bufer_x;

                    if (vertical_gist && isNeed_draw_vertcal_gstgrm)
                    {
                        if (vertical_histogram_type_buffer == VertcalHistogramType.CumulativeDelta)
                        {
                            VerticalHistogramLineSetLeft(i.ToString(), positions_X, bar_width);
                            if (chart[i].VerticalHistogramLine.Parent != null)
                                canv_vert_gstgr.Children.Remove(chart[i].VerticalHistogramLine);

                            canv_vert_gstgr.Children.Insert(0, chart[i].VerticalHistogramLine);
                        }
                        else
                        {
                            chart[i].total_vol_gist.Width = bar_width;
                            Canvas.SetLeft(chart[i].total_vol_gist, positions_X);
                            if (chart[i].total_vol_gist.Parent != null)//когда после закрытия цветового меню мыша залипает на масштабировании то сдесь вылетает исключение
                                canv_vert_gstgr.Children.Remove(chart[i].total_vol_gist);

                            canv_vert_gstgr.Children.Insert(0, chart[i].total_vol_gist);
                        }
                    }

                    if (chart[i].time_labels != null)
                    {
                        Canvas.SetLeft(chart[i].time_labels, posit_time_label);
                        canvas_times.Children.Insert(0, chart[i].time_labels);
                        bufer_df = visiblity_hidden_time_label(chart[i], posit_time_label, bufer_df, true);
                    }

                    if (chart[i].date_Labels != null)
                    {
                        Canvas.SetLeft(chart[i].date_Labels, posit_date_label);
                        canvas_times.Children.Insert(0, chart[i].date_Labels);
                        string si = i.ToString();
                        bufer_gh = visiblity_hidden_date_label(chart[i].date_Labels, posit_date_label, bufer_gh, true, chart[i].time_of_bar, si);//, first_bar
                    }
                }
            }
            catch { }
        }
        
        #endregion

        #region Positionirov

        private void Positionirov(bool history, bool isNeed_redraw_vertical_lines)
        {
            try
            {
                int rng = Convert.ToInt32(Math.Ceiling((border_chart.Width - 100) / bufer_x)) + 2;//мах к-во баров, кот мы можем вывести в бордер
                int N_first_edit_bar = chart.Count - rng;//номер ячейки первого расчётного-видимого бара. "Расчётный-видимый" потому, что этот бар ещё не обязательно будет видимый,
                                                         //он может не попадать в бордер по высоте
                if (N_first_edit_bar < 0)
                {
                    N_first_edit_bar = 0;
                    rng = chart.Count;
                }

                double[] result = Calculate_position_Top(history, N_first_edit_bar, rng);

                double poz_y1 = result[0];
                double dlinn = result[1];
                N_first_edit_bar = Convert.ToInt32(result[2]);

                if (N_first_edit_bar > N_lb)//если график силно задвинут вправо и первый видимый теперь бар - fds - не пересекается с последним видимым ранее(до позиционирования) в бордере баром - N_lb, или 
                {//или если это первая отрисовка графика(в Click_12 N_lb = -10 и при первой отрисовке графика fds по-любому > N_lb)

                    if (canvas_prices.Children.Count > 0)//если это не первая отрисовка графика а просто из очень далекого начала графика позиционируем его в конец
                    {
                        canvas_chart.Children.Clear();//то удаляем из канваса все добавленные ранее бары
                        canvas_times.Children.Clear();
                        if (vertical_gist)
                            canv_vert_gstgr.Children.Clear();
                    }

                    edit_chart(N_first_edit_bar, chart.Count - 1, N_first_edit_bar, true);

                    N_fb = N_first_edit_bar;
                    N_lb = chart.Count - 1;

                    if (isNeed_redraw_vertical_lines)
                    {
                        set_vertical_lines_coordinat(lines_X);//меняем координаты вертикальных линий
                    }
                }
                else if (N_first_edit_bar < N_fb)//если графк задвинут влево, т.е. последний бар ближе к центру бордера
                {
                    revers_edit_chart(N_fb - 1, N_first_edit_bar, N_lb, true);
                    N_fb = N_first_edit_bar;
                }
                else//( fds <= N_lb) если график не силно задвинут вправо и первый видимый теперь бар - fds - пересекается с последним добавленным в канвас баром - N_lb
                {
                    if (N_first_edit_bar > N_fb)//если первый расчётный сейчас видимый бар теперь правее от ранее(до позиционирования) первого видимого бара
                    {
                        int remowe_bars = N_first_edit_bar - N_fb;

                        remowe_sleva(remowe_bars);

                        if (vertical_gist)
                            canv_vert_gstgr.Children.RemoveRange(0, remowe_bars);

                        remove_date_time_labels(N_fb, N_first_edit_bar - 1);


                        N_fb = N_first_edit_bar;
                    }

                    if (chart.Count - 1 > N_lb)//если последний расчётный сейчас видимый бар теперь правее от ранее(до позиционирования) последнего видимого бара
                    {
                        edit_chart(N_lb + 1, chart.Count - 1, N_fb, true);
                        N_lb = chart.Count - 1;
                    }
                }

                double poz_x1;
                if (dlinn < border_chart.Width - 200)//если видимый график короче чем ширина бордера минус 200 пиксилей то график располагаем по центру бордера
                    poz_x1 = Math.Round((border_chart.Width + dlinn) / 2 - Canvas.GetLeft(chart[N_lb].chart_b), 0, MidpointRounding.AwayFromZero);
                else// если график шире, то график отступает на 100 пикселей от правой границы бордера
                    poz_x1 = Math.Round(border_chart.Width - 100 - Canvas.GetLeft(chart[N_lb].chart_b), 0, MidpointRounding.AwayFromZero);

                canvas_chart.Margin = new Thickness(poz_x1, poz_y1, 0, 0); //задаем  положение для canvas1-график      
                canvas_prices.Margin = new Thickness(0, poz_y1, 0, 0); //задаем  положение для ценовой шкалы
                LinesCanvas.Margin = new Thickness(0, poz_y1, 0, 0);
                if (isGist)
                    canvas_gistogramm.Margin = new Thickness(0, poz_y1, 0, 0);
                canvas_times.Margin = new Thickness(poz_x1, 0, 0, 0); //задаем  положение для временной шкалы
                canvas_vertical_line.Margin = new Thickness(poz_x1, 0, 0, 0);
                if (vertical_gist)
                    canv_vert_gstgr.Margin = new Thickness(poz_x1, border_chart.Height - VerticalHistohramBarMaxHeight, 0, 0);

                PriceCursorSetLeft(poz_x1);
                visibl_first_date_label();//выводим первый календарный лейбл в левом углу временной шкалы
            }
            catch { }
        }

        private double[] Calculate_position_Top(bool history, int N_first_edit_bar, int rng)
        {
            double poz_y1 = canvas_chart.Margin.Top, brdr_wdth = border_chart.Width, brdr_hght = border_chart.Height;//координаты канваса в бордере, ширина и высота бордера

            double mx = chart.GetRange(N_first_edit_bar, rng).Max(ch => ch.bar_highs_double);//мах цена расчётного-видимого участка графика
            double mn = chart.GetRange(N_first_edit_bar, rng).Min(ch => ch.bar_lows_double);//min цена расчётного-видимого участка графика
            double chart_height = ((mx - mn) / price_step + 1) * chart_mas_y + 1;//высота расчётного-видимого участка графика в пикселях
            int yrv = chart.FindLastIndex(delegate(Chart ch) { return ch.bar_highs_double == mx; });//номер ячейки последнего максимального бара видимой части графика
            double ym = ((price_canvases[0].price - mx) / price_step) * chart_mas_y + 6 - chart_mas_y / 2;//координата по вертикали максимального бара видимой части графика 

            bool ves_vlasit;//идентификатор того влазит ли расчётно-видимый график в бордер по высоте

            if (chart_height > brdr_hght - 20)//если высота расчётного-видимого графика больше чем высота бордера
            {
                double high_last_bar = chart.Last().bar_highs_double;
                double low_last_bar = chart.Last().bar_lows_double;
                double height_N_last_bar = ((high_last_bar - low_last_bar) / price_step + 1) * chart_mas_y + 1;//высота последнего бара в пикселях
                double GetTop_N_last_bar = ((price_canvases[0].price - high_last_bar) / price_step) * chart_mas_y + 6 - chart_mas_y / 2;//координата У последн бара
                if (height_N_last_bar > brdr_hght - 20)//если последний бар не влазит в бордер по высоте
                {
                    ves_vlasit = true;//если последний бар не влазит в бордер по высоте то и урезанный график потом не будет иметь смысла перепроверять на помещаемость в бордер по высоте

                    double last_price_pos_y = Calculate_N_cel_in_Price_scale_list(chart.Last().close_price) * chart_mas_y + 6;//координата У последней цены
                    if (history)// если рисуем историю
                        poz_y1 = brdr_hght / 2 - last_price_pos_y;//то цену закрытия последнего бара(текущую цену) ставим по центру бордера
                        //poz_y1 = brdr_hght / 2 - GetTop_N_last_bar - height_N_last_bar / 2;//средину последнего не помещающегося бара ставим по центру бордера
                    else//если это реалтайм-обновление график, т.е если этот метод вызван из метода realtime_ed_chart()
                        if (last_price_pos_y + poz_y1 + chart_mas_y > brdr_hght - 10 || poz_y1 + last_price_pos_y < 10)//если цена закрытия последнего бара не видима - вышла за границы бордера
                            poz_y1 = brdr_hght / 2 - last_price_pos_y;//то цену закрытия последнего бара(текущую цену) ставим по центру бордера 
                }
                else//если последний бар помещается в бордер по высоте то он обязательно весь должен быть видимым
                {
                    ves_vlasit = false;//идентифицируем, что расчётно-видимый график не влазит в бордер по высоте
                    int yrm = chart.FindLastIndex(delegate(Chart ch) { return ch.bar_lows_double == mn; });//номер ячейки последнего минимального бара видимой части графика

                    if (yrv > yrm || (yrv == yrm && mx - low_last_bar >= high_last_bar - mn))//если мах видимой части ближе к последнему бару чем минимум графика, или и мин и мах графика в одном баре, но мах графика ближе к мin последнего бара чем мин графика к мax последнего бара
                    {
                        if (((mx - low_last_bar) / price_step + 1) * chart_mas_y + 1 <= brdr_hght - 20 && (yrv != N_first_edit_bar || yrv == 0))//если мах видимой части графика вывести в бордер, и последний бар остаётся видимым, т.е. его мin не скрывается, 
                        {//причём максимальный бар не является первым-левым видимым в бордере или если он таки первый-левый видимым в бордере но  его индекс =0, т.е. слева больше нету графика не поместившегося в бордер

                            double poz_y_buf = 10 - ym;//координата У, если бы мы поставили график так, чтобы и мах графика и последний бар попадали в бордер

                            bool ne_vlazit = AllBarsVlazi(yrv, poz_y_buf);

                            if (!ne_vlazit)// если таки все бары, от мах до предпоследнего, вместе с последним влазятт в бордер по высоте то ставили график так, чтобы и мах графика и последний бар попадали в бордер
                                poz_y1 = poz_y_buf;
                            else// если таки не все бары, от мах до предпоследнего, влазятт в бордер по высоте то  средину последнего бара ставим по центру бордера
                                poz_y1 = Chart_ne_vlazit(history, poz_y1, GetTop_N_last_bar, height_N_last_bar, brdr_hght, ym, chart_height);

                        }//если мах графика не помещается в бордер вместе с последним баром
                        else
                            poz_y1 = Chart_ne_vlazit(history, poz_y1, GetTop_N_last_bar, height_N_last_bar, brdr_hght, ym, chart_height);

                    }//иначе - минимум видимой части ближе к последнему бару                    
                    else if (((high_last_bar - mn) / price_step + 1) * chart_mas_y <= brdr_hght - 20 && (yrm != N_first_edit_bar || yrm == 0))//если мin видимой части графика вывести в бордер, и последний бар остаётся видимым, т.е. его мax не скрывается
                    {//причём минимальный бар не является первым-левым видимым в бордере или если он таки первый-левый, то его индекс =0, т.е. слева больше нету графика не поместившегося в бордер

                        double ym12 = ((price_canvases[0].price - chart[yrm].bar_lows_double) / price_step + 1) * chart_mas_y - 6 + chart_mas_y / 2;// координата У минимума минимального бара
                        double poz_y_buf = brdr_hght - 10 - ym12;//координата У, если бы мы поставили график так, чтобы и мin графика и последний бар попадали в бордер

                        bool ne_vlazit = AllBarsVlazi(yrm, poz_y_buf);

                        if (!ne_vlazit)//если таки все бары, от мin до предпоследнего, вместе с последним влазятт в бордер по высоте то вертикальную координинату больше не пересчитываем
                            poz_y1 = poz_y_buf;
                        //else если таки не все бары, от мin до предпоследнего, влазятт в бордер по высоте то 
                        else
                            poz_y1 = Chart_ne_vlazit(history, poz_y1, GetTop_N_last_bar, height_N_last_bar, brdr_hght, ym, chart_height);
                    }//если и мин графика не помещается в бордер вместе с последним баром
                    else
                        poz_y1 = Chart_ne_vlazit(history, poz_y1, GetTop_N_last_bar, height_N_last_bar, brdr_hght, ym, chart_height);
                }
            }
            else//если высота видимой части графика меньше чем высота бордера
            {
                ves_vlasit = true;//идентифицируем, что расчётно-видимый график влазит в бордер по высоте
                poz_y1 = Chart_vlazit(history, brdr_hght, ym, chart_height);
            }

            //проверяем, если начальный участок расчётной-видимой части графика в бордер не попадает, то  урезаем расчётно-видимый график и окончательно определяем реально видимый отрезок графиkа
            int re = CalculateFirstVisibleBar(poz_y1, brdr_hght, N_first_edit_bar);//номер первого реально видимого бара

            double dlinn = (chart.Count - re) * bufer_x;//длинна реально видимой части графика

            if (re > N_first_edit_bar)//если первый видимый бар таки не тот, который изначально принимали за расчётный - видимый
            {
                if (dlinn < brdr_wdth - 200)//если реально видимый график короче чем ширина бордера-200, то пересчитываем fds - первый видимый бар
                {
                    N_first_edit_bar = Convert.ToInt32((chart.Count - 1 + re - brdr_wdth / bufer_x) / 2);
                    if (N_first_edit_bar < 0) N_first_edit_bar = 0;
                }

                if (!ves_vlasit)//если график раньше не влазил в бордер по высоте, то теперь проверяем влезет ли урезанный график в бордер по высоте
                {
                    mx = chart.GetRange(re, chart.Count - re).Max(ch => ch.bar_highs_double);//мах цена расчётного-видимого участка графика
                    mn = chart.GetRange(re, chart.Count - re).Min(ch => ch.bar_lows_double);//min цена расчётного-видимого участка графика
                    chart_height = ((mx - mn) / price_step + 1) * chart_mas_y + 1;//высота расчётного-видимого участка графика в пикселях

                    if (chart_height <= brdr_hght - 20)//а если урезанный видимый график теперь помещается в бордер то пересчитываем его координату У - весь видимый график ставим по центру высоты бордера
                    {
                        ym = ((price_canvases[0].price - mx) / price_step) * chart_mas_y + 6 - chart_mas_y / 2;//координата по вертикали максимального бара видимой части графика
                        poz_y1 = Chart_vlazit(history, brdr_hght, ym, chart_height);

                        re = CalculateFirstVisibleBar(poz_y1, brdr_hght, N_first_edit_bar);//номер первого реально видимого бара
                        if(re > N_first_edit_bar)
                        {
                            dlinn = (chart.Count - re) * bufer_x;//длинна реально видимой части графика
                            if (dlinn < brdr_wdth - 200)//если реально видимый график короче чем ширина бордера-200, то пересчитываем fds - первый видимый бар
                            {
                                N_first_edit_bar = Convert.ToInt32((chart.Count - 1 + re - brdr_wdth / bufer_x) / 2);
                                if (N_first_edit_bar < 0) N_first_edit_bar = 0;
                            }
                        }
                    }
                    //else если график всёравно не влазит, то координату У не меняем
                }
                //else если график изначально влазил в бордер по высоте или последний бар не влазит в бордер по высоте то координату У не меняем
            }

            return new double[] { Math.Round(poz_y1, MidpointRounding.AwayFromZero), dlinn, N_first_edit_bar };
        }

        private double Chart_ne_vlazit(bool history, double poz_y1, double GetTop_N_last_bar, double height_N_last_bar, double brdr_hght, double ym, double chart_height)
        {            
            if (history)//если рисуем историю
                poz_y1 = brdr_hght / 2 - GetTop_N_last_bar - height_N_last_bar / 2;// средину последнего бара ставим по центру бордера
            else //если реалтайм 
                if (poz_y1 + GetTop_N_last_bar < 10 || GetTop_N_last_bar + height_N_last_bar + poz_y1 > brdr_hght - 10)//если мин или мах последнего бара не попадает в бордер, то средину последнего бара ставим по центру бордера
                poz_y1 = brdr_hght / 2 - GetTop_N_last_bar - height_N_last_bar / 2;// средину последнего бара ставим по центру бордера
            else if (ym + poz_y1 > brdr_hght / 2 || ym + chart_height + poz_y1 < brdr_hght / 2)//если max графика ниже центра бордера или min графика выше центра бордера
                poz_y1 = brdr_hght / 2 - GetTop_N_last_bar - height_N_last_bar / 2;// средину последнего бара ставим по центру бордера
            //else если реалтайм и последний бар влазит в бордер то ничего не меняем

            return poz_y1;
        }

        private double Chart_vlazit(bool history, double brdr_hght, double ym, double chart_height)
        {
            double poz_y1;
            if (history)//если рисуем историю
                poz_y1 = (brdr_hght - chart_height) / 2 - ym;//по вертикали график ставим по средине бордера                        
            else if (canvas_chart.Margin.Top + ym < 10 || ym + chart_height + canvas_chart.Margin.Top > brdr_hght - 10)//если раньше до пересчёта poz_y1 (пэ canvas_chart.Margin.Top ) мин или мах видимого графика не влазит в бордер
                poz_y1 = (brdr_hght - chart_height) / 2 - ym;//по вертикали график ставим по средине бордера
            else if (ym + canvas_chart.Margin.Top > brdr_hght / 2 || ym + chart_height + canvas_chart.Margin.Top < brdr_hght / 2)//если max графика ниже центра бордера или min графика выше центра бордера
                poz_y1 = (brdr_hght - chart_height) / 2 - ym;//то видимую часть графика по вертикали также позиционирум по центру бордера
            else
                poz_y1 = canvas_chart.Margin.Top;//иначе если реалтайм и ни мин ни мах не выпадают из бордера, то график оставляем на том же месте

            return poz_y1;
        }

        private int CalculateFirstVisibleBar(double poz_y1, double brdr_hght, int N_first_edit_bar)
        {
            int re;//номер первого реально видимого бара
            for (re = N_first_edit_bar; re < chart.Count; re++)
            {
                if (chart[re].bar_highs_double == 0)
                    continue;//если это геп

                double poz_y_tek_bar_1 = ((price_canvases[0].price - chart[re].bar_highs_double) / price_step) * chart_mas_y + 6 - chart_mas_y / 2;//координата по вертикали текущего проверяемого бара
                if (brdr_hght > poz_y_tek_bar_1 + poz_y1 && poz_y1 + poz_y_tek_bar_1 >= 0)//если координата мах бара попадает в диапазон бордера
                    break;
                double poz_y_min_tek_bar = ((price_canvases[0].price - chart[re].bar_lows_double) / price_step + 1) * chart_mas_y - 6 + chart_mas_y / 2;//координата У минимума текущего бара
                if (poz_y_min_tek_bar + poz_y1 <= brdr_hght && poz_y_min_tek_bar + poz_y1 > 0)//или min бара попадает в диапазон бордера
                    break;
                if (poz_y1 + poz_y_tek_bar_1 < 0 && poz_y_min_tek_bar + poz_y1 > brdr_hght)//если высота бара больше высоты бордера и его мах выше бордера а минимум ниже бордера
                    break;
            }

            return re;
        }

        private bool AllBarsVlazi(int strt, double poz_y_buf)
        {
            bool ne_vlazit = false;
            for (int op = strt; op < chart.Count - 1; op++)//проверяем все ли бары, от min до предпоследнего, влазят в бордер по высоте 
            {
                if (chart[op].bar_highs_double == 0) continue;//если это геп

                double poz_y_tek_bar_1 = ((price_canvases[0].price - chart[op].bar_highs_double) / price_step) * chart_mas_y + 6 - chart_mas_y / 2;// координата по вертикали текущего проверяемого бара
                double poz_y_min_tek_bar = ((price_canvases[0].price - chart[op].bar_lows_double) / price_step + 1) * chart_mas_y - 6 + chart_mas_y / 2;// координата У минимума текущего бара
                if (poz_y_buf + poz_y_tek_bar_1 < 15 && poz_y_min_tek_bar + poz_y_buf > 0)//если мах текущего бара выше верхней границы бордера, а мin текущ бара видим в бордере, ато если весь бар  више бордера, то из-за него не будем разрывать цыкл
                {
                    ne_vlazit = true;
                    break;
                }
            }

            return ne_vlazit;
        }

        private void Pozitionir_2(object sender, RoutedEventArgs e)
        {
            if (canvas_prices.Children.Count == 0 || button_press)
                return;

            isMove = false;

            lock (chart)
                Positionirov(true, true);

            pozizionir = true;
            button15.Foreground = Brushes.Red;
        }

        #endregion

        #region  Move Chart

        Point cursor_pos;// = new Point();//переменная для определения начального полож мыши. Испол в Activate_Move, Activate_Move_Y, Activate_Move_Х        
        Point cursor_pos_y;// = new Point();
        double pos_x, pos_y;//координаты обьекта-канваса1, использ в UpdateXY_1, Activate_Move, Mod_Bars_Y, Apdate_Y_1, Activate_Move_Y, Activate_Move_X, Mod_Bars_X, Chart_apdate, Window_SizeChanged
        bool isMove;//переменная указывает - нажата или отпущена левая кнопка мыши на графике(Бордер_2), используется в  Check_Options, Activate_Move. Возвращается в исходн полож в  Stop_Move
        int N_fb_bufer, N_lb_bufer;

        private void Activate_Move() //ф-ция работает, если на Бордер-график нажали левую кнопку мыши
        {            
            if (canvas_prices.Children.Count == 0 || button_press || isMove) return;

            border_chart.CaptureMouse();//захватываем мышу на бордере-графике, чтобы график передвигался даже если мыша(с нажатой левой кнопкой) вышла за пределы окна

            isMove = true;//указываем, что активировано перемещение графика
            cursor_pos = Mouse.GetPosition(this); //определяем начальное положение мыши
            cursor_pos_y = Mouse.GetPosition(this);

            if (bufer_x <= 3) hiet = 0.75;//если расстояние му барами 3 пикселя и меньше, то график будем передвигать на 25% медленнее чем двигается мыша
            else if (bufer_x <= 10) hiet = 1.2;//график двигаем на 20% быстрее мыши
            else hiet = 1.5;//график двигаем на 50% быстрее мыши

            was_move = false;

            lock (chart)
            {
                pos_x = canvas_chart.Margin.Left;//определяем начальное положение  (до перемещения) canvas1 в border
                pos_y = canvas_chart.Margin.Top;
            }
        }

        private void UpdateXY_1()
        {
            Point current_mouse = Mouse.GetPosition(this);//текущее положение мыши при перемещении
            double razn_x = current_mouse.X - cursor_pos.X; //разница в координатах мыши между текущим и начальным положением
            lock (chart)
            {
                pos_y = Math.Round(pos_y + current_mouse.Y - cursor_pos_y.Y, MidpointRounding.AwayFromZero);//cursor_pos - эта переменн заявлена над ф-циями
                cursor_pos_y = current_mouse;
                if (razn_x <= -10)//график двигается влево
                {
                    pozizionir = false;
                    button15.Foreground = Brushes.White;
                    pos_x = Math.Round(pos_x + razn_x * hiet, MidpointRounding.AwayFromZero);//это должно быть внутри условия чтобы график не дрожал когда его задвинули до упора влево или вправо, а дальше двигать прога не даёт
                    double pos_X_last_bar_date = Canvas.GetLeft(chart[N_lb].chart_b);
                    if (pos_x < border_chart.Width / 2 - pos_X_last_bar_date)//чтобы график не задвигали сильно влево
                        pos_x = Math.Round(border_chart.Width / 2 - pos_X_last_bar_date, 0, MidpointRounding.AwayFromZero);

                    cursor_pos = current_mouse;//чтобы график не дрожал когда его задвинули до упора влево или вправо, а дальше двигать прога не даёт

                    if (!was_move && bufer_x <= 4)// && N_fb > 0 && N_lb < chart.Count - 1)
                    {
                        was_move = true;

                        if (vertical_gist)
                        {
                            canv_vert_gstgr.Visibility = Visibility.Hidden;
                            N_fb_bufer = N_fb;
                            N_lb_bufer = N_lb;
                        }

                        if (isGist)
                            canvas_gistogramm.Visibility = Visibility.Hidden;
                    }

                    double remowe_bars_1 = -pos_x - Canvas.GetLeft(chart[N_fb].chart_b);
                    if (remowe_bars_1 >= bufer_x)
                    {
                        int remowe_bars = Convert.ToInt32(Math.Floor(remowe_bars_1 / bufer_x));//удаляем скрывшиеся слева бары
                        if (remowe_bars > canvas_chart.Children.Count)
                            remowe_bars = canvas_chart.Children.Count;

                        remowe_sleva(remowe_bars);
                        remove_date_time_labels(N_fb, N_fb + remowe_bars - 1);
                        visibl_first_date_label();//выводим первый календарный лейбл в левом углу временной шкалы
                        if (!was_move && vertical_gist)
                            canv_vert_gstgr.Children.RemoveRange(0, remowe_bars);

                        N_fb += remowe_bars;
                    }

                    if (N_lb < chart.Count - 1)
                        add_sprava(pos_x, was_move);//дорисовывем справа появляющиеся новые бары
                }
                else if (razn_x >= 10)//график двигается вправо
                {
                    pozizionir = false;
                    button15.Foreground = Brushes.White;
                    pos_x = Math.Round(pos_x + razn_x * hiet, MidpointRounding.AwayFromZero);//это должно быть внутри условия чтобы график не дрожал когда его задвинули до упора влево или вправо, а дальше двигать прога не даёт
                    double pos_X_first_bar_date = Canvas.GetLeft(chart[N_fb].chart_b);
                    if (pos_x > border_chart.Width / 2 - pos_X_first_bar_date)//чтобы график не задвигали сильно впрао
                        pos_x = Math.Round(border_chart.Width / 2 - pos_X_first_bar_date, 0, MidpointRounding.AwayFromZero);

                    cursor_pos = current_mouse;//чтобы график не дрожал когда его задвинули до упора влево или вправо, а дальше двигать прога не даёт

                    if (!was_move && bufer_x <= 4)// && N_fb > 0 && N_lb < chart.Count - 1)
                    {
                        was_move = true;

                        if (vertical_gist)
                        {
                            canv_vert_gstgr.Visibility = Visibility.Hidden;
                            N_fb_bufer = N_fb;
                            N_lb_bufer = N_lb;
                        }
                        
                        if (isGist)
                            canvas_gistogramm.Visibility = Visibility.Hidden;
                    }

                    remove_sprava(pos_x, was_move);//удаляем скрывшиеся справа бары
                    if (N_fb > 0)
                        insert_revers_sleva(pos_x, was_move);//дорисовывем слева новые бары
                }

                pos_y = vverx_vniz(pos_y);//чтобы график не задвигали сильно вверх или вниз
                canvas_chart.Margin = new Thickness(pos_x, pos_y, 0, 0); //задаем новое положение для canvas1-график
                LinesCanvas.Margin = new Thickness(0, pos_y, 0, 0);
                canvas_vertical_line.Margin = new Thickness(pos_x, 0, 0, 0);
                canvas_prices.Margin = new Thickness(0, pos_y, 0, 0); //задаем новое положение для ценовой шкалы
                canvas_times.Margin = new Thickness(pos_x, 0, 0, 0); //задаем новое положение для временной шкалы
                if (!was_move)
                {
                    if (isGist)
                        canvas_gistogramm.Margin = new Thickness(0, pos_y, 0, 0);

                    if (vertical_gist)
                        canv_vert_gstgr.Margin = new Thickness(pos_x, border_chart.Height - VerticalHistohramBarMaxHeight, 0, 0);
                }

                PriceCursorSetLeft(canvas_chart.Margin.Left);
            }
        }

        private void add_sprava(double ps_x, bool was_move_s)
        {
            //этот метод вызывается из UpdateXY_1(), changed_size_window(), realtime_ed_chart()

            double koll_new_bars_1 = -ps_x + border_chart.Width - Canvas.GetLeft(chart[N_lb].chart_b);

            if (koll_new_bars_1 > bufer_x)
            {
                int koll_new_bars = Convert.ToInt32(Math.Floor(koll_new_bars_1 / bufer_x));
                int end_b = N_lb + koll_new_bars;
                if (end_b > chart.Count - 1) end_b = chart.Count - 1;

                if (!was_move_s)
                    edit_chart(N_lb + 1, end_b, N_fb, true);
                else
                    edit_chart(N_lb + 1, end_b, N_fb, false);

                N_lb = end_b;
            }
        }

        private void insert_revers_sleva(double ps_x, bool was_move_s)
        {
            double koll_new_bars_1 = ps_x + Canvas.GetLeft(chart[N_fb].chart_b);
            if (koll_new_bars_1 > 0)
            {
                int koll_new_bars = Convert.ToInt32(Math.Ceiling(koll_new_bars_1 / bufer_x));
                int fds = N_fb - koll_new_bars;
                if (fds < 0) fds = 0;
                int N_fb_do = N_fb;
                if (!was_move_s)
                    revers_edit_chart(N_fb - 1, fds, N_lb, true);
                else
                    revers_edit_chart(N_fb - 1, fds, N_lb, false);

                N_fb = fds;
                visibl_first_date_label();//выводим первый календарный лейбл в левом углу временной шкалы
            }
        }

        private void remove_sprava(double ps_x, bool was_move_s)
        {
            double remowe_bars_1 = Canvas.GetLeft(chart[N_lb].chart_b) + ps_x - border_chart.Width;
            if (remowe_bars_1 >= 0)
            {
                int remowe_bars;
                if (remowe_bars_1 == 0)
                    remowe_bars = 1;
                else
                {
                    remowe_bars = Convert.ToInt32(Math.Ceiling(remowe_bars_1 / bufer_x));
                    if (remowe_bars > canvas_chart.Children.Count)
                        remowe_bars = canvas_chart.Children.Count;
                }

                remove_bars_sprava(remowe_bars);
                remove_date_time_labels(N_lb - remowe_bars + 1, N_lb);
                if (!was_move_s && vertical_gist && canv_vert_gstgr.Children.Count > 0)
                    canv_vert_gstgr.Children.RemoveRange(canv_vert_gstgr.Children.Count - remowe_bars, remowe_bars);

                N_lb -= remowe_bars;
            }
        }

        private void remove_bars_sprava(int remowe_bars)
        {
                canvas_chart.Children.RemoveRange(canvas_chart.Children.Count - remowe_bars * 2, remowe_bars * 2);
        }

        private void remowe_sleva(int remowe_bars)
        {
            
                canvas_chart.Children.RemoveRange(0, remowe_bars * 2);
        }

        private void remove_date_time_labels(int strt, int nd)
        {
            for (int i = strt; i <= nd; i++)
            {
                if (chart[i].time_labels != null)
                    canvas_times.Children.Remove(chart[i].time_labels);
                if (chart[i].date_Labels != null)
                    canvas_times.Children.Remove(chart[i].date_Labels);
            }
        }

        private void Mousemove(object sender, MouseEventArgs e)//двигаем мышу по главному окну
        {
            if (isMove == true)
                UpdateXY_1();//если левая кнопка мыши нажата на графике - то перемещаем его 
            else if (mas_Y == true)
                Apdate_Y_1(); //если левая кнопка мыши нажата на ценовой шкале-масштабируем по ценовой шкале
            else if (mas_X == true)
                Apdate_X_1();//если левая кнопка мыши нажата на временной шкале-масштабируем по оси Х
            else if (cross_move)
            {
                if (GorizontalActivLine != null && VerticalActivLine != null)
                {
                    Move_line_Y();
                    Move_line_X();
                    if (OpenCloseFrame.Parent != null)
                        SetOpenCloseLabel();
                }
                else DeleteCross();
            }
            else if (GorizontalActivLine != null)
                Move_line_Y();
            else if (VerticalActivLine != null)
                Move_line_X();

            lock (Vars.main_windows)
                foreach (ChartWindow chartWindow in Vars.main_windows)
                    if (Vars.IsOnConnection) chartWindow.Background = Brushes.Black;
                    else chartWindow.Background = Brushes.Red;
        }

        private void Stop_Move(object sender, MouseButtonEventArgs e)//ф-ция срабатывает если отпустили вверх левую кнопку миши на Главном окне
        {
            if (isMove)
            {
                if (was_move)
                    finish_UpdateXY_1();

                border_chart.ReleaseMouseCapture();
                isMove = false;
            }
            else if (mas_X)
            {
                if (was_move)
                    finish_masshtab_X();

                if (!(color_choice || VerticalActivLine != null))//если масштабирования не было и будем ставить активную линию или мы её уже поставили, то захват мыши пока не отпускаем
                    border_times.ReleaseMouseCapture();

                mas_X = false;
            }
            else if (mas_Y)
            {
                if (was_move)
                {
                    Vertical_transform_horizontal_histogram();
                    canvas_gistogramm.Margin = new Thickness(0, canvas_chart.Margin.Top, 0, 0);
                    canvas_gistogramm.Visibility = Visibility.Visible;
                }

                if (!(color_choice || GorizontalActivLine != null))
                    border_prices.ReleaseMouseCapture();

                mas_Y = false;
            }

            if (canvas_times.Children.Count > 0 && bufer_x >= max_width_bar + 1 && chart_mas_y >= 12)
                button14.Content = "-";
            else
                button14.Content = "+";

            Cursor = Cursors.Arrow;
        }

        private void finish_UpdateXY_1()
        {
            lock (chart)
            {
                if (vertical_gist)
                {
                    if (N_fb > N_lb_bufer || N_lb < N_fb_bufer)
                    {
                        canv_vert_gstgr.Children.Clear();
                        edit_vertc_gistgr(N_fb, N_lb);
                    }
                    else
                    {
                        if (N_fb_bufer > N_fb)
                            revers_edit_vertc_gistgr(N_fb_bufer - 1, N_fb);
                        else if (N_fb_bufer < N_fb)
                            canv_vert_gstgr.Children.RemoveRange(0, N_fb - N_fb_bufer);

                        if (N_lb_bufer > N_lb)
                        {
                            if (canv_vert_gstgr.Children.Count > 0)
                            {
                                int remowe_bars = N_lb_bufer - N_lb;
                                canv_vert_gstgr.Children.RemoveRange(canv_vert_gstgr.Children.Count - remowe_bars, remowe_bars);
                            }
                        }
                        else if (N_lb_bufer < N_lb)
                            edit_vertc_gistgr(N_lb_bufer + 1, N_lb);
                    }

                    canv_vert_gstgr.Visibility = Visibility.Visible;
                    canv_vert_gstgr.Margin = new Thickness(pos_x, border_chart.Height - VerticalHistohramBarMaxHeight, 0, 0);
                }

                if (isGist)
                {
                    canvas_gistogramm.Margin = new Thickness(0, pos_y, 0, 0);
                    canvas_gistogramm.Visibility = Visibility.Visible;
                }
            }
        }

        private void visibl_first_date_label()//выводим первый календарный лейбл в левом углу временной шкалы
        {
            First_visibl_date_label.Content = "";
            try
            {
                if (N_fb > 0)//если слева ещё есть не поместившиеся в бордер бары
                {
                    IEnumerable<Chart> query = chart.GetRange(N_fb, N_lb - N_fb + 1).Where(dts => dts.date_Labels != null);//опреднляем сколько календарных лейблов уже выведено на графике
                    if (query.Count() < 3 && !chart.GetRange(N_fb, Convert.ToInt32(130 / bufer_x)).Any(ch => ch.date_Labels != null))//если на графике выведено меньше 3 календарных лейблов и  в первых 130 пикселях бордера нету календарного лейбла
                    {
                        DateTime? for_label = chart[N_fb].time_of_bar;
                        string label_cntnt;
                        if (interval == 1440)
                        {
                            label_cntnt = for_label.Value.Year.ToString();
                        }
                        else
                        {
                            label_cntnt = for_label.Value.Day.ToString();
                            if (for_label.Value.Day < 10)
                                label_cntnt = "0" + label_cntnt;
                            string mnf = for_label.Value.Month.ToString();
                            if (for_label.Value.Month < 10)
                                mnf = "0" + mnf;
                            label_cntnt = label_cntnt + "/" + mnf;
                        }

                        First_visibl_date_label.Content = label_cntnt;
                    }
                }
            }
            catch { }
        }

        private double vverx_vniz(double pz_y)//чтобы график не задвигали сильно вверх или вниз
        {
            int rng = N_lb - N_fb + 1;
            List<Chart> Visible_bars = chart.GetRange(N_fb, rng);
            double mx = Visible_bars.Max(ch => ch.bar_highs_double);//мах цена расчётного-видимого участка графика

            if (mx != 0)
            {
                double ym_mx = ((price_canvases[0].price - mx) / price_step) * chart_mas_y + 6 - chart_mas_y / 2;//координата по вертикали максимума видимой части графика

                if (ym_mx + pz_y > border_chart.Height / 2)//чтобы график не задвигали сильно вниз
                    pz_y = Math.Round(border_chart.Height / 2 - ym_mx, 0, MidpointRounding.AwayFromZero);
                else
                {
                    double mn = Visible_bars.Min(ch => ch.bar_lows_double);//min цена расчётного-видимого участка графика
                    double ym_mn = ((price_canvases[0].price - mn) / price_step + 1) * chart_mas_y - 6 + chart_mas_y / 2;// координата по вертикали минимума видимой части графика
                    if (ym_mn + pz_y < border_chart.Height / 2)//чтобы график не задвигали сильно вверх
                        pz_y = Math.Round(border_chart.Height / 2 - ym_mn, 0, MidpointRounding.AwayFromZero);
                }
            }

            return pz_y;
        }

        #endregion

        #region ChartTransform

        int indx;//номер центрального видимого бара при масштабировании по горизонтали или центральная цена при масштабировании по вертикали. Определяется в Activate_Move_X и Activate_Move_Y, исползуется в Mod_Bars_X, Apdate_X_1, Apdate_Y_1
        bool was_move = false;//указывает было ли масштабирование графика. Если было то по этой переменной будем активируем масштабирование гистограммы   
        
        //VerticalChartTransform

        bool mas_Y = false;//переменн указыв - нажата или отпущена левая кнопка мыши на ценовой шкале(Бордер_1), используется в Activate_Move_Y, Check_Options. Возвращается в исходн полож в  Stop_Move
        double hiet;//высота видимого графика в тиках. Испол в Apdate_Y_1.  вычисляется в Activate_Move_Y. И отдельно используется как идентификатор для no_change_chart_pozit при выборе интервала и в Click_12
        double mx = 0;//мах цена видимого графика. Activate_Move_Y, Apdate_Y_1. И отдельно используется как идентификатор для no_change_chart_pozit при выборе интервала  и в Click_12, Activate_Move, UpdateXY_1 

        private void Activate_Move_Y(object sender, MouseButtonEventArgs e)//ф-ция работает, если на Бордере-ценов шкале нажали левую кнопку мыши
        {
            if (canvas_prices.Children.Count == 0 || button_press) return;//если график пока не нарисован

            if (GorizontalActivLine != null)//если есть активная линия то закрепляем её
                secure_activ_gorizontal_line();
            else
            {
                if (mas_Y) return;

                mas_Y = true;//идинтифицируем начало масштабирования
                color_choice = true;//разрешаем создавать горизонтальную линию(если не будет масштабирования)
                was_move = false;
                cursor_pos = Mouse.GetPosition(this); //определяем начальное положение мыши

                lock (chart)
                {
                    indx = Calculate_Vertical_Central_Cell();//вычисляем центральную цену графика по высоте учитывая что канвас мог быть передвинут в бордере
 
                    pos_x = canvas_chart.Margin.Left;//определяем координату (до масштабирования) canvas1 в border. Она изменяться не будет, мы по ней снова отпозиционируем график в Mod_Bars_Y

                    int rng = N_lb - N_fb + 1;
                    mx = chart.GetRange(N_fb, rng).Max(ch => ch.bar_highs_double);//мах цена видимого графика
                    hiet = Math.Round((mx - chart.GetRange(N_fb, rng).Min(ch => ch.bar_lows_double)) / price_step + 1, MidpointRounding.AwayFromZero);//высота видимого графика в тиках
                }

                border_prices.CaptureMouse();
            }
        }

        private void Apdate_Y_1()
        {
            Point current_mouse = Mouse.GetPosition(this);//текущее положение мыши во время её переменещения

            //разница в координатах мыши по оси Y между текущим и начальным положением
            double razn_y1 = current_mouse.Y - cursor_pos.Y;//вычисляем скока пиксилей прошла мыша (cursor_pos1 - эта переменн заявлена над ф-циями)

            if (razn_y1 >= 3)
            {
                color_choice = false;//если есть вертикальное масштабирование, то при отжатии левой кнопки мыши вверх активировать горизонтальную линию уже не будем

                vertical_decrease(current_mouse, false);
            }
            else if (razn_y1 <= -3)
            {
                color_choice = false;//если есть вертикальное масштабирование, то при отжатии левой кнопки мыши вверх активировать горизонтальную линию уже не будем

                vertical_increase(current_mouse, false);
            }
        }

        private int Calculate_Vertical_Central_Cell()
        {
            int indx_1 = Convert.ToInt32(Vars.MathRound((border_chart.Height / chart_mas_y / 2 - canvas_chart.Margin.Top / chart_mas_y)));//вычисляем центральную цену графика по высоте учитывая что канвас мог быть передвинут в бордере
            if (indx_1 < 0) indx_1 = 0;
            else if (indx_1 > price_canvases.Count - 1)
                indx_1 = price_canvases.Count - 1;

            return indx_1;
        }

        private void vertical_increase(Point current_mouse1, bool chart_transform)
        {
            if (chart_mas_y < 12)// высота тика ещё меньше 12 пикселей
            {
                pozizionir = false;
                button15.Foreground = Brushes.White;//(Brush)bc.ConvertFrom("#FFE4BA00");

                cursor_pos = current_mouse1;

                double koef = 0.5;// 0.25;//увеличиваем высоту тика в пикселях
                if (chart_mas_y < 0.5)//!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! сдесь обязательно должен быть знак "<" а в vertical_decrease() "<="
                    koef = 0.05;
                else if (chart_mas_y < 2)
                    koef = 0.1;//0.05;//!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! сдесь обязательно должен быть знак "<" а в vertical_decrease() "<="
                else if (chart_mas_y >= 8)
                    koef = 1;// 0.5;//!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! сдесь обязательно должен быть знак ">=" а в vertical_increase() ">"

                if (chart_transform)
                    koef *= 2;

                lock (chart)
                {
                    chart_mas_y += koef;

                    if (chart_mas_y > 12)
                        chart_mas_y = 12;

                    //должно быть после изменения chart_mas_y
                    if (N_visibl_prc_label > 0 && chart_mas_y * transfrm_steps[N_visibl_prc_label - 1] >= 50)
                        N_visibl_prc_label -= 1;

                    if (!was_move && isGist)//это нельзя переносить в Mod_Bars_Y() потому что та функция вызывается из Expand()
                    {
                        was_move = true;
                        canvas_gistogramm.Visibility = Visibility.Hidden;
                    }

                    Mod_Bars_Y();

                    double new_canvas_y = Math.Round(border_chart.Height / 2 - indx*chart_mas_y, MidpointRounding.AwayFromZero);
                    new_canvas_y = vverx_vniz(new_canvas_y);//если график по вертикали замасштабирован до минимума, то при его развёртывании он выпадает из бордера

                    //это нельзя переносить в Mod_Bars_Y() потому что та функция вызывается из Expand()
                    canvas_chart.Margin = new Thickness(pos_x, new_canvas_y, 0, 0); //задаем  положение для canvas-график в бордере
                    canvas_prices.Margin = new Thickness(0, new_canvas_y, 0, 0); //задаем  положение для какнваса-ценовой_шкалы в бордере
                    LinesCanvas.Margin = new Thickness(0, new_canvas_y, 0, 0);
                }
            }
        }

        private void vertical_decrease(Point current_mouse1, bool chart_transform)
        {
            if (chart_mas_y > 0.15)//если  высота тика ещё больше 0,15 пикселя
            {
                pozizionir = false;
                button15.Foreground = Brushes.White;//(Brush)bc.ConvertFrom("#FFE4BA00");

                cursor_pos = current_mouse1;

                double koef = 0.5;//на сколько пикселей будем изменять высоту тика
                if (chart_mas_y <= 0.5)
                    koef = 0.05;//!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! сдесь обязательно должен быть знак "<=" а в vertical_increase() "<"
                else if (chart_mas_y <= 2)
                    koef = 0.1;//0.05;//!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! сдесь обязательно должен быть знак "<=" а в vertical_increase() "<"
                else if (chart_mas_y > 8)
                    koef = 1;// 0.5;//!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! сдесь обязательно должен быть знак ">" а в vertical_increase() ">="

                if (chart_transform)
                    koef *= 2;

                lock (chart)
                {
                    chart_mas_y -= koef;//если высота тика уже меньше или равеа 0,5 пикселя то высоту тика уменьшаем на 0,05 пикселя

                    if (chart_mas_y < 0.15)
                    {
                        koef = koef - (0.15 - chart_mas_y);
                        chart_mas_y = 0.15;
                    }

                    //должно быть после изменения chart_mas_y
                    if (N_visibl_prc_label < transfrm_steps.Length - 1 && chart_mas_y * transfrm_steps[N_visibl_prc_label] < 50)
                        N_visibl_prc_label += 1;

                    //должно быть после верхнего условия, ато там was_move проверяется
                    if (!was_move && isGist)//это нельзя переносить в Mod_Bars_Y() потому что та функция вызывается из Expand()
                    {
                        was_move = true;
                        canvas_gistogramm.Visibility = Visibility.Hidden;
                    }

                    Mod_Bars_Y();//вызываем ф-цию, масштабирующую график

                    //вычисляем новую координату У канваса в бордере
                    double new_canvas_y;
                    if (!chart_transform && hiet * (chart_mas_y + koef) >= border_chart.Height && hiet * chart_mas_y < border_chart.Height)//если высота видимого графика была больше-равна высоты бордера а теперь становится меньше высоты бордера то отцетровываем график                        
                    {
                        double ym = ((price_canvases[0].price - mx) / price_step) * chart_mas_y;//новая координата У мах-го бара видимого графика
                        new_canvas_y = Math.Round((border_chart.Height - hiet * chart_mas_y) / 2 - ym, MidpointRounding.AwayFromZero);
                        indx = Convert.ToInt32(Math.Round((border_chart.Height / chart_mas_y / 2 - new_canvas_y / chart_mas_y), 0, MidpointRounding.AwayFromZero));//если отцентровываем график то по-новому определяем центральную цену бордера
                        if (indx < 0) indx = 0;
                        else if (indx > price_canvases.Count - 1)
                            indx = price_canvases.Count - 1;
                    }
                    else
                        new_canvas_y = Math.Round(border_chart.Height / 2 - indx * chart_mas_y, MidpointRounding.AwayFromZero);

                    //это нельзя переносить в Mod_Bars_Y() потому что та функция вызывается из Expand()
                    canvas_chart.Margin = new Thickness(pos_x, new_canvas_y, 0, 0); //задаем  положение для canvas-график в бордере
                    canvas_prices.Margin = new Thickness(0, new_canvas_y, 0, 0); //задаем  положение для какнваса-ценовой_шкалы в бордере
                    LinesCanvas.Margin = new Thickness(0, new_canvas_y, 0, 0);
                }
            }
        }

        private void Mod_Bars_Y()//ф-ция масштабирования графика
        {

            //этот метод вызывается из Expand


            //масштабируем ценовую шкалу
            int kfc = transfrm_steps[N_visibl_prc_label];
            for (int i = 0; i < price_canvases.Count; i++)
            {
                if (price_canvases[i].price_label == null) continue;
                Canvas.SetTop(price_canvases[i].price_label, PriceScaleLabelSetTop(i.ToString()));//задаём новое расположение текущему лейблу 
                if (i % kfc == 0)
                    price_canvases[i].price_label.Visibility = Visibility.Visible;
                else price_canvases[i].price_label.Visibility = Visibility.Hidden;
            }

            PriceCursorSetTop();//указатель текущей цены 

            Resize_horizontal_line();//масштабируем линии
            
            double bar_width = bufer_x - 1;
            //if (bufer_x == 1) bar_width = 1;

            //масштабируем бары
            for (int i = N_fb; i <= N_lb; i++)
            {
                if (chart[i].bar_highs == 0) continue;//если это геп

                double[] position_y_body_Height = set_bar_or_candle(chart[i], Canvas.GetLeft(chart[i].chart_b), bar_width);

                set_filters(chart[i], bar_width);

                Canvas.SetTop(chart[i].chart_b, position_y_body_Height[0]);
                chart[i].chart_b.Height = position_y_body_Height[1];

                set_cluster_chart_lables_visiblity(chart[i]);
            }
        }

        private double PriceScaleLabelSetTop(string sN_cell)
        {
            int N_cell = Convert.ToInt32(sN_cell);
            return Math.Round(N_cell * chart_mas_y - 3, MidpointRounding.AwayFromZero);
        }


        //HorizontalChartTransform

        bool mas_X = false;//переменн указыв - нажата или отпущена левая кнопка мыши на временной шкале-Бордер_3, используется в Activate_Move_X, Check_Options. Возвращается в исходн полож в  Stop_Move        

        private void Activate_Move_X(object sender, MouseButtonEventArgs e)//ф-ция работает, если на Бордере-временной шкале нажали левую кнопку мыши
        {
            if (canvas_prices.Children.Count == 0 || button_press) return;

            if (VerticalActivLine != null)
                secure_activ_vertical_line();
            else
            {
                if (mas_X) return;

                color_choice = true;//разрешаем создавать вертикальнукю линию(если не будет масштабирования)
                mas_X = true;//идинтифицируем начало масштабирования
                was_move = false;
                cursor_pos = Mouse.GetPosition(this); //определяем начальное положение мыши 

                //вычисляем номер центрального бара в бордере
                lock (chart)
                {
                    pos_y = canvas_chart.Margin.Top;
                    indx = Calculate_Horizontal_Central_Cell();
                }

                border_times.CaptureMouse();
            }
        }

        private int Calculate_Horizontal_Central_Cell()
        {
            int indx_1 = N_fb + Convert.ToInt32(Vars.MathRound((border_chart.Width / 2 - Canvas.GetLeft(chart[N_fb].chart_b) - canvas_chart.Margin.Left) / bufer_x));
            if (indx_1 < N_fb) indx_1 = N_fb;
            else if (indx_1 > N_lb) indx_1 = N_lb;

            return indx_1;
        }

        private void Apdate_X_1()
        {
            Point current_mouse = Mouse.GetPosition(this);//текущее положение мыши во время её переменещения

            //разница в координатах мыши по оси X между текущим и начальным положением
            double razn_x1 = current_mouse.X - cursor_pos.X;//вычисляем скока пиксилей прошла мыша (cursor_pos_x - эта переменн заявлена над ф-циями)

            if (razn_x1 >= 5)
            {
                color_choice = false;//если мышу продвинули на 5 пикселей, то активировать вертикальную линию при отжатии левой кнопки мыши вверх уже не будем
                gorisontal_increase(current_mouse, false);
            }
            else if (razn_x1 <= -5)
            {
                color_choice = false;//активировать вертикальную линию при отжатии левой кнопки мыши вверх уже не будем
                gorisontal_decrease(current_mouse, false);
            }
        }

        private void gorisontal_increase(Point current_mouse_x, bool chart_transform)
        {
            if (bufer_x <= max_width_bar)// и ширина бара меньше мах-но допустимой ширины
            {
                pozizionir = false;
                button15.Foreground = Brushes.White;//(Brush)bc.ConvertFrom("#FFE4BA00");
                cursor_pos = current_mouse_x;

                lock (chart)
                {
                    if (chart_transform)
                    {
                        bufer_x += 2;
                        if (bufer_x > max_width_bar + 1)
                            bufer_x = max_width_bar + 1;
                    }
                    else if (bufer_x == 2)
                        bufer_x = 4;
                    else bufer_x += 1;//увеличиваем расстояние му барами

                    if (!was_move && vertical_gist)//bufer_x < 5)//если бары довольно узкие то скрываем временную шкалу и идентифицируем это
                    {
                        was_move = true;
                        canv_vert_gstgr.Visibility = Visibility.Hidden;
                        N_fb_bufer = N_fb;
                        N_lb_bufer = N_lb;
                    }

                    bool vv_vn = remove_sprava_sleva(indx, true, chart_transform, false);//при расширении графика удаляем лишние бары слева-справа, там же отмасштабируются оставшиеся бары

                    if (vv_vn)//если по краям график обрезался так, что в бордере не осталось достаточно видимого графика, то перепозиционируем график по вертикали
                    {
                        pos_y = vverx_vniz(pos_y);//чтобы график не пропадал или вверху или снизу
                        canvas_prices.Margin = new Thickness(0, pos_y, 0, 0);
                        LinesCanvas.Margin = new Thickness(0, pos_y, 0, 0);
                        if (isGist)
                            canvas_gistogramm.Margin = new Thickness(0, pos_y, 0, 0);
                    }

                    Mod_Bars_X(was_move);
                }
            }
        }

        private void gorisontal_decrease(Point current_mouse_x, bool chart_transform)
        {
            if (bufer_x > 2)//если расстояние м/у барами ещё шире 1-го пикселя (или 2-ух)
            {
                pozizionir = false;
                button15.Foreground = Brushes.White;//(Brush)bc.ConvertFrom("#FFE4BA00");
                cursor_pos = current_mouse_x;

                lock (chart)
                {
                    //int koef = 1;
                    if (chart_transform)
                    {
                        if (bufer_x == 5)
                            bufer_x = 4;
                        else
                        {
                            bufer_x -= 2;

                            if (bufer_x < 2)
                                bufer_x = 2;
                            //else koef = 2;
                        }
                    }
                    else if (bufer_x == 4)
                    {
                        bufer_x = 2;
                        //koef = 2;
                    }
                    else bufer_x -= 1;//уменьшаем расстояние му барами

                    if (!was_move && vertical_gist)//bufer_x == 4)//если присужении графика мы дошли до достаточно узких баров то скрываем временную шкалу
                    {
                        was_move = true;//идентифицируем что временная шкала скрыта
                        canv_vert_gstgr.Visibility = Visibility.Hidden;
                        N_fb_bufer = N_fb;
                        N_lb_bufer = N_lb;
                    }

                    //должно быть после bufer_x -= 1; и до masshtab_bars_X(), т.к. просчитывает положение баров до масштабирования но по новыому bufer_x
                     //central_bar_number_calculate(koef);//пересчитываем номер центрального бара

                    masshtab_chart_X(false, chart_transform, false);//вызываем ф-цию, масштабирующую график
                    int ghyt = Convert.ToInt32(Math.Ceiling(border_chart.Width / bufer_x / 2));//к-во баров помещающихся в половине бордера по ширине

                    if (N_fb > 0)
                    {
                        int fds = indx - ghyt;//вычисляем номер первого видимого левого бара на графике после масштабирования
                        if (fds < 0) fds = 0;
                        if (fds < N_fb)
                        {
                            revers_edit_chart(N_fb - 1, fds, N_lb, false);
                            N_fb = fds;
                        }

                        //должно быть за рамками условия if (fds < N_fb), иначе плохо работает
                        visibl_first_date_label();
                    }

                    if (N_lb < chart.Count - 1)
                    {
                        int fds = indx + ghyt;//вычисляем номер последнего видимого правого бара на графике после масштабирования
                        if (fds > chart.Count - 1) fds = chart.Count - 1;
                        if (fds > N_lb)
                        {
                            edit_chart(N_lb + 1, fds, N_fb, false);
                            N_lb = fds;
                        }
                    }

                    Mod_Bars_X(was_move);
                }
            }
        }

        private bool remove_sprava_sleva(int indx_4, bool gorisontal_increase, bool chart_transform, bool expand)
        {
            //этот метод вызывается из Expand() и realtime_ed_chart() и gorisontal_increase()

            int ghyt = Convert.ToInt32(Math.Ceiling(border_chart.Width / bufer_x / 2));//к-во баров помещающихся в половине бордера по ширине
            bool vv_vn = false;
            int fds = indx_4 + ghyt;
            if (fds > chart.Count - 1)
                fds = chart.Count - 1;

            if (fds < N_lb)
            {
                int remowe_bars = N_lb - fds;
                remove_bars_sprava(remowe_bars);
                remove_date_time_labels(fds + 1, N_lb);
                if (!gorisontal_increase && vertical_gist && canv_vert_gstgr.Children.Count > 0)
                    canv_vert_gstgr.Children.RemoveRange(canv_vert_gstgr.Children.Count - remowe_bars, remowe_bars);

                N_lb = fds;
                vv_vn = true;
            }

            fds = indx_4 - ghyt;
            if (fds < 0)
                fds = 0;

            if (fds > N_fb)
            {
                int remowe_bars = fds - N_fb;
                remowe_sleva(remowe_bars);
                remove_date_time_labels(N_fb, fds - 1);
                if (!gorisontal_increase && vertical_gist && canv_vert_gstgr.Children.Count > 0)
                    canv_vert_gstgr.Children.RemoveRange(0, remowe_bars);

                N_fb = fds;
                masshtab_chart_X(true, chart_transform, expand);//вызываем ф-цию, масштабирующую график
                
                //!!!!!!!!!!!!!! должно быть после masshtab_chart_X()
                visibl_first_date_label();

                vv_vn = true;
            }
            else masshtab_chart_X(true, chart_transform, expand);//вызываем ф-цию, масштабирующую график

            return vv_vn;
        }

        private void Mod_Bars_X(bool was_move_s)
        {
            double pzt_x = Math.Round(border_chart.Width / 2 - Canvas.GetLeft(chart[indx].chart_b), MidpointRounding.AwayFromZero);
            canvas_chart.Margin = new Thickness(pzt_x, pos_y, 0, 0);       
            canvas_vertical_line.Margin = new Thickness(pzt_x, 0, 0, 0);
            canvas_times.Margin = new Thickness(pzt_x, 0, 0, 0);
            PriceCursorSetLeft(pzt_x);
        }

        private void masshtab_chart_X(bool uvelich, bool chart_transform, bool expand)
        {
            //этот метод вызывается из Expand() и realtime_ed_chart() и gorisontal_decrease(), remove_sprava_sleva()

            double positions_X = -bufer_x, bufer_gh = 100000, bufer_df = 100000;
            double bar_width = bufer_x - 1;
            //if(bufer_x == 1) bar_width = 1;
            double posit_time_label = Math.Round(positions_X - otstup + 4 + bar_width / 2), posit_date_label = Math.Round(positions_X - otstup + 1 + bar_width / 2, MidpointRounding.AwayFromZero);

            for (int i = N_fb; i <= N_lb; i++)//цикл соответствует к-ву баров в графике
            {
                positions_X += bufer_x;//вычисляем координату Х следующего бара
                posit_time_label += bufer_x;
                posit_date_label += bufer_x;

                if (chart[i].bar_highs != 0)
                {
                    if (uvelich)//если график увеличиваем
                    {
                        if ((expand && bufer_x > 8) || (bufer_x == 9 || (bufer_x == 10 && chart_transform)))// тени свечей скрываем, а тело свечми превращаем в бар
                        {//edit bar
                            chart[i].chart_b.Height = Calculate_bar_height(chart[i]);
                            double position_y = Calculate_bar_position_top(chart[i].N_cel_in_Price_scale_list);//координата У текущего бара
                            Canvas.SetTop(chart[i].chart_b, position_y);
                            chart[i].shadow.Visibility = Visibility.Hidden;
                            chart[i].chart_b.BorderBrush = Brushes.Gray;
                            if (chart[i].color == -6)
                                chart[i].chart_b.Background = Brushes.Black;
                            else
                                chart[i].chart_b.Background = (Brush)bc.ConvertFrom("#FF4D4D55");//(Brush)bc.ConvertFrom("#FFB27B00");
                        }
                        else //edit candle
                        if (bufer_x <= 8)//если при увеличении размеры тика не позволяют ещё выводить бары, значит выводим свечи, значит задаём горизонтальную координату тени свечи
                            Canvas.SetLeft(chart[i].shadow, positions_X + Math.Floor(bar_width / 2));
                    }//если график уменьшаем
                    else if (expand || bufer_x == 8 || (bufer_x == 7 && chart_transform))//тени свечей делаем видимыми тока один раз при bufer_x == 8
                    {//edit candle
                        chart[i].shadow.Height = Calculate_candle_shadow_height(chart[i]);
                        double posit_Y = Calculate_candle_shadow_position_top(chart[i]);
                        Canvas.SetTop(chart[i].shadow, posit_Y);
                        Canvas.SetLeft(chart[i].shadow, positions_X + Math.Floor(bar_width / 2));
                        double position_y = Calculate_candle_body_position_top(chart[i]);
                        chart[i].chart_b.Height = Calculate_candle_body_height(chart[i]);
                        Canvas.SetTop(chart[i].chart_b, position_y);
                        chart[i].shadow.Visibility = Visibility.Visible;
                    }
                    else if (bufer_x <= 8)//если при уменьшении размеры тика не позволяют выводить бары, значит выводим свечи, значит задаём горизонтальную координату тени свечи
                        Canvas.SetLeft(chart[i].shadow, positions_X + Math.Floor(bar_width / 2));

                    set_filters(chart[i], bar_width);
                    set_cluster_chart_lables_visiblity(chart[i]);
                    chart[i].chart_b.Width = bar_width;//задаём новую ширину бара в пикселях
                }

                Canvas.SetLeft(chart[i].chart_b, positions_X);//изменяем расположение текущего бара(уже нарисованного) в соответствиe с новыми параметрами   

                if (chart[i].time_labels != null)
                {
                    Canvas.SetLeft(chart[i].time_labels, posit_time_label);//изменяем расположение временноых лейблов
                    bufer_df = visiblity_hidden_time_label(chart[i], posit_time_label, bufer_df, false);
                }

                if (chart[i].date_Labels != null)
                {
                    Canvas.SetLeft(chart[i].date_Labels, posit_date_label);//изменяем расположение лейблов с датами
                    string si = i.ToString();
                    bufer_gh = visiblity_hidden_date_label(chart[i].date_Labels, posit_date_label, bufer_gh, false, chart[i].time_of_bar, si);
                }
            }

            set_vertical_lines_coordinat(lines_X);//масштабируем вертикальные линии
        }

        private void finish_masshtab_X()
        {
            lock (chart)
            {
                int strt = N_fb_bufer, nd = N_lb_bufer;
                if (N_fb_bufer < N_fb)
                {
                    int remowe_bars = N_fb - N_fb_bufer;
                    if (canv_vert_gstgr.Children.Count > 0)
                        canv_vert_gstgr.Children.RemoveRange(0, remowe_bars);

                    strt = N_fb;
                }

                if (N_lb_bufer > N_lb)
                {
                    int remowe_bars = N_lb_bufer - N_lb;
                    if(canv_vert_gstgr.Children.Count > 0)
                        canv_vert_gstgr.Children.RemoveRange(canv_vert_gstgr.Children.Count - remowe_bars, remowe_bars);

                    nd = N_lb;
                }

                masshtab_X_vertic_gistgrm(strt, nd);

                if (N_fb_bufer > N_fb)
                    revers_edit_vertc_gistgr(N_fb_bufer - 1, N_fb);

                if (N_lb_bufer < N_lb)
                    edit_vertc_gistgr(N_lb_bufer + 1, N_lb);

                canv_vert_gstgr.Margin = new Thickness(canvas_chart.Margin.Left, border_chart.Height - VerticalHistohramBarMaxHeight, 0, 0);
                canv_vert_gstgr.Visibility = Visibility.Visible;
            }
        }
        

        private void Mousewheel(object sender, MouseWheelEventArgs e)//вращаем колесо мыши
        {
            if (canvas_times.Children.Count == 0 || button_press || mas_Y)// || mas_X)
                return;//если график пока не нарисован

            Point current_mouse = Mouse.GetPosition(this);
            pos_y = canvas_chart.Margin.Top;

            if (e.Delta < 0)
            {
                pos_y = canvas_chart.Margin.Top;
                indx = Calculate_Horizontal_Central_Cell();
                was_move = false;
                mas_X = true;
                gorisontal_decrease(current_mouse, true);
                if (was_move)
                    finish_masshtab_X();

                mas_X = false;

                    //вертикальное масштабирование должно быть после горизонтального, ато там изменяется к-во видимых в бордере баров и в Mod_Bars_Y() делаются видимыми фильтра и лейблы в кластерах
                    pos_x = canvas_chart.Margin.Left;
                    indx = Calculate_Vertical_Central_Cell();
                    was_move = false;
                    mas_Y = true;
                    vertical_decrease(current_mouse, true);
                    if (was_move)
                    {
                        Vertical_transform_horizontal_histogram();
                        canvas_gistogramm.Margin = new Thickness(0, canvas_chart.Margin.Top, 0, 0);
                        canvas_gistogramm.Visibility = Visibility.Visible;
                    }

                    mas_Y = false;

                set_activ_lines_new_positions();

            }
            else if (e.Delta > 0)
            {
                pos_y = canvas_chart.Margin.Top;
                indx = Calculate_Horizontal_Central_Cell();
                was_move = false;
                mas_X = true;
                gorisontal_increase(current_mouse, true);
                if (was_move)
                    finish_masshtab_X();

                mas_X = false;
                pos_x = canvas_chart.Margin.Left;
                indx = Calculate_Vertical_Central_Cell();
                was_move = false;
                mas_Y = true;
                vertical_increase(current_mouse, true);
                if (was_move)
                {
                    Vertical_transform_horizontal_histogram();
                    canvas_gistogramm.Margin = new Thickness(0, canvas_chart.Margin.Top, 0, 0);
                    canvas_gistogramm.Visibility = Visibility.Visible;
                }

                mas_Y = false;
                set_activ_lines_new_positions();
            }

            if (canvas_times.Children.Count > 0 && bufer_x >= max_width_bar + 1 && chart_mas_y >= 12)
                button14.Content = "-";
            else button14.Content = "+";
        }

        private void Expand(object sender, RoutedEventArgs e)//развернуть график
        {
            if (canvas_prices.Children.Count == 0 || button_press) return;

            double pozt_y = canvas_chart.Margin.Top;
            double pozt_x = canvas_chart.Margin.Left;

            lock (chart)
            {
                if (bufer_x >= max_width_bar + 1 && chart_mas_y >= 12)
                {
                    if (pozizionir)//если позиционирование актвно то изменяем ширину видимых баров и вызываем ф-ию позиционирования
                    {
                        bufer_x = 4;
                        masshtab_chart_X(false, false, true);
                        masshtab_X_vertic_gistgrm(N_fb, N_lb);

                            int rng = Convert.ToInt32(Math.Ceiling((border_chart.Width - 100) / bufer_x)) + 2;
                            int fds = chart.Count - rng;
                            if (fds < 0)
                            {
                                fds = 0;
                                rng = chart.Count;
                            }

                            double mx_c = chart.GetRange(fds, rng).Max(ch => ch.bar_highs_double), mn_c = chart.GetRange(fds, rng).Min(ch => ch.bar_lows_double);
                            double hietc = (mx_c - mn_c) / price_step + 1;//высота видимого графика в тиках
                            double new_chart_mas_y = (border_chart.Height - 60) / hietc;//вычисляем новое знач chart_mas_y

                            if (new_chart_mas_y < 12)
                            {
                                chart_mas_y = new_chart_mas_y;
                                if (chart_mas_y <= 0.15)//округляем к уменьшению chart_mas_y
                                    chart_mas_y = 0.15;
                                else if (chart_mas_y < 0.5)
                                    chart_mas_y = Math.Floor(chart_mas_y * 100) / 100;
                                else if (chart_mas_y < 8)
                                    chart_mas_y = Math.Floor(chart_mas_y * 10) / 10;
                                else chart_mas_y = Math.Floor(chart_mas_y);

                                for (int i = 0; i < transfrm_steps.Length; i++)//вычисляем новое знач N_visibl_prc_label
                                {
                                    if (chart_mas_y * transfrm_steps[i] >= 50)
                                    {
                                        N_visibl_prc_label = i;
                                        break;
                                    }
                                }

                                Mod_Bars_Y();//лейблы в кластере скрываются в Mod_Bars_Y(), а в masshtab_chart_X() остаются видимыми при вызове отсюда
                                Vertical_transform_horizontal_histogram();
                            }

                        Positionirov(true, true);
                        button14.Content = "+";
                        return;
                    }
                    else
                    {
                        int indx_6 = Calculate_Horizontal_Central_Cell();

                        //должно быть после определения indx_6 
                        bufer_x = 4;
                        masshtab_chart_X(false, false, true);
                        masshtab_X_vertic_gistgrm(N_fb, N_lb);

                        pozt_x = Math.Round(border_chart.Width / 2 - Canvas.GetLeft(chart[indx_6].chart_b), MidpointRounding.AwayFromZero);

                        if (N_lb < chart.Count - 1)
                            add_sprava(pozt_x, false);
                        if (N_fb > 0)
                            insert_revers_sleva(pozt_x, false);

                            int rng = N_lb - N_fb + 1;
                            double mx_c = chart.GetRange(N_fb, rng).Max(ch => ch.bar_highs_double), mn_c = chart.GetRange(N_fb, rng).Min(ch => ch.bar_lows_double);
                            double hietc = (mx_c - mn_c) / price_step + 1;//высота видимого графика в тиках
                            double new_chart_mas_y = (border_chart.Height - 60) / hietc;

                            if (new_chart_mas_y < 12)
                            {
                                chart_mas_y = new_chart_mas_y;
                                if (chart_mas_y <= 0.15)
                                    chart_mas_y = 0.15;
                                else if (chart_mas_y < 0.5)
                                    chart_mas_y = Math.Floor(chart_mas_y * 100) / 100;
                                else if (chart_mas_y < 8)
                                    chart_mas_y = Math.Floor(chart_mas_y * 10) / 10;
                                else chart_mas_y = Math.Floor(chart_mas_y);

                                pozt_y = Math.Round((border_chart.Height - hietc * chart_mas_y) / 2 - (price_canvases[0].price - mx_c) / price_step * chart_mas_y, MidpointRounding.AwayFromZero);

                                for (int i = 0; i < transfrm_steps.Length; i++)
                                {
                                    if (chart_mas_y * transfrm_steps[i] >= 50)
                                    {
                                        N_visibl_prc_label = i;
                                        break;
                                    }
                                }

                                Mod_Bars_Y();//лейблы в кластере скрываются в Mod_Bars_Y(), а в masshtab_chart_X() остаются видимыми при вызове отсюда
                                Vertical_transform_horizontal_histogram();
                            }

                        canvas_times.Margin = new Thickness(pozt_x, 0, 0, 0); //задаем  положение для канваса-временной_шкалы в бодере                        
                        canvas_vertical_line.Margin = new Thickness(pozt_x, 0, 0, 0);
                        canvas_prices.Margin = new Thickness(0, pozt_y, 0, 0);
                        LinesCanvas.Margin = new Thickness(0, pozt_y, 0, 0);
                        if (isGist)
                            canvas_gistogramm.Margin = new Thickness(0, pozt_y, 0, 0);
                        if (vertical_gist)
                            canv_vert_gstgr.Margin = new Thickness(pozt_x, border_chart.Height - VerticalHistohramBarMaxHeight, 0, 0);

                        PriceCursorSetLeft(pozt_x);

                        button14.Content = "+";
                    }
                }
                else
                {
                    double buf_x_2 = bufer_x;

                    if (bufer_x < max_width_bar + 1)
                    {
                        if (pozizionir)//если позиционирование актвно то изменяем ширину видимых баров и вызываем ф-ию позиционирования
                        {
                            bufer_x = max_width_bar + 1;
                            masshtab_chart_X(true, false, true);
                            masshtab_X_vertic_gistgrm(N_fb, N_lb);

                            if (chart_mas_y < 12)
                            {
                                chart_mas_y = 12;
                                N_visibl_prc_label = 0;
                                Mod_Bars_Y();//лейблы в кластере делаются видимыми в Mod_Bars_Y(), а в masshtab_chart_X() остаются не тронутыми при вызове отсюда
                                Vertical_transform_horizontal_histogram();
                            }

                            Positionirov(true, true);
                            button14.Content = "-";
                            return;
                        }
                        else//график разварачиваем относительно центра графика
                        {
                            int indx_5 = Calculate_Horizontal_Central_Cell();
                            bufer_x = max_width_bar + 1;

                            //лейблы в кластере делаются видимыми в Mod_Bars_Y(), а в remove_sprava_sleva-masshtab_chart_X() остаются не тронутыми при вызове отсюда
                            bool vv_vn = remove_sprava_sleva(indx_5, false, false, true);//расширяем график до мах возможной ширины
                            masshtab_X_vertic_gistgrm(N_fb, N_lb);
                            pozt_x = Math.Round(border_chart.Width / 2 - Canvas.GetLeft(chart[indx_5].chart_b), MidpointRounding.AwayFromZero);
                            canvas_times.Margin = new Thickness(pozt_x, 0, 0, 0); //задаем  положение для канваса-временной_шкалы в бодере
                            canvas_vertical_line.Margin = new Thickness(pozt_x, 0, 0, 0);
                            if (vertical_gist)
                                canv_vert_gstgr.Margin = new Thickness(pozt_x, border_chart.Height - VerticalHistohramBarMaxHeight, 0, 0);

                            if (chart_mas_y >= 12)//если график не будет увеличиваться по высоте(высота тика и так =мах), то проверяем не выпадает ли из бордера по вертикали обрезанный слева-справа график
                            {
                                if (vv_vn)
                                    pozt_y = vverx_vniz(pozt_y);//если chart_mas_y < 12, то vverx_vniz() будет выполнятся ниже

                                canvas_prices.Margin = new Thickness(0, pozt_y, 0, 0);
                                LinesCanvas.Margin = new Thickness(0, pozt_y, 0, 0);
                                if (isGist)
                                    canvas_gistogramm.Margin = new Thickness(0, pozt_y, 0, 0);
                            }

                            PriceCursorSetLeft(pozt_x);
                        }
                    }

                    if (chart_mas_y < 12)
                    {
                        int indx_3 = Calculate_Vertical_Central_Cell();
                        chart_mas_y = 12;
                        N_visibl_prc_label = 0;
                        Mod_Bars_Y();//лейблы в кластере делаются видимыми в Mod_Bars_Y(), а в masshtab_chart_X() остаются не тронутыми при вызове отсюда
                        Vertical_transform_horizontal_histogram();
                        pozt_y = Math.Round(border_chart.Height / 2 - indx_3 * chart_mas_y, MidpointRounding.AwayFromZero);
                        pozt_y = vverx_vniz(pozt_y);//если график по вертикали замасштабирован до минимума, то при его развёртывании он выпадает из бордера
                        canvas_prices.Margin = new Thickness(0, pozt_y, 0, 0);
                        LinesCanvas.Margin = new Thickness(0, pozt_y, 0, 0);
                        if (isGist)
                            canvas_gistogramm.Margin = new Thickness(0, pozt_y, 0, 0);
                    }

                    button14.Content = "-";
                }

                canvas_chart.Margin = new Thickness(pozt_x, pozt_y, 0, 0); //задаем  положение для canvas1-график в бордере 
            }
        }

        #endregion

        #region Histograms

        double max_total_v, min_cumulative_delta, max_cumulative_delta;// мах объём для вертикальной гистограммы
        public bool vertical_gist = true, isGist = true;
        public VertcalHistogramType vertical_histogram_type = VertcalHistogramType.CumulativeDelta;
        VertcalHistogramType vertical_histogram_type_buffer;
        List<Histogramma> MarcetProfile = new List<Histogramma>();
        List<Cluster> data_for_tick_chart_horisontal_histogram = new List<Cluster>();// массив для хранения данных для просчёта гистограммы
        public int hsthrm_fltr_1 = 0, hsthrm_fltr_2 = 0, hsthrm_fltr_3 = 0;//фильтра горизонтальной гистограммы
        public int Vert_hsthrm_fltr_1 = 0, Vert_hsthrm_fltr_2 = 0, Vert_hsthrm_fltr_3 = 0;//фильтра вертикальной гистограммы

        private void Calculate_hstgrm()
        {
            double line_height = 1;
            if (chart_mas_y >= 1.5) line_height = Math.Round(chart_mas_y, MidpointRounding.AwayFromZero);

            IEnumerable<IGrouping<double, Cluster>> gr_intrv = chart.SelectMany(mnts => mnts.Bars).GroupBy(it => it.price, it => it);
            foreach (IGrouping<double, Cluster> group in gr_intrv)
            {
                double rating = group.Sum(gr => gr.volume);
                if (rating != 0)
                    CreateHistogrammBar(line_height, group.Key, rating);
            }


            //var gstrm = chart.SelectMany(br => br.Bars).GroupBy(it => it.price, (zena, grup) => new { prc = zena, rating = grup.Sum(gr => gr.volume)});
            //foreach (var group in gstrm)
              //  CreateHistogrammBar(line_height, group.prc, group.rating);
            
            Update_Gistogramm(null);
        }

        private void CreateHistogrammBar(double line_height, double prc, double rating)
        {
            try
            {
                Border gistogramm_bar = new Border();
                gistogramm_bar.Height = line_height;
                gistogramm_bar.BorderThickness = new Thickness(0, 1, 1, 1);
                gistogramm_bar.Opacity = 0.5;
                MarcetProfile.Add(new Histogramma { histogramm_bar = gistogramm_bar, cluster = new Cluster { price = prc, volume = rating } });
                canvas_gistogramm.Children.Add(gistogramm_bar);
            }
            catch { }
        }

        private void Update_Gistogramm(List<double> bufer_mrct_prfl)
        {
            double max_gist_volum = MarcetProfile.Max(mp => mp.cluster.volume);
            double bufer_max_gist_volum = 0;
            if (bufer_mrct_prfl != null)
                bufer_max_gist_volum = bufer_mrct_prfl.Max();

            for (int i = 0; i < MarcetProfile.Count; i++)
            {
                if (bufer_mrct_prfl == null || i > bufer_mrct_prfl.Count - 1)//если это отрисовка истории или в реалтайме в гистоограмме добавилась новая цена
                {
                    double N_cell = Vars.MathRound(Calculate_N_cel_in_Price_scale_list(MarcetProfile[i].cluster.price));//то устанавливаем вертикальную координату нового бара гистограммы
                    MarcetProfile[i].N_cel_in_Price_scale_list = N_cell;
                    double new_posit_Y = Calculate_bar_position_top_1(N_cell);
                    Canvas.SetTop(MarcetProfile[i].histogramm_bar, new_posit_Y);
                }

                if (bufer_mrct_prfl == null || i > bufer_mrct_prfl.Count - 1 || bufer_max_gist_volum != max_gist_volum || bufer_mrct_prfl[i] != MarcetProfile[i].cluster.volume)//если это отрисовка истории или в реалтайме добавилась новая цена в гистограмме,или обновился(увеличился) мах объём, или на уже сеществующей цене изменился объём
                {
                    double wdt = Math.Round(MarcetProfile[i].cluster.volume / max_gist_volum * 200, MidpointRounding.AwayFromZero);//устанавливаем ширину текущего бара гистограммы
                    MarcetProfile[i].histogramm_bar.Width = wdt;
                }

                if (bufer_mrct_prfl == null || i > bufer_mrct_prfl.Count - 1 || bufer_mrct_prfl[i] != MarcetProfile[i].cluster.volume)//если это отрисовка истории или в реалтайме в гистоограмме добавилась новая цена или в реалтайме на уже сеществующей цене изменился объём, 
                    color_gorisontal_gistogram_bar(MarcetProfile[i], max_gist_volum);
                else if (bufer_max_gist_volum != max_gist_volum && bufer_mrct_prfl[i] == bufer_max_gist_volum)//или объём на данной цене остался тот же, но в прошлый раз он был максимальным(красным) а теперь мах об на другой цене, то текущий бар перекрашиваем из красного цвета в другой
                    color_gorisontal_gistogram_bar(MarcetProfile[i], max_gist_volum);
            }
        }

        private void color_gorisontal_gistogram_bar(Histogramma current_bar, double max_gist_volum)
        {
            bool filter_color = false;

            if (hsthrm_fltr_1 > 0 && current_bar.cluster.volume >= hsthrm_fltr_1)
            {
                current_bar.histogramm_bar.Background = (Brush)bc.ConvertFrom("#FF2222C3");
                current_bar.histogramm_bar.BorderBrush = (Brush)bc.ConvertFrom("#FF2222C3");
                filter_color = true;
            }
            else if (hsthrm_fltr_2 > 0 && current_bar.cluster.volume >= hsthrm_fltr_2)
            {
                current_bar.histogramm_bar.Background = (Brush)bc.ConvertFrom("#FF1472FB");//("#FF007EB9");
                current_bar.histogramm_bar.BorderBrush = (Brush)bc.ConvertFrom("#FF1472FB");//("#FF007EB9");
                filter_color = true;
            }
            else if (hsthrm_fltr_3 > 0 && current_bar.cluster.volume >= hsthrm_fltr_3)
            {
                current_bar.histogramm_bar.Background = (Brush)bc.ConvertFrom("#FF00DCFF");//("#FF14BD80");
                current_bar.histogramm_bar.BorderBrush = (Brush)bc.ConvertFrom("#FF00DCFF");//("#FF14BD80");
                filter_color = true;
            }
            else
            {
                current_bar.histogramm_bar.Background = (Brush)bc.ConvertFrom("#FF4D4D55");
                current_bar.histogramm_bar.BorderBrush = Brushes.Gray;
            }

            if (current_bar.cluster.volume == max_gist_volum)
            {
                //Canvas.SetZIndex(current_bar.histogramm_bar, 1);
                current_bar.histogramm_bar.BorderBrush = Brushes.Red;
                if (!filter_color)
                    current_bar.histogramm_bar.Background = Brushes.Red;
            }

            current_bar.histogramm_bar.Background.Freeze();
            current_bar.histogramm_bar.BorderBrush.Freeze();
        }

        private void real_time_updt_gsst(List<Tick> new_ticks)
        {
            List<double> bufer_mrct_prfl = new List<double>();// (MarcetProfile.Select(mp => mp.price_volum.volume));//делаем 2 копии гистограммы. Одну будем дополнять-обновлять и пересчитывать
            List<Cluster> real_t_gstgr = new List<Cluster>();// (MarcetProfile.Select(mp => mp.price_volum));// а потом обновлённую будем сравнивать со второй - первоначпльной(не изменённой) копией
            foreach (Histogramma bar in MarcetProfile)
            {
                bufer_mrct_prfl.Add(bar.cluster.volume);
                real_t_gstgr.Add(bar.cluster);
            }

            //real_t_gstgr.AddRange(new_ticks.Select(mp => mp.price_volum));
            foreach (Tick tick in new_ticks)
                real_t_gstgr.Add(new Cluster { price = tick.price, sell = tick.volume});
            var gstrm = real_t_gstgr.GroupBy(it => it.price, (zena, grup) => new { prc = zena, rating = grup.Sum(gr => gr.volume) });//перепросчитываем гистограмму

            double line_height = 1;
            if (chart_mas_y >= 1.5)
                line_height = Math.Round(chart_mas_y, MidpointRounding.AwayFromZero);

            int i = 0;
            foreach (var group in gstrm)//обновляем гистограмму
            {
                if (i < MarcetProfile.Count)
                    MarcetProfile[i].cluster.volume = group.rating;
                else CreateHistogrammBar(line_height, group.prc, group.rating);//если добавились новые цены в гистограмме, то добавляем новые бордеры

                i++;
            }

            Update_Gistogramm(bufer_mrct_prfl);
        }
        
        private void Vertical_transform_horizontal_histogram()
        {

            //этот метод вызывается из Expand и Stop_Move, Mousewheel(), NewMaxPriseScaleReDrawChart()


            if (isGist)//масштабируем гистограмму
            {
                lock (chart)
                {
                    double line_height = 1;
                    if (chart_mas_y >= 1.5)
                        line_height = Math.Round(chart_mas_y, MidpointRounding.AwayFromZero);

                    for (int i = 0; i < MarcetProfile.Count; i++)
                    {
                        double new_posit_Y = Calculate_bar_position_top_1(MarcetProfile[i].N_cel_in_Price_scale_list);
                        Canvas.SetTop(MarcetProfile[i].histogramm_bar, new_posit_Y);
                        MarcetProfile[i].histogramm_bar.Height = line_height;
                    }


                    //позиционируется гистограмма в Stop_Move() или Expand()
                }
            }
        }
        
        class Histogramma
        {
            public Border histogramm_bar { get; set; }
            public Cluster cluster { get; set; }
            public double N_cel_in_Price_scale_list { get; set; }
        }


        //Vertical Histogram

        private bool Calculate_vertical_gistogramm(int strt)
        {
            ClearVerticalHistogamm();

            double[] result_array = Calculate_Max_Veriical_Histogramm_for_BarChart(0);
            max_total_v = result_array[0];
            max_cumulative_delta = result_array[1];
            min_cumulative_delta = result_array[2];

            for (int i = strt; i < chart.Count; i++)
                Create_vertical_gistigramm_bar(chart[i], SearchPreviosBar(i.ToString(), chart));

            return true;
        } 

        private Chart SearchPreviosBar(string curent_bar_index, List<Chart> currentChartList)
        {
            Chart previosBar = null;
            if (vertical_histogram_type_buffer == VertcalHistogramType.CumulativeDelta)
            {
                int i = Convert.ToInt32(curent_bar_index);
                if (i > 0)
                {
                    int n_cell = currentChartList.FindLastIndex(i - 1, delegate (Chart bar) { return bar.bar_highs > 0; });

                    if (n_cell >= 0)
                        previosBar = currentChartList[n_cell];
                }
            }

            return previosBar;
        }
        
        private double[] Calculate_Max_Veriical_Histogramm_for_BarChart(int strt)
        {
            double max_t_v = 0, min_delta = 0, max_delta = 0;
            if (vertical_histogram_type_buffer == VertcalHistogramType.Volume)
            {
                for (int i = strt; i < chart.Count; i++)
                {
                    if (chart[i].bar_highs == 0) continue;                    
                    chart[i].vertic_gist_volum = chart[i].Bars.Sum(b => b.volume);
                    if (chart[i].vertic_gist_volum > max_t_v)
                        max_t_v = chart[i].vertic_gist_volum;
                }
            }
            else if (vertical_histogram_type_buffer == VertcalHistogramType.CumulativeDelta)
            {
                //double cumulative_delta = 0;
                for (int i = 0; i < chart.Count; i++)//т.к. CumulativeDelta в реалтайме может уменьшать свои мах-мин значения, то перебираем все бары, а не от strt
                {
                    /*cumulative_delta += chart[i].Delta;

                    chart[i].cumulative_delta = cumulative_delta;
                    if (i == 0)
                        max_delta =
                            min_delta = cumulative_delta;//т.к. даже у первого бара cumulative_delta может быть < 0, то в цыкле(после присвоения значения cumulative_delta первому бару) устанавливаем начальные значения max_delta и min_delta от первого бара
                    else if (cumulative_delta > max_delta)
                        max_delta = cumulative_delta;
                    else if (cumulative_delta < min_delta)
                        min_delta = cumulative_delta;*/
                    if (chart[i].bar_highs == 0) continue;

                    double buys = chart[i].Bars.Sum(b => b.buy);
                    double sells = chart[i].Bars.Sum(b => b.sell);

                    chart[i].cumulative_delta = buys - sells;

                    if (i == 0)
                        max_delta =
                            min_delta = chart[i].cumulative_delta;//т.к. даже у первого бара cumulative_delta может быть < 0, то в цыкле(после присвоения значения cumulative_delta первому бару) устанавливаем начальные значения max_delta и min_delta от первого бара
                    else
                    {
                        int previos = chart.FindLastIndex(i - 1, ch => ch.bar_highs != 0);
                        if (previos != -1)
                            chart[i].cumulative_delta += chart[previos].cumulative_delta;

                        if (chart[i].cumulative_delta > max_delta)
                            max_delta = chart[i].cumulative_delta;
                        else if (chart[i].cumulative_delta < min_delta)
                            min_delta = chart[i].cumulative_delta;
                    }
                }
            }

            return new double[] { max_t_v, max_delta, min_delta };
        }

        private void Create_vertical_gistigramm_bar(Chart current_bar, Chart previosBar)
        {
            if (vertical_histogram_type_buffer == VertcalHistogramType.CumulativeDelta)
            {
                current_bar.VerticalHistogramLine = new Line();
                current_bar.VerticalHistogramLine.StrokeThickness = 1;
                current_bar.VerticalHistogramLine.Stroke = Brushes.White;
                SetTop_and_Height_VerticalHistogramBar(current_bar, max_total_v, previosBar);
            }
            else
            {
                Border vert_g = new Border();
                current_bar.total_vol_gist = vert_g;
                vert_g.BorderThickness = new Thickness(1);//рамка-границы бордера  
                color_vertical_gistogram_bar(current_bar);

                vert_g.Opacity = 0.5;
                SetTop_and_Height_VerticalHistogramBar(current_bar, max_total_v, null);
            }
        }

        private void updt_vert_hstgrm(int nd, bool max_total_v_change)
        {
            if (max_total_v_change)
            {
                for (int i = 0; i <= nd; i++)
                        SetTop_and_Height_VerticalHistogramBar(chart[i], max_total_v, SearchPreviosBar(i.ToString(), chart));
            }
        }

        private void SetTop_and_Height_VerticalHistogramBar(Chart current_bar, double m_t_volume, Chart previosBar)
        {
           if (vertical_histogram_type_buffer == VertcalHistogramType.CumulativeDelta)
            {
                double full_height = max_cumulative_delta - min_cumulative_delta;
                if (full_height == 0) return;
                double gst_high = Math.Round(current_bar.cumulative_delta / full_height * VerticalHistohramBarMaxHeight, MidpointRounding.AwayFromZero);
                
                if (current_bar.bar_highs > 0)
                {
                    current_bar.VerticalHistogramLine.Y2 = Math.Round(max_cumulative_delta / full_height * VerticalHistohramBarMaxHeight - gst_high, MidpointRounding.AwayFromZero) - 2;

                    if (previosBar != null)
                    {
                        gst_high = Math.Round(previosBar.cumulative_delta / full_height * VerticalHistohramBarMaxHeight, MidpointRounding.AwayFromZero);
                        current_bar.VerticalHistogramLine.Y1 = Math.Round(max_cumulative_delta / full_height * VerticalHistohramBarMaxHeight - gst_high, MidpointRounding.AwayFromZero) - 2;
                    }
                    else
                        current_bar.VerticalHistogramLine.Y1 = current_bar.VerticalHistogramLine.Y2;
                }
                else
                    current_bar.VerticalHistogramLine.Visibility = Visibility.Hidden;
            }
            else if (current_bar.vertic_gist_volum != 0)
            {
                double gst_high = Math.Round(current_bar.vertic_gist_volum / m_t_volume * VerticalHistohramBarMaxHeight, MidpointRounding.AwayFromZero);
                if (gst_high < 4) gst_high = 4;
                current_bar.total_vol_gist.Height = gst_high;
                Canvas.SetTop(current_bar.total_vol_gist, VerticalHistohramBarMaxHeight + 1 - gst_high);
            }
            else current_bar.total_vol_gist.Height = 0;
        }

        private void color_vertical_gistogram_bar(Chart current_bar)
        {
            if (Vert_hsthrm_fltr_1 > 0 && current_bar.vertic_gist_volum >= Vert_hsthrm_fltr_1)
            {
                current_bar.total_vol_gist.Background = (Brush)bc.ConvertFrom("#FF2222C3");
                current_bar.total_vol_gist.BorderBrush = (Brush)bc.ConvertFrom("#FF2222C3");
            }
            else if (Vert_hsthrm_fltr_2 > 0 && current_bar.vertic_gist_volum >= Vert_hsthrm_fltr_2)
            {
                current_bar.total_vol_gist.Background = (Brush)bc.ConvertFrom("#FF1472FB");
                current_bar.total_vol_gist.BorderBrush = (Brush)bc.ConvertFrom("#FF1472FB");
            }
            else if (Vert_hsthrm_fltr_3 > 0 && current_bar.vertic_gist_volum >= Vert_hsthrm_fltr_3)
            {
                current_bar.total_vol_gist.Background = (Brush)bc.ConvertFrom("#FF00DCFF");
                current_bar.total_vol_gist.BorderBrush = (Brush)bc.ConvertFrom("#FF00DCFF");
            }
            else
            {
                current_bar.total_vol_gist.BorderBrush = Brushes.Gray;
                current_bar.total_vol_gist.Background = (Brush)bc.ConvertFrom("#FF4D4D55");
            }

            current_bar.total_vol_gist.BorderBrush.Freeze();
            current_bar.total_vol_gist.Background.Freeze();
        }

        private void realtime_update_vertical_histogram(double[] result_array, int chart_count_bufer, bool pererisovka_last_bar)
        {
            bool already_update = false;
            if (result_array[0] > max_total_v)
            {
                max_total_v = result_array[0];
                already_update = true;
            }

            if (result_array[1] != max_cumulative_delta)
            {
                max_cumulative_delta = result_array[1];
                already_update = true;
            }

            if (result_array[2] != min_cumulative_delta)
            {
                min_cumulative_delta = result_array[2];
                already_update = true;
            }

            if (already_update)//если изменился мах-мин вертикальн гистограммы то перерисовываем всю вертиkальную гистограмму
                updt_vert_hstgrm(chart_count_bufer - 1, true);

            if (pererisovka_last_bar)//если последний бар обновлялся, то перерисовываем его вертикальную гистограмму. Последний - имеется ввиду, до дорисовки новых баров
            {
                if (!already_update)//если последний обновлённый бар ещё не перерисовывали
                    SetTop_and_Height_VerticalHistogramBar(chart[chart_count_bufer - 1], max_total_v, SearchPreviosBar((chart_count_bufer - 1).ToString(), chart));

                if(vertical_histogram_type_buffer == VertcalHistogramType.Volume)
                    color_vertical_gistogram_bar(chart[chart_count_bufer - 1]);
            }

            for (int i = chart_count_bufer; i < chart.Count; i++)//дорисовываем новые столбики вертикальной гистограммы
                Create_vertical_gistigramm_bar(chart[i], SearchPreviosBar(i.ToString(), chart));
        }

        private void masshtab_X_vertic_gistgrm(int strt, int nd)
        {

            //этот метод вызывается из Expand() и realtime_ed_chart() и gorisontal_decrease()

            if (vertical_gist)
            {
                double positions_X = Canvas.GetLeft(chart[strt].chart_b) - bufer_x;
                double bar_width = bufer_x - 1;
                //if(bufer_x == 1) bar_width = 1;

                for (int i = strt; i <= nd; i++)//цикл соответствует к-ву баров в графике
                {
                    positions_X += bufer_x;
                    //if (chart[i].bar_highs == 0) continue;
                    if (vertical_histogram_type_buffer == VertcalHistogramType.CumulativeDelta)
                        VerticalHistogramLineSetLeft(i.ToString(), positions_X, bar_width);
                    else
                    {
                        chart[i].total_vol_gist.Width = bar_width;
                        Canvas.SetLeft(chart[i].total_vol_gist, positions_X);
                    }
                }
            }
        }

        private void edit_vertc_gistgr(int strt, int nd)
        {
            double positions_X = Canvas.GetLeft(chart[strt].chart_b) - bufer_x;

            double bar_width = bufer_x - 1;
            //if (bufer_x == 1) bar_width = 1;

            for (int i = strt; i <= nd; i++)
            {
                positions_X += bufer_x;
                if (vertical_histogram_type_buffer == VertcalHistogramType.CumulativeDelta)
                {
                    if (chart[i].VerticalHistogramLine != null)
                    {
                        VerticalHistogramLineSetLeft(i.ToString(), positions_X, bar_width);
                        if (chart[i].VerticalHistogramLine.Parent != null)
                            canv_vert_gstgr.Children.Remove(chart[i].VerticalHistogramLine);

                        canv_vert_gstgr.Children.Add(chart[i].VerticalHistogramLine);
                    }
                }
                else
                {
                    chart[i].total_vol_gist.Width = bar_width;
                    Canvas.SetLeft(chart[i].total_vol_gist, positions_X);
                    if (chart[i].total_vol_gist.Parent != null)
                        canv_vert_gstgr.Children.Remove(chart[i].total_vol_gist);

                    canv_vert_gstgr.Children.Add(chart[i].total_vol_gist);
                }
            }
        }

        private void revers_edit_vertc_gistgr(int strt, int nd)
        {
            double positions_X = Canvas.GetLeft(chart[strt].chart_b) + bufer_x;

            double bar_width = bufer_x - 1;
            //if (bufer_x == 1) bar_width = 1;

            for (int i = strt; i >= nd; i--)
            {
                positions_X -= bufer_x;
                if (vertical_histogram_type_buffer == VertcalHistogramType.CumulativeDelta)
                {
                    if (chart[i].VerticalHistogramLine != null)
                    {
                        VerticalHistogramLineSetLeft(i.ToString(), positions_X, bar_width);
                        if (chart[i].VerticalHistogramLine.Parent != null)
                            canv_vert_gstgr.Children.Remove(chart[i].VerticalHistogramLine);

                        canv_vert_gstgr.Children.Insert(0, chart[i].VerticalHistogramLine);
                    }
                }
                else
                {
                    chart[i].total_vol_gist.Width = bar_width;
                    Canvas.SetLeft(chart[i].total_vol_gist, positions_X);
                    if (chart[i].total_vol_gist.Parent != null)//когда после закрытия цветового меню мыша залипает на масштабировании то сдесь вылетает исключение
                        canv_vert_gstgr.Children.Remove(chart[i].total_vol_gist);

                    canv_vert_gstgr.Children.Insert(0, chart[i].total_vol_gist);
                }
            }
        }
        
        double VerticalHistohramBarMaxHeight = 100;

        private void Hiatogramm_settings(object sender, RoutedEventArgs e)
        {
            if (instrument == "" || button_press) return;

            int filter_11 = hsthrm_fltr_1;
            int filter_22 = hsthrm_fltr_2;
            int filter_33 = hsthrm_fltr_3;

            int filter_111 = Vert_hsthrm_fltr_1;
            int filter_222 = Vert_hsthrm_fltr_2;
            int filter_333 = Vert_hsthrm_fltr_3;

            VertcalHistogramType vertical_histogram_type_do = vertical_histogram_type_buffer;

            PopupHistogram hstgrm_form = new PopupHistogram(this, interval);
            hstgrm_form.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            hstgrm_form.ShowDialog();
            Set_vertical_histogram_type_buffer();

            if (filter_111 != Vert_hsthrm_fltr_1 || filter_222 != Vert_hsthrm_fltr_2 || filter_333 != Vert_hsthrm_fltr_3)
            {
                if (interval == 1)
                {
                    fltrss[3] = Vert_hsthrm_fltr_1;
                    fltrss[4] = Vert_hsthrm_fltr_2;
                    fltrss[5] = Vert_hsthrm_fltr_3;
                }
                else if (interval == 5)
                {
                    fltrss[9] = Vert_hsthrm_fltr_1;
                    fltrss[10] = Vert_hsthrm_fltr_2;
                    fltrss[11] = Vert_hsthrm_fltr_3;
                }
                else if (interval == 240)//10)
                {
                    fltrss[15] = Vert_hsthrm_fltr_1;
                    fltrss[16] = Vert_hsthrm_fltr_2;
                    fltrss[17] = Vert_hsthrm_fltr_3;
                }
                else if (interval == 15)
                {
                    fltrss[21] = Vert_hsthrm_fltr_1;
                    fltrss[22] = Vert_hsthrm_fltr_2;
                    fltrss[23] = Vert_hsthrm_fltr_3;
                }
                else if (interval == 30)
                {
                    fltrss[27] = Vert_hsthrm_fltr_1;
                    fltrss[28] = Vert_hsthrm_fltr_2;
                    fltrss[29] = Vert_hsthrm_fltr_3;
                }
                else if (interval == 60)
                {
                    fltrss[33] = Vert_hsthrm_fltr_1;
                    fltrss[34] = Vert_hsthrm_fltr_2;
                    fltrss[35] = Vert_hsthrm_fltr_3;
                }
                else if (interval == 1440)
                {
                    fltrss[39] = Vert_hsthrm_fltr_1;
                    fltrss[40] = Vert_hsthrm_fltr_2;
                    fltrss[41] = Vert_hsthrm_fltr_3;
                }
            }

            if (canvas_prices.Children.Count == 0) return;
            
            if (isGist)
            {
                lock (chart)
                {
                    if (MarcetProfile.Count == 0)//eсли раньше гистограммы не было, то рисуем её
                    {
                        Calculate_hstgrm();
                        canvas_gistogramm.Margin = new Thickness(0, canvas_chart.Margin.Top, 0, 0);
                    }//если раньше гистограмма была, то переразукрашиваем её, если нужно
                    else if (filter_11 != hsthrm_fltr_1 || filter_22 != hsthrm_fltr_2 || filter_33 != hsthrm_fltr_3)
                    {
                        double max_gist_volum = MarcetProfile.Max(mp => mp.cluster.volume);

                        for (int i = 0; i < MarcetProfile.Count; i++)
                            color_gorisontal_gistogram_bar(MarcetProfile[i], max_gist_volum);
                    }
                }
            }
            else if (MarcetProfile.Count > 0)//если раньше гистограмма была, то удаляем её
            {
                lock (chart)
                {
                    canvas_gistogramm.Children.Clear();
                    MarcetProfile.Clear();
                }
            }

            if (vertical_gist)
            {
                lock (chart)
                {
                    if (canv_vert_gstgr.Children.Count == 0)//если раньше гистограммы не было, то
                    {
                        DrawVerticalHistogram();
                    } //если раньше гистограмма была, то переразукрашиваем её, если изменился какой-нить фильтр 
                    else if (vertical_histogram_type_do != vertical_histogram_type_buffer)
                    {
                        DrawVerticalHistogram();
                    }
                    else if ((filter_111 != Vert_hsthrm_fltr_1 || filter_222 != Vert_hsthrm_fltr_2 || filter_333 != Vert_hsthrm_fltr_3) && vertical_histogram_type_buffer == VertcalHistogramType.Volume)
                    {
                        for (int i = 0; i < chart.Count; i++)
                            color_vertical_gistogram_bar(chart[i]);
                    }
                }
            }
            else if (canv_vert_gstgr.Children.Count > 0)//если раньше гистограмма была, то удаляем её
                ClearVerticalHistogamm();
        }

        private void ClearVerticalHistogamm()
        {
            lock (chart)
                canv_vert_gstgr.Children.Clear();
        }

        private void DrawVerticalHistogram()
        {
            if (!Calculate_vertical_gistogramm(0)) return;
            
            double bar_width = bufer_x - 1;
            //if (bufer_x == 1) bar_width = 1;

            double positions_X = Canvas.GetLeft(chart[N_fb].chart_b) - bufer_x;

            for (int i = N_fb; i <= N_lb; i++)
            {
                positions_X += bufer_x;
                if (vertical_histogram_type_buffer == VertcalHistogramType.CumulativeDelta)
                {
                    if (chart[i].VerticalHistogramLine != null)
                    {
                        VerticalHistogramLineSetLeft(i.ToString(), positions_X, bar_width);
                        canv_vert_gstgr.Children.Add(chart[i].VerticalHistogramLine);
                    }
                }
                else
                {
                    chart[i].total_vol_gist.Width = bar_width;
                    Canvas.SetLeft(chart[i].total_vol_gist, positions_X);
                    canv_vert_gstgr.Children.Add(chart[i].total_vol_gist);//попадающие в бордер бары выводим на график
                }
            }

            canv_vert_gstgr.Margin = new Thickness(canvas_chart.Margin.Left, border_chart.Height - VerticalHistohramBarMaxHeight, 0, 0);
        }

        #endregion

        #region Lines

        ChartLines GorizontalActivLine = null, VerticalActivLine = null;
        List<ChartLines> lines_Y = new List<ChartLines>();//горизонтальные линии. обнуляется в Change_contracts
        List<ChartLines> lines_X = new List<ChartLines>();//вертикальные линии. обнуляется в Cick_12
        bool color_choice = false;//разрешает или запрещает создавать новую линию. Если нажали левую кнопку мыши на ценовой или временной шкале - то color_choice = true. Потом если сделали масштабирование то color_choice = false и ф-ции Create_Line_Y или Create_Line_Х не выполняются(return). Если масштабирование было то ф-ции Create_Line_Y или Create_Line_Х выполняются и создаётся линия. И в этих же методах color_choice обнуляется = false до исходного положения. Больше нигде не используется
        bool isLineDeleted = false;//если нажали правую кнопку мыши чтобы удалить активную линию, то isLineDeleted становится  = true, чтобы не вызывалось контекстное меню. Проверяется в методе Opening_Context_Menu()
        Border OpenCloseFrame;// = new Border();//выплывающий лейбл возле гистограммы на активной линии. Определяется в Window_Loaded_1 и label_gistogramm
        Label vertical_gstgrm_vsbl_lbl, horisontalal_gstgrm_vsbl_lbl;///лейбл на активной линии возле гистограммы. Определяется в Window_Loaded_1

        private void Create_Line_Y(object sender, MouseButtonEventArgs e)//отпустили вверх левую кнопку миши на бордере-ценовой шкале
        {
            try
            {
                if (canvas_prices.Children.Count == 0 || button_press || color_choice == false || isEventBusy) return;

                isEventBusy = true;
                mas_Y = false;

                lock (chart)
                {
                    color_choice = false;
                    cursor_pos = Mouse.GetPosition(this);

                    double raznica = (cursor_pos.Y - border_chart.Margin.Top - 1 - canvas_chart.Margin.Top) / chart_mas_y;//
                    int N_activ_price = Convert.ToInt32(Math.Floor(raznica));//определ номер ячейки цены, на котор навели мышу 

                    if (N_activ_price < 0)//если выше мах или ниже мин цены на вертикальной шкале
                    {
                        border_prices.ReleaseMouseCapture();//отпускаем захват мыши
                        isEventBusy = false;
                        return;
                    }

                    if (lines_Y.Count > 0)//если на графике уже есть линии, то проверяем не наведина ли мыша на уже сувществующию линию
                    {
                        int strt_rng = N_activ_price - Convert.ToInt32(Math.Floor(12 / chart_mas_y - raznica + N_activ_price));
                        if (strt_rng < 0)
                            strt_rng = 0;

                        GorizontalActivLine = lines_Y.FindLast(delegate (ChartLines line) { return line.N_cell >= strt_rng && line.N_cell <= N_activ_price; });

                        if (GorizontalActivLine != null)
                        {
                            GorizontalActivLine.line_body.Height = 2;// 1;
                            Canvas.SetZIndex(GorizontalActivLine.line_label, 3);
                            Canvas.SetZIndex(GorizontalActivLine.line_body, 3);
                            lines_Y.Remove(GorizontalActivLine);
                            if (isGist)
                            {
                                label_gistogramm();
                                canvas_gistogramm.Children.Add(horisontalal_gstgrm_vsbl_lbl);
                            }

                            isEventBusy = false;
                            return;
                        }
                    }

                    //сюда дойдём если мыша не наведена на уже сувществующую линию и значит создеём-рисуем новую линию
                    if (N_activ_price > price_canvases.Count - 1)//если выше мах или ниже мин цены на вертикальной шкале
                        border_prices.ReleaseMouseCapture();//отпускаем захват мыши
                    else
                        CreateHorisontalLine(N_activ_price, false);
                }

                isEventBusy = false;
            }
            catch { }
        }

        private void CreateHorisontalLine(int N_activ_price, bool create_cross)
        {
            try
            {
                GorizontalActivLine = CreateHorisontalLine_CommonActions_step_1();
                GorizontalActivLine.price = price_canvases[N_activ_price].price;
                string labelContent = doubleToString(GorizontalActivLine.price, true);
                GorizontalActivLine.line_label_content.Content = labelContent;
                GorizontalActivLine = CreateHorisontalLine_CommonActions_step_2(GorizontalActivLine, N_activ_price);
                Canvas.SetZIndex(GorizontalActivLine.line_body, 5);
                Canvas.SetZIndex(GorizontalActivLine.line_label, 5);
                GorizontalActivLine.line_label_content.Foreground = Brushes.Black;

                if (create_cross)
                {
                    GorizontalActivLine.line_body.Background = Brushes.Gray;
                    GorizontalActivLine.line_label.Background = Brushes.Gray;
                    GorizontalActivLine.line_body.Height = 0.5;
                }
                else
                {
                    GorizontalActivLine.line_body.Background = Brushes.Brown;
                    GorizontalActivLine.line_label.Background = Brushes.Brown;
                    GorizontalActivLine.line_body.Height = 2;//1;
                }

                if (isGist)
                {
                    label_gistogramm();
                    canvas_gistogramm.Children.Add(horisontalal_gstgrm_vsbl_lbl);
                }
            }
            catch { }
        }

        private double Calculate_top_posit_for_horisontal_line_label(double N_cell)
        {
            return Math.Round(N_cell * chart_mas_y - 1, MidpointRounding.AwayFromZero);
        }

        private double Calculate_top_posit_for_horisontal_line_body(double top_position)
        {
            return top_position += 7;
        }

        private void Move_line_Y()
        {
            try
            {
                cursor_pos = Mouse.GetPosition(this);

                lock (chart)
                {
                    if (cursor_pos.Y < border_chart.Margin.Top || cursor_pos.Y > border_chart.Margin.Top + border_chart.Height)
                    //если ушли ниже или выше бордера с графиком(ценовой шкалой), то  линия исчезает
                    {
                        DeleteHorizontalLine();
                        border_prices.ReleaseMouseCapture();//отпускаем мышу от бордер-прайса
                    }
                    else set_new_position_horisontal_line(cursor_pos);
                }
            }
            catch { }
        }

        private void set_new_position_horisontal_line(Point cursor_pos)
        {
            try
            {
                //определ номер ячейки цены, на котор навели мышу 
                int new_N_price = Convert.ToInt32(Math.Floor((cursor_pos.Y - border_chart.Margin.Top - 1 - canvas_chart.Margin.Top) / chart_mas_y));
                if (new_N_price < 0)
                    new_N_price = 0;
                else if (new_N_price > price_canvases.Count - 1)
                    new_N_price = price_canvases.Count - 1;

                //двигаем нарисованную но не закреплённую линию
                //if (new_N_price != GorizontalActivLine.N_cell)  если активировать это условие то плохо работает Mousewheele()
                //{
                GorizontalActivLine.N_cell = new_N_price;
                GorizontalActivLine.price = price_canvases[new_N_price].price;
                string labelContent = doubleToString(GorizontalActivLine.price, true);
                GorizontalActivLine.line_label_content.Content = labelContent;
                double position_Y = Calculate_top_posit_for_horisontal_line_label(new_N_price);
                Canvas.SetTop(GorizontalActivLine.line_label, position_Y);
                Canvas.SetTop(GorizontalActivLine.line_body, Calculate_top_posit_for_horisontal_line_body(position_Y));

                if (isGist)
                    label_gistogramm();
                //}
            }
            catch { }
        }

        private void label_gistogramm()
        {
            try
            {
                int z = MarcetProfile.FindIndex(delegate (Histogramma mp) { return mp.cluster.price == GorizontalActivLine.price; });
                if (z >= 0)
                {
                    string cntnt = ((decimal)MarcetProfile[z].cluster.volume).ToString();// "N0", nfi);
                    horisontalal_gstgrm_vsbl_lbl.Width = 16 + (cntnt.Length - 1) * 7;
                    horisontalal_gstgrm_vsbl_lbl.Content = cntnt;

                    Canvas.SetTop(horisontalal_gstgrm_vsbl_lbl, Math.Round(chart_mas_y * GorizontalActivLine.N_cell - 5, MidpointRounding.AwayFromZero));
                    Canvas.SetLeft(horisontalal_gstgrm_vsbl_lbl, MarcetProfile[z].histogramm_bar.Width + 2);

                    horisontalal_gstgrm_vsbl_lbl.Visibility = Visibility.Visible;
                }
                else
                    horisontalal_gstgrm_vsbl_lbl.Visibility = Visibility.Hidden;
            }
            catch { }
        }

        private void Color_menu_Y(object sender, MouseButtonEventArgs e)//нажали правую кнопку миши на бордере-ценовой шкале
        {
            try
            {
                if (canvas_prices.Children.Count == 0 || button_press || isEventBusy) return;

                isEventBusy = true;

                lock (chart)
                {
                    if (GorizontalActivLine != null)//если есть активная, то удаляем её
                    {
                        DeleteHorizontalLine();
                        border_prices.ReleaseMouseCapture();
                        if (border_chart.IsMouseOver)//если правая кнопка мыши нажата  на бордере графикке то
                            isLineDeleted = true;//то идентифицируем что это для удаления активной линии, чтобы не выводить контекстное меню
                    }
                    else if (lines_Y.Count > 0)//если на графике есть линии
                    {
                        cursor_pos = Mouse.GetPosition(this);

                        double raznica = (cursor_pos.Y - border_chart.Margin.Top - 1 - canvas_chart.Margin.Top) / chart_mas_y;
                        int N_color_choice_price_1 = Convert.ToInt32(Math.Floor(raznica));//определ номер ячейки цены, на котор кликнули левой кнопкой мыщи

                        if (N_color_choice_price_1 < 0)
                        {
                            isEventBusy = false;
                            return;
                        }

                        int strt_rng = N_color_choice_price_1 - Convert.ToInt32(Math.Floor(12 / chart_mas_y - raznica + N_color_choice_price_1));//определяем диапазон цен, на кот будем проверять-есть ли там линии
                        if (strt_rng < 0)
                            strt_rng = 0;

                        ChartLines search_line = lines_Y.FindLast(delegate (ChartLines line) { return line.N_cell >= strt_rng && line.N_cell <= N_color_choice_price_1; });

                        if (search_line != null)//если мыша таки наведена на какую-нить линию то  вызываем окно для выбора цвета
                        {
                            int z_index = Canvas.GetZIndex(search_line.line_label);
                            Canvas.SetZIndex(search_line.line_label, 3);
                            Canvas.SetZIndex(search_line.line_body, 3);
                            search_line.line_body.Height = 2;// 1;
                            DoEvents();

                            System.Windows.Forms.ColorDialog colorDialog = new System.Windows.Forms.ColorDialog();

                            if (colorDialog.ShowDialog() != System.Windows.Forms.DialogResult.Cancel)
                            {
                                System.Drawing.Color color1 = colorDialog.Color;
                                Color color = Colors.Red;
                                color.A = color1.A;
                                color.R = color1.R;
                                color.G = color1.G;
                                color.B = color1.B;
                                Brush brush = new SolidColorBrush(color);
                                brush.Freeze();
                                search_line.line_body.Background = brush;
                                search_line.line_label.Background = brush;
                                search_line.line_label_content.Foreground = GetLineForeground(brush);
                            }

                            Canvas.SetZIndex(search_line.line_label, z_index);
                            Canvas.SetZIndex(search_line.line_body, z_index);
                            search_line.line_body.Height = 1;// 0.5;
                        }
                    }
                }

                isEventBusy = false;
            }
            catch { }
        }

        private void DeleteHorizontalLine()
        {
            try
            {
                LinesCanvas.Children.Remove(GorizontalActivLine.line_body);//удаляем линию
                canvas_prices.Children.Remove(GorizontalActivLine.line_label);
                if (isGist)
                    canvas_gistogramm.Children.Remove(horisontalal_gstgrm_vsbl_lbl);//удаляем лейбл возле горизонтальной гистограммы
                GorizontalActivLine = null;//обнуляем линию
            }
            catch { }
        }

        private void secure_activ_gorizontal_line()
        {
            try
            {
                lock (chart)
                {
                    int est_lin = lines_Y.FindLastIndex(delegate (ChartLines line) { return line.N_cell == GorizontalActivLine.N_cell; });

                    if (est_lin >= 0)//если под закрепляемой линией на этой же  есть ещё линия
                    {
                        Canvas.SetZIndex(GorizontalActivLine.line_body, 2);
                        Canvas.SetZIndex(GorizontalActivLine.line_label, 2);
                        Canvas.SetZIndex(lines_Y[est_lin].line_body, 1);
                        Canvas.SetZIndex(lines_Y[est_lin].line_label, 1);
                    }
                    else
                    {
                        Canvas.SetZIndex(GorizontalActivLine.line_body, 1);
                        Canvas.SetZIndex(GorizontalActivLine.line_label, 1);
                    }

                    GorizontalActivLine.line_body.Height = 1;// 0.5;
                    lines_Y.Add(GorizontalActivLine);
                    GorizontalActivLine = null;
                    if (isGist)
                        canvas_gistogramm.Children.Remove(horisontalal_gstgrm_vsbl_lbl);

                    border_prices.ReleaseMouseCapture();
                }
            }
            catch { }
        }

        private void Resize_horizontal_line()
        {
            try
            {
                for (int z = 0; z < lines_Y.Count; z++)
                {
                    if (lines_Y[z].line_body.Parent != null)
                    {
                        double new_posit_Y = Calculate_top_posit_for_horisontal_line_label(lines_Y[z].N_cell);
                        Canvas.SetTop(lines_Y[z].line_label, new_posit_Y);
                        Canvas.SetTop(lines_Y[z].line_body, Calculate_top_posit_for_horisontal_line_body(new_posit_Y));
                    }
                }
            }
            catch { }
        }

        private void DrawHorizontalLines()
        {
            try
            {
                for (int z = 0; z < lines_Y.Count; z++)
                {
                    if (lines_Y[z].line_body.Parent == null && lines_Y[z].price <= price_canvases[0].price && lines_Y[z].price >= price_canvases.Last().price)
                    {
                        int N_cel = Convert.ToInt32(Vars.MathRound(Calculate_N_cel_in_Price_scale_list(lines_Y[z].price)));
                        lines_Y[z] = CreateHorisontalLine_CommonActions_step_2(lines_Y[z], N_cel);
                        lines_Y[z].line_body.Width = border_chart.Width * 3;
                    }
                    else
                        lines_Y[z].N_cell = -1;
                }
            }
            catch { }
        }

        private void Save_lines()
        {
            try
            {
                string file_p = Vars.core_path + "\\tickdata\\" + instrument;
                if (Directory.Exists(file_p) == false)//проверяем существует ли такая папка - куда я хочу записывать полученные от сервера данные.
                    Directory.CreateDirectory(file_p);//если нет, то создаем её
                file_p += "\\lines.txt";

                lock (Vars.object_for_lock)
                {
                    using (StreamWriter wrtr = File.CreateText(file_p))
                    {
                        for (int i = 0; i < lines_Y.Count; i++)
                        {
                            string prc_l = ((decimal)lines_Y[i].price).ToString();
                            prc_l = prc_l.Replace(",", ".");
                            string ln = prc_l + "-" + bc.ConvertToString(lines_Y[i].line_body.Background);
                            wrtr.WriteLine(ln);
                        }

                        wrtr.Close();
                    }
                }

                lines_Y.Clear();
            }
            catch { }
        }

        private void ReadLineFile()
        {
            string file_path = Vars.core_path + "\\tickdata\\" + instrument + "\\lines.txt";
            if (File.Exists(file_path))
            {
                string[] lns;
                lock (Vars.object_for_lock)
                    lns = File.ReadAllLines(file_path);
                for (int i = 0; i < lns.Length; i++)
                {
                    try
                    {
                        ChartLines current_line = CreateHorisontalLine_CommonActions_step_1();

                        string[] lne = lns[i].Split('-');
                        double price_line = Convert.ToDouble(lne[0], Vars.FormatInfo);
                        current_line.price = price_line;
                        string labelContent = doubleToString(price_line, true);
                        current_line.line_label_content.Content = labelContent;
                        Brush brush = (Brush)bc.ConvertFrom(lne[1]);
                        current_line.line_label_content.Foreground = GetLineForeground(brush);
                        current_line.line_label.Background = brush;
                        current_line.line_label.Background.Freeze();
                        Canvas.SetZIndex(current_line.line_label, 1);
                        current_line.line_body.Height = 1;// 0.5;
                        current_line.line_body.Background = brush;
                        current_line.line_body.Background.Freeze();
                        Canvas.SetZIndex(current_line.line_body, 1);
                        lines_Y.Add(current_line);
                    }
                    catch
                    {
                        MessageBox.Show("Error at file 'lines.txt'.\n    Number = " + (i + 1).ToString());
                    }
                }
            }
        }

        private ChartLines CreateHorisontalLine_CommonActions_step_1()
        {
            ChartLines current_line = new ChartLines();

            current_line.line_label_content = new Label();
            Canvas.SetTop(current_line.line_label_content, -7);
            Canvas.SetLeft(current_line.line_label_content, -3);

            current_line.line_label = new Canvas();
            current_line.line_label.Width = 45;
            current_line.line_label.Height = 13;
            current_line.line_label.Children.Add(current_line.line_label_content);

            current_line.line_body = new Canvas();
            current_line.line_body.Width = border_chart.Width*3;

            return current_line;
        }

        private ChartLines CreateHorisontalLine_CommonActions_step_2(ChartLines current_line, int N_cell)
        {
            current_line.N_cell = N_cell;
            double new_posit_Y = Calculate_top_posit_for_horisontal_line_label(N_cell);
            Canvas.SetTop(current_line.line_label, new_posit_Y);
            canvas_prices.Children.Add(current_line.line_label);
            Canvas.SetTop(current_line.line_body, Calculate_top_posit_for_horisontal_line_body(new_posit_Y));
            LinesCanvas.Children.Add(current_line.line_body);

            return current_line;
        }


        //VerticalLines

        private void Create_Line_X(object sender, MouseButtonEventArgs e)//отпустили вверх левую кнопку миши на бордере-временной шкале
        {
            try
            {
                if (canvas_prices.Children.Count == 0 || button_press || color_choice == false || isEventBusy)
                    return;

                isEventBusy = true;
                mas_X = false;

                lock (chart)
                {
                    color_choice = false;
                    cursor_pos = Mouse.GetPosition(this);

                    double mouse_posit = (cursor_pos.X - border_chart.Margin.Left - 1 - canvas_chart.Margin.Left - Canvas.GetLeft(chart[N_fb].chart_b));//расстояние в пикселях от первого выведенного бара chart[N_fb] до мыши(оно может быть отрицательным, если мыша слева от этого бара и N_fb = 0)
                                                                                                                                                        //mouse_posit / bufer_x - коллич баров, помещающихся му первым выведенным баром chart[N_fb] и мышей
                    int N_activ_time = N_fb + Convert.ToInt32(Math.Floor(mouse_posit / bufer_x));//определ номер временной ячейки, на котор навели мышу 
                                                                                                 //если N_fb = 0 и mouse_posit < 0, то и N_activ_time < 0

                    if (lines_X.Count > 0)//если на графике уже есть линии, то проверяем не наведина ли мыша на уже сувществующию линию
                    {
                        VerticalActivLine = lines_X.FindLast(delegate (ChartLines line) { return line.N_cell == N_activ_time; });
                        if (VerticalActivLine == null)
                        {
                            double bar_width = bufer_x - 1;
                            //if (bufer_x == 1) bar_width = 1;

                            if (bar_width < 2 * otstup - 7)//если ширина бара меньше чем ширина лейбла вертикальной линии
                            {
                                double bike_end = (2 * otstup - 7 - bar_width) / 2;//расстояние в пикселях между левым краем лейбла вертикальной линии и левым краем бара, на котором стоит эта линия
                                int last_left_N = N_activ_time, last_right_N = N_activ_time;

                                for (double i = 1; i <= bike_end; i++)// в цыкле будем двигать мышу влево-вправо по 1-му пикселю и первычислять N_activ_time
                                {
                                    double m = mouse_posit - i;
                                    int N_activ_tm = N_fb + Convert.ToInt32(Math.Floor(m / bufer_x));
                                    if (N_activ_tm >= N_fb && N_activ_tm != last_left_N)
                                    {
                                        last_left_N = N_activ_tm;
                                        VerticalActivLine = lines_X.FindLast(delegate (ChartLines line) { return line.N_cell == N_activ_tm; });
                                        if (VerticalActivLine != null) break;
                                    }

                                    m = mouse_posit + i;
                                    N_activ_tm = N_fb + Convert.ToInt32(Math.Floor(m / bufer_x));
                                    if (N_activ_tm <= N_lb && N_activ_tm != last_right_N)
                                    {
                                        last_right_N = N_activ_tm;
                                        VerticalActivLine = lines_X.FindLast(delegate (ChartLines line) { return line.N_cell == N_activ_tm; });
                                        if (VerticalActivLine != null) break;
                                    }
                                }
                            }
                        }

                        if (VerticalActivLine != null)//если мыша таки наведена на какую-нить линию, то делаем её активной
                        {
                            VerticalActivLine.line_body.Width = 2;// 1;
                            Canvas.SetZIndex(VerticalActivLine.line_body, 3);
                            Canvas.SetZIndex(VerticalActivLine.line_label_content, 3);
                            lines_X.Remove(VerticalActivLine);
                            if (vertical_gist)
                            {
                                label_vertical_gistogramm();
                                canvas_for_label.Children.Add(vertical_gstgrm_vsbl_lbl);
                            }

                            SetOpenCloseLabel();
                            canvas_for_label.Children.Add(OpenCloseFrame);

                            isEventBusy = false;
                            return;
                        }
                    }

                    //сюда дойдём если мыша не наведена на уже сувществующую линию и значит создеём новую линию
                    if (N_activ_time < 0 || N_activ_time > chart.Count - 1)
                        border_times.ReleaseMouseCapture();
                    else
                        CreateVerticalLine(N_activ_time, false);

                    isEventBusy = false;
                }
            }
            catch { }
        }

        private void CreateVerticalLine(int N_activ_time, bool create_cross)
        {
            try
            {
                VerticalActivLine = CreateVerticalLine_CommonActions(chart[N_activ_time].time_of_bar, N_activ_time.ToString());

                Canvas.SetZIndex(VerticalActivLine.line_label_content, 3);
                VerticalActivLine.line_label_content.Foreground = Brushes.Black;

                if (create_cross)//если мы пришли сюда после нажатия колеса мыши
                {
                    VerticalActivLine.line_label_content.Background = Brushes.Gray;
                    VerticalActivLine.line_body.Background = Brushes.Gray;
                    VerticalActivLine.line_body.Width = 0.5;
                }
                else
                {
                    VerticalActivLine.line_label_content.Background = Brushes.Brown;
                    VerticalActivLine.line_body.Background = Brushes.Brown;
                    VerticalActivLine.line_body.Width = 2;// 1;
                }

                Canvas.SetZIndex(VerticalActivLine.line_body, 3);

                if (vertical_gist)
                {
                    label_vertical_gistogramm();
                    canvas_for_label.Children.Add(vertical_gstgrm_vsbl_lbl);
                }

                SetOpenCloseLabel();
                canvas_for_label.Children.Add(OpenCloseFrame);
            }
            catch { }
        }

        private ChartLines CreateVerticalLine_CommonActions(DateTime? time_of_bar, string sN_cell)
        {
            ChartLines current_line = new ChartLines();
            try
            {
                current_line.date = time_of_bar;
                int N_cell = Convert.ToInt32(sN_cell);
                current_line.N_cell = N_cell;
                //current_line.bar_color = chart[N_cell].color;
                current_line.line_body = new Canvas();
                current_line.line_body.Height = border_chart.Height * 3;
                double new_posit_X = Calculate_left_position_for_vertical_line_body(current_line.N_cell);
                Canvas.SetLeft(current_line.line_body, new_posit_X);
                canvas_vertical_line.Children.Add(current_line.line_body);

                current_line.line_label_content = new Label();
                current_line.line_label_content.Height = 15;
                current_line.line_label_content.Width = otstup * 2 - 7;
                current_line.line_label_content.Padding = new Thickness(1, 0, 0, 0);
                current_line.line_label_content.Content = set_vertical_line_time_label(time_of_bar);
                Canvas.SetLeft(current_line.line_label_content, Calculate_left_position_for_vertical_line_label(new_posit_X));
                canvas_times.Children.Add(current_line.line_label_content);
            }
            catch { }
            return current_line;
        }

        private string set_vertical_line_time_label(DateTime? bar_time)
        {
            string bartime = "";

            try
            {
                if (interval == 1440)
                {
                    bartime = bar_time.Value.Day.ToString();
                    if (bar_time.Value.Day < 10)
                        bartime = "0" + bartime;
                    string mnf = bar_time.Value.Month.ToString();
                    if (bar_time.Value.Month < 10)
                        mnf = "0" + mnf;
                    bartime = bartime + "/" + mnf;
                }
                else bartime = bar_time.Value.ToString("HH:mm");
            }
            catch { }

            return bartime;
        }

        private void Move_line_X()
        {
            try
            {
                cursor_pos = Mouse.GetPosition(this);

                lock (chart)
                {
                    if (cursor_pos.X < border_chart.Margin.Left || cursor_pos.X > border_chart.Margin.Left + border_chart.Width || cursor_pos.Y < border_chart.Margin.Top)
                    //если ушли за бордер с графиком, то  линия исчезает
                    {
                        DeleteVerticalLine();
                        VerticalActivLine = null;

                        border_times.ReleaseMouseCapture();
                    }
                    else set_new_position_vertical_line(cursor_pos);
                }
            }
            catch { }
        }

        private void set_new_position_vertical_line(Point cursor_pos)
        {
            try
            {
                //определ номер ячейки цены, на котор навели мышу 
                double mouse_posit = (cursor_pos.X - border_chart.Margin.Left - 1 - canvas_chart.Margin.Left - Canvas.GetLeft(chart[N_fb].chart_b)) / bufer_x;
                int new_N_time = N_fb + Convert.ToInt32(Math.Floor(mouse_posit));//определ номер ячейки, на котор навели мышу

                if (new_N_time < N_fb)
                    new_N_time = N_fb;
                else if (new_N_time > N_lb)
                    new_N_time = N_lb;

                //двигаем нарисованную но не закреплённую линию
                //if (new_N_time != VerticalActivLine.N_cell)  если активировать это условие то плохо работает Mousewheel()
                //{
                VerticalActivLine.N_cell = new_N_time;
                VerticalActivLine.date = chart[new_N_time].time_of_bar;
                VerticalActivLine.line_label_content.Content = set_vertical_line_time_label(chart[VerticalActivLine.N_cell].time_of_bar);
               // VerticalActivLine.bar_color = chart[new_N_time].color;

                double new_posit_X = Calculate_left_position_for_vertical_line_body(VerticalActivLine.N_cell);
                Canvas.SetLeft(VerticalActivLine.line_body, new_posit_X);
                Canvas.SetLeft(VerticalActivLine.line_label_content, Calculate_left_position_for_vertical_line_label(new_posit_X));

                if (vertical_gist)
                    label_vertical_gistogramm();

                if (OpenCloseFrame.Parent != null)//cross_move == true 
                    SetOpenCloseLabel();
                //}
            }
            catch { }
        }

        private double Calculate_left_position_for_vertical_line_body(int N_cell)
        {
            double bar_width = bufer_x - 1;
            //if (bufer_x == 1) bar_width = 1;

            return Math.Floor(Canvas.GetLeft(chart[N_fb].chart_b) + (N_cell - N_fb) * bufer_x + bar_width / 2);
        }

        private double Calculate_left_position_for_vertical_line_label(double position_X)
        {
            return position_X - otstup + 5;
        }

        private void label_vertical_gistogramm()
        {
            try
            {
                if (VerticalActivLine.N_cell == -1) return;
                string content_volume = "";

                if (vertical_histogram_type_buffer == VertcalHistogramType.CumulativeDelta)
                {
                    if (chart[VerticalActivLine.N_cell].bar_highs != 0 && chart[VerticalActivLine.N_cell].VerticalHistogramLine != null)
                        content_volume = ((decimal)chart[VerticalActivLine.N_cell].cumulative_delta).ToString();// "N0", nfi);
                }
                else if (chart[VerticalActivLine.N_cell].vertic_gist_volum != 0)
                    content_volume = ((decimal)chart[VerticalActivLine.N_cell].vertic_gist_volum).ToString();// "N0", nfi);

                if (content_volume != "")
                {
                    vertical_gstgrm_vsbl_lbl.Width = 16 + (content_volume.Length - 1) * 7;
                    vertical_gstgrm_vsbl_lbl.Content = content_volume;
                    Canvas.SetTop(vertical_gstgrm_vsbl_lbl, border_chart.Height - VerticalHistohramBarMaxHeight - 25);
                    double bar_width = bufer_x - 1;
                    //if (bufer_x == 1) bar_width = 1;
                    Canvas.SetLeft(vertical_gstgrm_vsbl_lbl, Math.Round(Canvas.GetLeft(chart[VerticalActivLine.N_cell].chart_b) + canvas_chart.Margin.Left - vertical_gstgrm_vsbl_lbl.Width / 2 + bar_width / 2, MidpointRounding.AwayFromZero));
                    vertical_gstgrm_vsbl_lbl.Visibility = Visibility.Visible;
                }
                else vertical_gstgrm_vsbl_lbl.Visibility = Visibility.Hidden;
            }
            catch { }
        }

        private void Color_menu_X(object sender, MouseButtonEventArgs e)//нажали правую кнопку миши на бордере-временной шкале
        {
            if (canvas_prices.Children.Count == 0 || button_press || isEventBusy) return;
            try
            {
                isEventBusy = true;

                lock (chart)
                {
                    if (VerticalActivLine != null)
                    {
                        DeleteVerticalLine();
                        border_times.ReleaseMouseCapture();
                        if (border_chart.IsMouseOver)//если правая кнопка мыши нажата  на бордере графикке то
                            isLineDeleted = true;//то идентифицируем что это для удаления активной линии, чтобы не выводить контекстное меню
                    }
                    else if (lines_X.Count > 0)//если на графике есть линии
                    {
                        cursor_pos = Mouse.GetPosition(this);

                        double mouse_posit = (cursor_pos.X - border_chart.Margin.Left - 1 - canvas_chart.Margin.Left - Canvas.GetLeft(chart[N_fb].chart_b));
                        int N_color_choice_time = N_fb + Convert.ToInt32(Math.Floor(mouse_posit / bufer_x));//определ номер ячейки временной шлалы, на котор навели мышу 

                        ChartLines search_line = null;
                        if (lines_X.Count > 0)
                            search_line = lines_X.FindLast(delegate (ChartLines line) { return line.N_cell == N_color_choice_time; });

                        if (search_line == null)
                        {
                            if (search_line == null)
                            {
                                if (bufer_x - 1 < 2 * otstup - 7)//если ширина бара меньше чем ширина лейбла вертикальной линии
                                {
                                    double bike_end = (2 * otstup - 7 - (bufer_x - 1)) / 2;//расстояние в пикселях между левым краешком лейбла вертикальной линии и левым краешком бара, на котором стоит эта линия
                                    int last_left_N = N_color_choice_time, last_right_N = N_color_choice_time;

                                    for (double i = 1; i <= bike_end; i++)// в цыкле будем двигать мышу влево-вправо по 1-му пикселю и первычислять N_color_choice_time
                                    {
                                        N_color_choice_time = N_fb + Convert.ToInt32(Math.Floor((mouse_posit - i) / bufer_x));
                                        if (N_color_choice_time >= N_fb && N_color_choice_time != last_left_N)
                                        {
                                            last_left_N = N_color_choice_time;

                                            if (lines_X.Count > 0)
                                            {
                                                search_line = lines_X.FindLast(delegate (ChartLines line) { return line.N_cell == N_color_choice_time; });
                                                if (search_line != null)
                                                    break;
                                            }
                                        }

                                        N_color_choice_time = N_fb + Convert.ToInt32(Math.Floor((mouse_posit + i) / bufer_x));
                                        if (N_color_choice_time <= N_lb && N_color_choice_time != last_right_N)
                                        {
                                            last_right_N = N_color_choice_time;

                                            if (lines_X.Count > 0)
                                            {
                                                search_line = lines_X.FindLast(delegate (ChartLines line) { return line.N_cell == N_color_choice_time; });
                                                if (search_line != null)
                                                    break;
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        if (search_line != null)//если мыша таки наведена на какую-нить линию, то вызываем окно для выбора цвета
                        {
                            int z_index = Canvas.GetZIndex(search_line.line_label_content);
                            Canvas.SetZIndex(search_line.line_label_content, 3);
                            Canvas.SetZIndex(search_line.line_body, 3);
                                search_line.line_body.Width = 2;// 1;

                            DoEvents();

                            System.Windows.Forms.ColorDialog colorDialog = new System.Windows.Forms.ColorDialog();
                            if (colorDialog.ShowDialog() != System.Windows.Forms.DialogResult.Cancel)
                            {
                                System.Drawing.Color color1 = colorDialog.Color;
                                Color color = Colors.Red;
                                color.A = color1.A;
                                color.R = color1.R;
                                color.G = color1.G;
                                color.B = color1.B;
                                Brush brush = new SolidColorBrush(color);
                                brush.Freeze();
                                
                                    search_line.line_body.Background = brush;//запоминаем новый цвет линии в массиве для линий 
                                    search_line.line_label_content.Background = brush;
                                    search_line.line_label_content.Foreground = GetLineForeground(brush);
                            }

                            Canvas.SetZIndex(search_line.line_label_content, z_index);
                            Canvas.SetZIndex(search_line.line_body, z_index);
                                search_line.line_body.Width = 1;// 0.5;
                        }
                    }
                }

                isEventBusy = false;
            }
            catch { }
        }

        private void DeleteVerticalLine()
        {
            try
            {
                canvas_vertical_line.Children.Remove(VerticalActivLine.line_body);
                canvas_times.Children.Remove(VerticalActivLine.line_label_content);
                VerticalActivLine = null;
                if (vertical_gist)
                    canvas_for_label.Children.Remove(vertical_gstgrm_vsbl_lbl);

                canvas_for_label.Children.Remove(OpenCloseFrame);
            }
            catch { }
        }

        private void set_vertical_lines_coordinat(List<ChartLines> current_vertical_lines_list)
        {
            try
            {
                double bar_width = bufer_x - 1;
                //if (bufer_x == 1) bar_width = 1;

                for (int i = 0; i < current_vertical_lines_list.Count; i++)
                {
                    if (current_vertical_lines_list[i].N_cell == -1) continue;

                    double new_posit_X = Calculate_left_position_for_vertical_line_body(current_vertical_lines_list[i].N_cell);
                    Canvas.SetLeft(current_vertical_lines_list[i].line_body, new_posit_X);
                    Canvas.SetLeft(current_vertical_lines_list[i].line_label_content, Calculate_left_position_for_vertical_line_label(new_posit_X));

                    if (current_vertical_lines_list[i].line_label_content.Parent == null)
                        canvas_times.Children.Add(current_vertical_lines_list[i].line_label_content);
                }
            }
            catch { }
        }

        private void secure_activ_vertical_line()
        {
            try
            {
                lock (chart)
                {
                    int est_lin = lines_X.FindLastIndex(delegate (ChartLines line) { return line.N_cell == VerticalActivLine.N_cell; });

                    if (est_lin >= 0)//если под закрепляемой линией на этой же  есть ещё линия
                    {
                        Canvas.SetZIndex(VerticalActivLine.line_body, 2);
                        Canvas.SetZIndex(VerticalActivLine.line_label_content, 2);
                        Canvas.SetZIndex(lines_X[est_lin].line_body, 1);
                        Canvas.SetZIndex(lines_X[est_lin].line_label_content, 1);
                    }
                    else
                    {
                        Canvas.SetZIndex(VerticalActivLine.line_body, 1);
                        Canvas.SetZIndex(VerticalActivLine.line_label_content, 1);
                    }

                    VerticalActivLine.line_body.Width = 1;// 0.5;
                    lines_X.Add(VerticalActivLine);
                    VerticalActivLine = null;
                    if (vertical_gist)
                        canvas_for_label.Children.Remove(vertical_gstgrm_vsbl_lbl);

                    canvas_for_label.Children.Remove(OpenCloseFrame);
                    border_times.ReleaseMouseCapture();
                }
            }
            catch { }
        }
        
        private void DrawVerticalLines()
        {
            try
            {
                    foreach (ChartLines current_line in lines_X)
                    {
                        DateTime? line_new_time = Set_vertical_line_new_time(current_line.date, interval);
                        int N_cell_current_line = chart.FindIndex(delegate (Chart current_chart) { return line_new_time == current_chart.time_of_bar; });
                        current_line.N_cell = N_cell_current_line;

                        if (N_cell_current_line >= 0)
                        {
                            current_line.line_label_content.Content = set_vertical_line_time_label(chart[N_cell_current_line].time_of_bar);
                            current_line.line_label_content.Width = otstup * 2 - 7;
                            canvas_vertical_line.Children.Add(current_line.line_body);
                        }

                        current_line.line_body.Height = border_chart.Height * 3;
                    }
                

                set_vertical_lines_coordinat(lines_X);
            }
            catch { }
        }

        private DateTime? Set_vertical_line_new_time(DateTime? current_line_time, double intrv)
        {
            int sec = 0, minute = 0, hour = current_line_time.Value.Hour, year = current_line_time.Value.Year, month = current_line_time.Value.Month, day = current_line_time.Value.Day;
            try
            {
                if (intrv < 60)
                    minute = Convert.ToInt32(Math.Floor(current_line_time.Value.Minute / intrv) * intrv);
            }
            catch { }

            return new DateTime(year, month, day, hour, minute, sec);
        }

        private void set_activ_lines_new_positions()
        {
            try
            {
                cursor_pos = Mouse.GetPosition(this);

                if (VerticalActivLine != null)
                {
                    set_new_position_vertical_line(cursor_pos);

                    if (VerticalActivLine.line_label_content.Parent == null)//если в Positionirov() или в RT_ReDraw_Chart_for_tickgroup() очищался canvas_times
                        canvas_times.Children.Add(VerticalActivLine.line_label_content);
                }

                if (GorizontalActivLine != null)
                    set_new_position_horisontal_line(cursor_pos);
            }
            catch { }
        }

        private void Opening_Context_Menu(object sender, ContextMenuEventArgs e)//метод вызывается перед появлением контекстного меню
        {
            if (isLineDeleted)//если правую кнопку мыши над бордером-графиком нажали чтобы удалить активную линию то
            {
                e.Handled = true;//то контекстное меню не выводим
                isLineDeleted = false;
            }
        }

        private void Determine_new_N_cell_of_vertical_lines(List<ChartLines> current_vertical_lines_list)
        {
            foreach (ChartLines current_line in current_vertical_lines_list)
                Determine_new_N_cell_of_vertical_line(current_line);
        }

        private void Determine_new_N_cell_of_vertical_line(ChartLines current_line)
        {
            int new_N_cell = -1;
            new_N_cell = chart.FindIndex(delegate(Chart current_bar) { return current_bar.time_of_bar == current_line.date; });
            if (new_N_cell >= 0)
                current_line.N_cell = new_N_cell;
        }

        private Brush GetLineForeground(Brush recive_brush)
        {
            Brush return_brush;

            if (bc.ConvertToString(recive_brush) == "#FF0000FF" || bc.ConvertToString(recive_brush) == "#FF000000" || bc.ConvertToString(recive_brush) == "#FF400000" || bc.ConvertToString(recive_brush) == "#FF000080" || bc.ConvertToString(recive_brush) == "#FF000040" || bc.ConvertToString(recive_brush) == "#FF400040" || bc.ConvertToString(recive_brush) == "#FF400080" || bc.ConvertToString(recive_brush) == "#FF0000A0")
                return_brush = Brushes.LightGray;
            else return_brush = Brushes.Black;

            return return_brush;
        }

        class ChartLines
        {
            public Canvas line_body { get; set; }//линия
            public Canvas line_label { get; set; }//канвас с лейблом-ценой линии, добавляется-выводится на ценовой шкале
            public Label line_label_content { get; set; }//лейбл с ценой линии, добавляется в верхний канвас
            public int N_cell { get; set; }
            public double price { get; set; }
            public DateTime? date { get; set; }
            //public int bar_color { get; set; }
            public double PriceCursor_N_Cell { get; set; }
        }

        #endregion

        #region PriceCursor

        ChartLines PriceCursor = null;

        private void SetPriceCursor(bool realtime)
        {
            double last_price, lastPriceOriginal;
            if (!realtime)
                last_price = lastPriceOriginal = chart.Last().close_price;
            else if (summary_t != null)
            {
                last_price = summary_t.price;
                lastPriceOriginal = summary_t.originalPrice;
            }
            else
                return;

            double N_cel_last_price_1 = Calculate_N_cel_in_Price_scale_list(last_price);

            string labelContent = ((decimal)lastPriceOriginal).ToString();
            PriceCursor.line_label_content.Content = labelContent;
            PriceCursor.line_label.Width = labelContent.Length * 7 + 1;

            if (!realtime || N_cel_last_price_1 != PriceCursor.PriceCursor_N_Cell)
            {
                PriceCursor.PriceCursor_N_Cell = N_cel_last_price_1;
                PriceCursorSetTop();
            }

            if (!realtime)
            {
                LinesCanvas.Children.Add(PriceCursor.line_label);
                LinesCanvas.Children.Add(PriceCursor.line_body);
            }
        }

        private void PriceCursorSetTop()
        {
            double posit_y = Calculate_top_posit_for_horisontal_line_label(PriceCursor.PriceCursor_N_Cell);
            Canvas.SetTop(PriceCursor.line_label, posit_y);
            Canvas.SetTop(PriceCursor.line_body, posit_y + 7);
        }

        private void PriceCursorSetLeft(double pozt_x)
        {
            if (N_lb == chart.Count - 1)//линия-курсор-указатель последней цены - только горизонтальная координата. А вертикальн устанавлив в Chart_apdate и Click_12, Mod_Bars_Y
            {
                double posX = Canvas.GetLeft(chart.Last().chart_b) + pozt_x + bufer_x - 1;
                Canvas.SetLeft(PriceCursor.line_body, posX);
                Canvas.SetLeft(PriceCursor.line_label, posX + 30);
            }
            else
            {
                Canvas.SetLeft(PriceCursor.line_body, border_chart.Width);
                Canvas.SetLeft(PriceCursor.line_label, border_chart.Width);
            }

            //PriceCursor.line_body.Width = border_chart.Width;
        }

        private void CreatePriceCursor()
        {
            PriceCursor = CreateHorisontalLine_CommonActions_step_1();
            PriceCursor.line_body.Width = 30;

            PriceCursor.line_label.Background = Brushes.LightGray;
            PriceCursor.line_body.Background = Brushes.Gray;
            Canvas.SetZIndex(PriceCursor.line_label, 4);
            Canvas.SetZIndex(PriceCursor.line_body, 4);
            PriceCursor.line_body.Height = 1;
            PriceCursor.line_label_content.Foreground = Brushes.Black;
        }

        #endregion

        #region Filters

        public int volume_filter_for_cluster_1 = 0, volume_filter_for_cluster_2 = 0, volume_filter_for_cluster_3 = 0;//фильтра для обьёмов

        public bool max_volume_in_bar = false;//выделение мах объёма в баре
        public bool isAutoDetermineVolumeFilters_for_Clusres = false;
        double auto_volume_filter_for_cluster_1 = 0, auto_volume_filter_for_cluster_2 = 0, auto_volume_filter_for_cluster_3 = 0;

        private void Filtersss(object sender, RoutedEventArgs e)//изменяем фильтрa
        {
            if (instrument == "" || button_press) return;

            int filter_11 = volume_filter_for_cluster_1;
            int filter_22 = volume_filter_for_cluster_2;
            int filter_33 = volume_filter_for_cluster_3;

            bool max_volume_bar_bufer = max_volume_in_bar, isAutoDetermineVolumeFilters_do = isAutoDetermineVolumeFilters_for_Clusres;

            PopupFilters filterform = new PopupFilters(this);
            filterform.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            filterform.ShowDialog();

            bool reDraw_cluster = false;

            if (filter_11 != volume_filter_for_cluster_1 || filter_22 != volume_filter_for_cluster_2 || filter_33 != volume_filter_for_cluster_3)
            {
                reDraw_cluster = true;

               if (interval == 1)
                {
                    fltrss[0] = volume_filter_for_cluster_1;
                    fltrss[1] = volume_filter_for_cluster_2;
                    fltrss[2] = volume_filter_for_cluster_3;
                }
                else if (interval == 5)
                {
                    fltrss[6] = volume_filter_for_cluster_1;
                    fltrss[7] = volume_filter_for_cluster_2;
                    fltrss[8] = volume_filter_for_cluster_3;
                }
                else if (interval == 240)
                {
                    fltrss[12] = volume_filter_for_cluster_1;
                    fltrss[13] = volume_filter_for_cluster_2;
                    fltrss[14] = volume_filter_for_cluster_3;
                }
                else if (interval == 15)
                {
                    fltrss[18] = volume_filter_for_cluster_1;
                    fltrss[19] = volume_filter_for_cluster_2;
                    fltrss[20] = volume_filter_for_cluster_3;
                }
                else if (interval == 30)
                {
                    fltrss[24] = volume_filter_for_cluster_1;
                    fltrss[25] = volume_filter_for_cluster_2;
                    fltrss[26] = volume_filter_for_cluster_3;
                }
                else if (interval == 60)
                {
                    fltrss[30] = volume_filter_for_cluster_1;
                    fltrss[31] = volume_filter_for_cluster_2;
                    fltrss[32] = volume_filter_for_cluster_3;
                }
                else if (interval == 1440)
                {
                    fltrss[36] = volume_filter_for_cluster_1;
                    fltrss[37] = volume_filter_for_cluster_2;
                    fltrss[38] = volume_filter_for_cluster_3;
                }
            }
            else if (max_volume_in_bar != max_volume_bar_bufer || isAutoDetermineVolumeFilters_do != isAutoDetermineVolumeFilters_for_Clusres)
                reDraw_cluster = true;

            if (canvas_prices.Children.Count == 0 || !reDraw_cluster)
                return;

            lock (chart)
            {
                for (int i = 0; i < chart.Count; i++)
                {
                    Chart current_bar = chart[i];
                    if (chart[i].bar_highs == 0) continue;
                    if (current_bar.canv_filters != null)
                    {
                        current_bar.filters.Children.Clear();
                        current_bar.canv_filters.Clear();
                    }

                    double m_v_b = 0;
                    if (max_volume_in_bar)
                        m_v_b = current_bar.Bars.Max(b => b.volume);


                    AutoDetermineVolumeFilters(i.ToString());
                    double[] filtra = DefineVolumeFilters();

                    for (int x = 0; x < current_bar.Bars.Count; x++)
                    {
                        double current_volume = current_bar.Bars[x].volume;
                        double N_claster = Vars.MathRound((current_bar.bar_highs - current_bar.Bars[x].price) / price_step);
                        Set_color_filter(current_bar, N_claster, current_volume, x.ToString(), filtra[0], filtra[1], filtra[2], m_v_b);
                    }
                }

                bool isRenderTransform = true;

                if (isClusterChart && bufer_x >= max_width_bar + 1 && chart_mas_y >= 12)
                    isRenderTransform = false;
                else isRenderTransform = false; /*if (isAddCandle)
                {
                    if(bufer_x <= 8)//если кластерпрофайл в данный момент не видим, т.е. скрыт
                        isRenderTransform = false;
                }
                //else если свечи не выводим, то масштабируем обязательно, т.к. кластерпрофайл  по-любому видимый, т.е. не скрыт*/

            if (isRenderTransform)
                    RenderTransformClusterProfile(max_width_bar);
            }
        }

        private void AutoDetermineVolumeFilters(string N_cell_of_current_bar)
        {
            if (!isAutoDetermineVolumeFilters_for_Clusres)
                return;

            List<double> AllClusters = new List<double>();
            double total_volume_of_all_clusters = 0;
            int strt = 0, nd = Convert.ToInt32(N_cell_of_current_bar);

            if(interval >= 30)
            {
                strt = nd - 3;
                if (strt < 0) strt = 0;
            }
            else 
            {
                DateTime? lsearchingTime = chart[nd].time_of_bar.Value.AddMinutes(-60);

                for (int i = nd - 1; i >= 0; i--)//ищем номер ближайшего последнего бара после клиринга
                {
                    if (chart[i].time_of_bar < lsearchingTime)
                        break;

                    strt = i;
                }
            }

            for (int i = strt; i <= nd; i++)
            {
                for (int n = 0; n < chart[i].Bars.Count; n++)
                {
                    AllClusters.Add(chart[i].Bars[n].volume);
                    total_volume_of_all_clusters += chart[i].Bars[n].volume;
                }
            }

            AllClusters.Sort();
            bool isComplette_filter_1 = false, isComplette_filter_2 = false;
            double current_tot_vol = 0;

            for(int i =  AllClusters.Count - 1; i >= 0; i--)
            {
                current_tot_vol += AllClusters[i];

                if(!isComplette_filter_1 && current_tot_vol/total_volume_of_all_clusters >= 0.1)
                {
                    isComplette_filter_1 = true;
                    auto_volume_filter_for_cluster_1 = AllClusters[i];
                }

                if (!isComplette_filter_2 && current_tot_vol / total_volume_of_all_clusters >= 0.2)
                {
                    isComplette_filter_2 = true;
                    auto_volume_filter_for_cluster_2 = AllClusters[i];
                }

                if(current_tot_vol / total_volume_of_all_clusters >= 0.3)
                {
                    auto_volume_filter_for_cluster_3 = AllClusters[i];
                    break;
                }
            }
        }
        
        private void Save_fltrs()
        {
            string file_p = Vars.core_path + "\\tickdata\\" + instrument;
            if (Directory.Exists(file_p) == false)//проверяем существует ли такая папка - куда я хочу записывать полученные от сервера данные.
                Directory.CreateDirectory(file_p);//если нет, то создаем её
            file_p += "\\filters.txt";

            lock (Vars.object_for_lock)
            {
                using (StreamWriter wrtr = File.CreateText(file_p))
                {
                    for (int i = 0; i < fltrss.Length; i++)
                        wrtr.WriteLine(fltrss[i]);

                    wrtr.WriteLine(hsthrm_fltr_1);
                    wrtr.WriteLine(hsthrm_fltr_2);
                    wrtr.WriteLine(hsthrm_fltr_3);
                    wrtr.WriteLine(isAutoDetermineVolumeFilters_for_Clusres);
                    wrtr.WriteLine(max_volume_in_bar);
                    wrtr.WriteLine(vertical_histogram_type);

                    wrtr.Close();
                }
            }
        }

        private void Read_filters_file()
        {
            string file_path = Vars.core_path + "\\tickdata\\" + instrument + "\\filters.txt";
            try
            {
                if (File.Exists(file_path))
                { 
                    string[] filtra;
                    lock (Vars.object_for_lock)
                        filtra = File.ReadAllLines(file_path);

                    for (int i = 0; i < fltrss.Length; i++)
                        fltrss[i] = Convert.ToInt32(filtra[i]);

                    hsthrm_fltr_1 = Convert.ToInt32(filtra[42]);
                    hsthrm_fltr_2 = Convert.ToInt32(filtra[43]);
                    hsthrm_fltr_3 = Convert.ToInt32(filtra[44]);
                    isAutoDetermineVolumeFilters_for_Clusres = Convert.ToBoolean(filtra[45]);
                    max_volume_in_bar = Convert.ToBoolean(filtra[46]);
                    vertical_histogram_type = (VertcalHistogramType)Enum.Parse(typeof(VertcalHistogramType), filtra[47]);                    
                }
                else
                {
                    isAutoDetermineVolumeFilters_for_Clusres = false;
                    max_volume_in_bar = false;
                    vertical_histogram_type = VertcalHistogramType.CumulativeDelta;
                }
            }
            catch
            {
                hsthrm_fltr_1 = 0;
                hsthrm_fltr_2 = 0;
                hsthrm_fltr_3 = 0;
                for (int i = 0; i < fltrss.Length; i++)
                    fltrss[i] = 0;

                isAutoDetermineVolumeFilters_for_Clusres = false;
                max_volume_in_bar = false;
                vertical_histogram_type = VertcalHistogramType.CumulativeDelta;
            }
        }

        private void set_volume_filters()
        {
            if (interval == 1)
            {
                volume_filter_for_cluster_1 = fltrss[0];
                volume_filter_for_cluster_2 = fltrss[1];
                volume_filter_for_cluster_3 = fltrss[2];
                Vert_hsthrm_fltr_1 = fltrss[3];
                Vert_hsthrm_fltr_2 = fltrss[4];
                Vert_hsthrm_fltr_3 = fltrss[5];
            }
            else if (interval == 5)
            {
                volume_filter_for_cluster_1 = fltrss[6];
                volume_filter_for_cluster_2 = fltrss[7];
                volume_filter_for_cluster_3 = fltrss[8];
                Vert_hsthrm_fltr_1 = fltrss[9];
                Vert_hsthrm_fltr_2 = fltrss[10];
                Vert_hsthrm_fltr_3 = fltrss[11];
            }
            else if (interval == 240)//10)
            {
                volume_filter_for_cluster_1 = fltrss[12];
                volume_filter_for_cluster_2 = fltrss[13];
                volume_filter_for_cluster_3 = fltrss[14];
                Vert_hsthrm_fltr_1 = fltrss[15];
                Vert_hsthrm_fltr_2 = fltrss[16];
                Vert_hsthrm_fltr_3 = fltrss[17];
            }
            else if (interval == 15)
            {
                volume_filter_for_cluster_1 = fltrss[18];
                volume_filter_for_cluster_2 = fltrss[19];
                volume_filter_for_cluster_3 = fltrss[20];
                Vert_hsthrm_fltr_1 = fltrss[21];
                Vert_hsthrm_fltr_2 = fltrss[22];
                Vert_hsthrm_fltr_3 = fltrss[23];
            }
            else if (interval == 30)
            {
                volume_filter_for_cluster_1 = fltrss[24];
                volume_filter_for_cluster_2 = fltrss[25];
                volume_filter_for_cluster_3 = fltrss[26];
                Vert_hsthrm_fltr_1 = fltrss[27];
                Vert_hsthrm_fltr_2 = fltrss[28];
                Vert_hsthrm_fltr_3 = fltrss[29];
            }
            else if (interval == 60)
            {
                volume_filter_for_cluster_1 = fltrss[30];
                volume_filter_for_cluster_2 = fltrss[31];
                volume_filter_for_cluster_3 = fltrss[32];
                Vert_hsthrm_fltr_1 = fltrss[33];
                Vert_hsthrm_fltr_2 = fltrss[34];
                Vert_hsthrm_fltr_3 = fltrss[35];
            }
            else if (interval == 1440)
            {
                volume_filter_for_cluster_1 = fltrss[36];
                volume_filter_for_cluster_2 = fltrss[37];
                volume_filter_for_cluster_3 = fltrss[38];
                Vert_hsthrm_fltr_1 = fltrss[39];
                Vert_hsthrm_fltr_2 = fltrss[40];
                Vert_hsthrm_fltr_3 = fltrss[41];
            }
        }

        private void RenderTransformClusterProfile(double mwb)
        {
            double bar_width = bufer_x - 1;
            //if (bufer_x == 1) bar_width = 1;

            for (int i = N_fb; i <= N_lb; i++)
                if (chart[i].canv_filters != null && chart[i].canv_filters.Count > 0)
                    RenderTransformCurrentBarClusterProfile(chart[i], bar_width, mwb);
        }

        private void RenderTransformCurrentBarClusterProfile(Chart current_bar, double bar_width, double mwb)
        {
            current_bar.filters.RenderTransform = new ScaleTransform(bar_width / mwb, chart_mas_y / 12);
        }

        #endregion

        #region cross_line

        bool cross_move = false;

        private void MousedownBorderChart(object sender, MouseButtonEventArgs e)//нажали любую кнопку мыши на бордере-графике
        {
            if (canvas_prices.Children.Count == 0 || button_press) return;

            if (cross_move)
            {
                DeleteCross();

                if (Mouse.RightButton == MouseButtonState.Pressed)//если нажата правая кнопка мыши на бордере графикке то
                    isLineDeleted = true;//то идентифицируем что это для удаления активных линий, чтобы не выводить контекстное меню

                if (Mouse.LeftButton == MouseButtonState.Pressed)
                    Activate_Move();
            }
            else if (Mouse.LeftButton == MouseButtonState.Pressed)
            {
                    Activate_Move();
            }
            else if (Mouse.MiddleButton == MouseButtonState.Pressed)//если нажали колесо мыши - создаём крестик
            {
                cursor_pos = Mouse.GetPosition(this);

                lock (chart)
                {
                    double raznica = (cursor_pos.Y - border_chart.Margin.Top - 1 - canvas_chart.Margin.Top) / chart_mas_y;
                    int N_activ_price = Convert.ToInt32(Math.Floor(raznica));//определ номер ячейки цены, на котор навели мышу 

                    if (N_activ_price < 0)//если выше мах или ниже мин цены на вертикальной шкале
                        N_activ_price = 0;
                    else if (N_activ_price > price_canvases.Count - 1)
                        N_activ_price = price_canvases.Count - 1;

                    double mouse_posit = (cursor_pos.X - border_chart.Margin.Left - 1 - canvas_chart.Margin.Left - Canvas.GetLeft(chart[N_fb].chart_b)) / bufer_x;
                    int N_activ_time = N_fb + Convert.ToInt32(Math.Floor(mouse_posit));//определ номер временной ячейки, на котор навели мышу 

                    if (N_activ_time < 0)
                        N_activ_time = 0;
                    else if (N_activ_time > chart.Count - 1)
                        N_activ_time = chart.Count - 1;

                    CreateHorisontalLine(N_activ_price, true);
                    CreateVerticalLine(N_activ_time, true);
                }

                cross_move = true;
            }
        }

        private void MouseleaveBorderChart(object sender, MouseEventArgs e)
        {
            if (cross_move)
                DeleteCross();
        }

        private void DeleteCross()
        {
            cross_move = false;

            lock (chart)
            {
                if (GorizontalActivLine != null)
                    DeleteHorizontalLine();

                if (VerticalActivLine != null)
                    DeleteVerticalLine();
            }
        }

        private void CreateOpenCloseLabel()
        {
            OpenCloseFrame = new Border();
            OpenCloseFrame.Background = Brushes.Black;
            OpenCloseFrame.BorderThickness = new Thickness(1, 1, 1, 1);
            OpenCloseFrame.BorderBrush = (Brush)bc.ConvertFrom("#FF00FF40");
            OpenCloseFrame.Height = 47;
            Canvas inOpenCloseFrame = new Canvas();
            inOpenCloseFrame.Margin = new Thickness(-4, -7, 0, 0);
            OpenCloseFrame.Child = inOpenCloseFrame;
        }

        private void SetOpenCloseLabel()
        {
            if(chart[VerticalActivLine.N_cell].bar_highs == 0)
            {
                OpenCloseFrame.Visibility = Visibility.Hidden;
                return;
            }

            Canvas inOpenCloseFrame = (Canvas)OpenCloseFrame.Child;
            inOpenCloseFrame.Children.Clear();

            double high = chart[VerticalActivLine.N_cell].bar_highs_double, opn = chart[VerticalActivLine.N_cell].open_price, cls = chart[VerticalActivLine.N_cell].close_price, lw = chart[VerticalActivLine.N_cell].bar_lows_double;
            string cntnt_h = "H  " + ((decimal)high).ToString();
            string cntnt_o = "O  " + ((decimal)opn).ToString();
            string cntnt_c = "C  " + ((decimal)cls).ToString();
            string cntnt_l = "L   " + ((decimal)lw).ToString();
            int lnght = cntnt_h.Length;
            if (cntnt_o.Length > lnght) lnght = cntnt_o.Length;
            if (cntnt_c.Length > lnght) lnght = cntnt_c.Length;
            if (cntnt_l.Length - 1 > lnght) lnght = cntnt_l.Length - 1;

            if(high != Vars.MathRound(high) || opn != Vars.MathRound(opn) || cls != Vars.MathRound(cls) || lw != Vars.MathRound(lw))                
                OpenCloseFrame.Width = 3 + (lnght - 1) * 6;
            else OpenCloseFrame.Width = 7 + (lnght - 1) * 6;

            Label cnt_h = new Label();
            cnt_h.Content = cntnt_h;
            cnt_h.Foreground = (Brush)bc.ConvertFrom("#FF00FF40");
            cnt_h.FontSize = 11;
            inOpenCloseFrame.Children.Add(cnt_h);
            Label cnt_o = new Label();
            cnt_o.Content = cntnt_o;
            cnt_o.Foreground = (Brush)bc.ConvertFrom("#FF00FF40");
            cnt_o.FontSize = 11;
            Canvas.SetTop(cnt_o, 11);
            inOpenCloseFrame.Children.Add(cnt_o);
            Label cnt_c = new Label();
            cnt_c.Content = cntnt_c;
            cnt_c.Foreground = (Brush)bc.ConvertFrom("#FF00FF40");
            cnt_c.FontSize = 11;
            Canvas.SetTop(cnt_c, 22);
            Canvas.SetLeft(cnt_c, 1);
            inOpenCloseFrame.Children.Add(cnt_c);
            Label cnt_l = new Label();
            cnt_l.Content = cntnt_l;
            cnt_l.Foreground = (Brush)bc.ConvertFrom("#FF00FF40");
            cnt_l.FontSize = 11;
            Canvas.SetTop(cnt_l, 33);
            inOpenCloseFrame.Children.Add(cnt_l);

            Canvas.SetLeft(OpenCloseFrame, Math.Round(Canvas.GetLeft(chart[VerticalActivLine.N_cell].chart_b) + canvas_chart.Margin.Left - OpenCloseFrame.Width / 2 + (bufer_x - 1) / 2, MidpointRounding.AwayFromZero));
 
            OpenCloseFrame.Visibility = Visibility.Visible;
        }

        #endregion
        
    }
}