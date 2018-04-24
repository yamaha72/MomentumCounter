using System;
using System.Windows;
using System.Windows.Controls;

namespace Momentum
{
    public partial class PopupFilters : Window
    {
        ChartWindow Main_win = null;
        bool swtch;

        public PopupFilters(ChartWindow _main_win)
        {
            InitializeComponent();
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Main_win = _main_win;

                textbox_1.Text = Main_win.volume_filter_for_cluster_1.ToString();
                textbox_2.Text = Main_win.volume_filter_for_cluster_2.ToString();
                textbox_3.Text = Main_win.volume_filter_for_cluster_3.ToString();

                if (Main_win.isAutoDetermineVolumeFilters_for_Clusres)
                    AutoFilter.IsChecked = true;
                else SetFilter.IsChecked = true;
            
            if (Main_win.max_volume_in_bar)
            {
                check_box_1.IsChecked = true;
                swtch = true;
            }
            else
            {
                check_box_1.IsChecked = false;
                swtch = false;
            }
        }

        private void Textchange_1(object sender, TextChangedEventArgs e)
        {
            string filter1 = textbox_1.Text;
            int frt = textbox_1.SelectionStart;
            string filter2 = Didgit_Check(filter1);
            if (filter2 != filter1)
            {
                textbox_1.Text = filter2;
                textbox_1.SelectionStart = frt - (filter1.Length - filter2.Length);
            }
        }

        private void Textchange_2(object sender, TextChangedEventArgs e)
        {
            string filter1 = textbox_2.Text;
            int frt = textbox_2.SelectionStart;
            string filter2 = Didgit_Check(filter1);
            if (filter2 != filter1)
            {
                textbox_2.Text = filter2;
                textbox_2.SelectionStart = frt - (filter1.Length - filter2.Length);
            }
        }

        private void Textchange_3(object sender, TextChangedEventArgs e)
        {
            string filter1 = textbox_3.Text;
            int frt = textbox_3.SelectionStart;
            string filter2 = Didgit_Check(filter1);
            if (filter2 != filter1)
            {
                textbox_3.Text = filter2;
                textbox_3.SelectionStart = frt - (filter1.Length - filter2.Length);
            }
        }

        /*private void Textchange_4(object sender, TextChangedEventArgs e)//max_tick_filter
        {
            string filter1 = textbox_4.Text;
            int frt = textbox_4.SelectionStart;
            string filter2 = Didgit_Check(filter1);
            if (filter2 != filter1)
            {
                textbox_4.Text = filter2;
                textbox_4.SelectionStart = frt - (filter1.Length - filter2.Length);
            }
        }*/

        private string Didgit_Check(string filter)//провер фильтр на цыфры-символы
        {
            if (filter.Length > 8) filter = filter.Substring(0, 8);
            for (int x = 0; x < filter.Length; x++)
            {
                char chr = (char)filter[x];
                if (Char.IsDigit(chr) != true)
                {
                    filter = filter.Substring(0, x) + filter.Substring(x + 1, filter.Length - x - 1);
                    x--;
                }
            }

            return filter;
        }

        public void Click_17(object sender, RoutedEventArgs e)
        {
            int v_fltr_1, v_fltr_2, v_fltr_3;

            if (textbox_1.Text == "") v_fltr_1 = 0;
            else v_fltr_1 = Convert.ToInt32(textbox_1.Text);
            if (textbox_2.Text == "") v_fltr_2 = 0;
            else v_fltr_2 = Convert.ToInt32(textbox_2.Text);
            if (textbox_3.Text == "") v_fltr_3 = 0;
            else v_fltr_3 = Convert.ToInt32(textbox_3.Text);

            int vol_buf = 0;

            if (v_fltr_1 > 0)
            {
                if (v_fltr_1 < v_fltr_2)
                {
                    vol_buf = v_fltr_1;

                    if (v_fltr_1 < v_fltr_3)
                    {
                        if (v_fltr_3 > v_fltr_2)
                        {
                            v_fltr_1 = v_fltr_3;
                            v_fltr_3 = vol_buf;
                        }
                        else
                        {
                            v_fltr_1 = v_fltr_2;

                            if (v_fltr_2 == v_fltr_3)
                            {
                                v_fltr_3 = 0;
                                v_fltr_2 = vol_buf;
                            }
                            else
                            {
                                v_fltr_2 = v_fltr_3;
                                v_fltr_3 = vol_buf;
                            }
                        }
                    }
                    else
                    {
                        v_fltr_1 = v_fltr_2;
                        v_fltr_2 = vol_buf;

                        if (v_fltr_2 == v_fltr_3) v_fltr_3 = 0;
                    }
                }
                else
                {
                    if (v_fltr_1 < v_fltr_3)
                    {
                        if (v_fltr_1 == v_fltr_2)
                        {
                            v_fltr_1 = v_fltr_3;
                            v_fltr_3 = 0;
                        }
                        else
                        {
                            vol_buf = v_fltr_1;
                            v_fltr_1 = v_fltr_3;
                            if (v_fltr_2 > 0)
                            {
                                v_fltr_3 = v_fltr_2;
                                v_fltr_2 = vol_buf;
                            }
                            else v_fltr_3 = vol_buf;
                        }
                    }
                    else
                    {
                        if (v_fltr_2 > 0)
                        {
                            if (v_fltr_3 > 0)
                            {
                                if (v_fltr_3 > v_fltr_2)
                                {
                                    if (v_fltr_3 == v_fltr_1) v_fltr_3 = 0;
                                    else
                                    {
                                        vol_buf = v_fltr_2;
                                        v_fltr_2 = v_fltr_3;
                                        v_fltr_3 = vol_buf;
                                    }
                                }
                                else
                                {
                                    if (v_fltr_3 == v_fltr_1 || v_fltr_3 == v_fltr_2) v_fltr_3 = 0;
                                    if (v_fltr_2 == v_fltr_1)
                                    {
                                        v_fltr_2 = v_fltr_3;
                                        v_fltr_3 = 0;
                                    }
                                }
                            }
                            else if (v_fltr_2 == v_fltr_1) v_fltr_2 = 0;
                        }
                        else if (v_fltr_3 == v_fltr_1) v_fltr_3 = 0;
                    }
                }
            }
            else if (v_fltr_2 > 0 && v_fltr_3 > 0)
            {
                if (v_fltr_3 > v_fltr_2)
                {
                    vol_buf = v_fltr_2;
                    v_fltr_2 = v_fltr_3;
                    v_fltr_3 = vol_buf;
                }
                else if (v_fltr_3 == v_fltr_2) v_fltr_3 = 0;
            }

                Main_win.volume_filter_for_cluster_1 = v_fltr_1;
                Main_win.volume_filter_for_cluster_2 = v_fltr_2;
                Main_win.volume_filter_for_cluster_3 = v_fltr_3;

                Main_win.isAutoDetermineVolumeFilters_for_Clusres = AutoFilter.IsChecked.Value;
           

            Main_win.max_volume_in_bar = swtch;

            Close();
        }

        private void checkd_1(object sender, RoutedEventArgs e)
        {
            if (check_box_1.IsChecked == true)
            {
                swtch = true;
            }
            else
            {
                swtch = false;
            }
        }

        private void AutoChecked(object sender, RoutedEventArgs e)
        {
            textbox_1.IsEnabled = false;
            textbox_2.IsEnabled = false;
            textbox_3.IsEnabled = false;
        }

        private void SetChecked(object sender, RoutedEventArgs e)
        {
            textbox_1.IsEnabled = true;
            textbox_2.IsEnabled = true;
            textbox_3.IsEnabled = true;
        }
    }
}
