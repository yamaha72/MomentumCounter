using System;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Momentum
{
    public partial class PopupHistogram : Window
    {
        ChartWindow Main_win = null;
        double intrv;
        VertcalHistogramType vertic_histogr_type;
        bool swtch, swtch_2;
        BrushConverter bc = new BrushConverter();

        public PopupHistogram(ChartWindow _main_win, double interval)
        {
            InitializeComponent();
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Main_win = _main_win;
            intrv = interval;
            vertic_histogr_type = Main_win.vertical_histogram_type;

            swtch = Main_win.isGist;
            swtch_2 = Main_win.vertical_gist;
            textbox_1.Text = Main_win.hsthrm_fltr_1.ToString();
            textbox_2.Text = Main_win.hsthrm_fltr_2.ToString();
            textbox_3.Text = Main_win.hsthrm_fltr_3.ToString();
            textbox_4.Text = Main_win.Vert_hsthrm_fltr_1.ToString();
            textbox_5.Text = Main_win.Vert_hsthrm_fltr_2.ToString();
            textbox_6.Text = Main_win.Vert_hsthrm_fltr_3.ToString();

            if (Main_win.isGist)
                check_box_1.IsChecked = true;
            else
            {
                swtch = false;
                textbox_1.IsEnabled = false;
                textbox_2.IsEnabled = false;
                textbox_3.IsEnabled = false;
            }

            if (Main_win.vertical_gist)
                check_box_2.IsChecked = true;
            else
            {
                textbox_4.IsEnabled = false;
                textbox_5.IsEnabled = false;
                textbox_6.IsEnabled = false;

                CumulativeDeltaCheckBox.IsEnabled = false;
                VolumeCheckBox.IsEnabled = false;
            }

           
                if (vertic_histogr_type == VertcalHistogramType.Volume)
                    VolumeCheckBoxClick(null, null);
                else if (vertic_histogr_type == VertcalHistogramType.CumulativeDelta)
                {
                    CumulativeDeltaCheckBox.IsChecked = true;
                    textbox_4.IsEnabled = false;
                    textbox_5.IsEnabled = false;
                    textbox_6.IsEnabled = false;
                }


        }

        #region TextBoxChanged
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
        
        private void Textchange_4(object sender, TextChangedEventArgs e)
        {
            string filter1 = textbox_4.Text;
            int frt = textbox_4.SelectionStart;
            string filter2 = Didgit_Check(filter1);
            if (filter2 != filter1)
            {
                textbox_4.Text = filter2;
                textbox_4.SelectionStart = frt - (filter1.Length - filter2.Length);
            }
        }

        private void Textchange_5(object sender, TextChangedEventArgs e)
        {
            string filter1 = textbox_5.Text;
            int frt = textbox_5.SelectionStart;
            string filter2 = Didgit_Check(filter1);
            if (filter2 != filter1)
            {
                textbox_5.Text = filter2;
                textbox_5.SelectionStart = frt - (filter1.Length - filter2.Length);
            }
        }

        private void Textchange_6(object sender, TextChangedEventArgs e)
        {
            string filter1 = textbox_6.Text;
            int frt = textbox_6.SelectionStart;
            string filter2 = Didgit_Check(filter1);
            if (filter2 != filter1)
            {
                textbox_6.Text = filter2;
                textbox_6.SelectionStart = frt - (filter1.Length - filter2.Length);
            }
        }
        
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

        #endregion

        private void Click_1(object sender, RoutedEventArgs e)
        {
            int v_fltr_1, v_fltr_2, v_fltr_3, gv_fltr_1, gv_fltr_2, gv_fltr_3;

            #region Sort

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

            if (textbox_4.Text == "") gv_fltr_1 = 0;
            else gv_fltr_1 = Convert.ToInt32(textbox_4.Text);
            if (textbox_5.Text == "") gv_fltr_2 = 0;
            else gv_fltr_2 = Convert.ToInt32(textbox_5.Text);
            if (textbox_6.Text == "") gv_fltr_3 = 0;
            else gv_fltr_3 = Convert.ToInt32(textbox_6.Text);

            int gvol_buf = 0;

            if (gv_fltr_1 > 0)
            {
                if (gv_fltr_1 < gv_fltr_2)
                {
                    gvol_buf = gv_fltr_1;

                    if (gv_fltr_1 < gv_fltr_3)
                    {
                        if (gv_fltr_3 > gv_fltr_2)
                        {
                            gv_fltr_1 = gv_fltr_3;
                            gv_fltr_3 = gvol_buf;
                        }
                        else
                        {
                            gv_fltr_1 = gv_fltr_2;

                            if (gv_fltr_2 == gv_fltr_3)
                            {
                                gv_fltr_3 = 0;
                                gv_fltr_2 = gvol_buf;
                            }
                            else
                            {
                                gv_fltr_2 = gv_fltr_3;
                                gv_fltr_3 = gvol_buf;
                            }
                        }
                    }
                    else
                    {
                        gv_fltr_1 = gv_fltr_2;
                        gv_fltr_2 = gvol_buf;

                        if (gv_fltr_2 == gv_fltr_3) gv_fltr_3 = 0;
                    }
                }
                else
                {
                    if (gv_fltr_1 < gv_fltr_3)
                    {
                        if (gv_fltr_1 == gv_fltr_2)
                        {
                            gv_fltr_1 = gv_fltr_3;
                            gv_fltr_3 = 0;
                        }
                        else
                        {
                            gvol_buf = gv_fltr_1;
                            gv_fltr_1 = gv_fltr_3;
                            if (gv_fltr_2 > 0)
                            {
                                gv_fltr_3 = gv_fltr_2;
                                gv_fltr_2 = gvol_buf;
                            }
                            else gv_fltr_3 = gvol_buf;
                        }
                    }
                    else
                    {
                        if (gv_fltr_2 > 0)
                        {
                            if (gv_fltr_3 > 0)
                            {
                                if (gv_fltr_3 > gv_fltr_2)
                                {
                                    if (gv_fltr_3 == gv_fltr_1) gv_fltr_3 = 0;
                                    else
                                    {
                                        gvol_buf = gv_fltr_2;
                                        gv_fltr_2 = gv_fltr_3;
                                        gv_fltr_3 = gvol_buf;
                                    }
                                }
                                else
                                {
                                    if (gv_fltr_3 == gv_fltr_1 || gv_fltr_3 == gv_fltr_2)gv_fltr_3 = 0;
                                    if (gv_fltr_2 == gv_fltr_1)
                                    {
                                        gv_fltr_2 = gv_fltr_3;
                                        gv_fltr_3 = 0;
                                    }
                                }
                            }
                            else if (gv_fltr_2 == gv_fltr_1) gv_fltr_2 = 0;
                        }
                        else if (gv_fltr_3 == gv_fltr_1) gv_fltr_3 = 0;
                    }
                }
            }
            else if (gv_fltr_2 > 0 && gv_fltr_3 > 0)
            {
                if (gv_fltr_3 > gv_fltr_2)
                {
                    gvol_buf = gv_fltr_2;
                    gv_fltr_2 = gv_fltr_3;
                    gv_fltr_3 = gvol_buf;
                }
                else if (gv_fltr_3 ==gv_fltr_2) gv_fltr_3 = 0;
            }

            #endregion

            Main_win.hsthrm_fltr_1 = v_fltr_1;
            Main_win.hsthrm_fltr_2 = v_fltr_2;
            Main_win.hsthrm_fltr_3 = v_fltr_3;

            Main_win.Vert_hsthrm_fltr_1 = gv_fltr_1;
            Main_win.Vert_hsthrm_fltr_2 = gv_fltr_2;
            Main_win.Vert_hsthrm_fltr_3 = gv_fltr_3;

            Main_win.isGist = swtch;
            
                Main_win.vertical_histogram_type = vertic_histogr_type;

            Main_win.vertical_gist = swtch_2;

            Close();
        }
        
        private void checkd_1(object sender, RoutedEventArgs e)
        {
            if (check_box_1.IsChecked == true)
            {
                swtch = true;
                textbox_1.IsEnabled = true;
                textbox_2.IsEnabled = true;
                textbox_3.IsEnabled = true;
            }
            else
            {
                swtch = false;
                textbox_1.IsEnabled = false;
                textbox_2.IsEnabled = false;
                textbox_3.IsEnabled = false;
            }
        }
        
        private void checkd_2(object sender, RoutedEventArgs e)
        {
            if (check_box_2.IsChecked == true)
            {
                swtch_2 = true;
                VolumeCheckBox.IsEnabled = true;
                if (VolumeCheckBox.IsChecked.Value)
                {
                    textbox_4.IsEnabled = true;
                    textbox_5.IsEnabled = true;
                    textbox_6.IsEnabled = true;
                }

                    CumulativeDeltaCheckBox.IsEnabled = true;

            }
            else
            {
                swtch_2 = false;
                textbox_4.IsEnabled = false;
                textbox_5.IsEnabled = false;
                textbox_6.IsEnabled = false;
                CumulativeDeltaCheckBox.IsEnabled = false;
                VolumeCheckBox.IsEnabled = false;
            }
        }
        
        private void CumulativeDeltaCheckBoxClick(object sender, RoutedEventArgs e)
        {
            vertic_histogr_type = VertcalHistogramType.CumulativeDelta;
            CumulativeDeltaCheckBox.IsChecked = true;

            VolumeCheckBox.IsChecked = false;
            textbox_4.IsEnabled = false;
            textbox_5.IsEnabled = false;
            textbox_6.IsEnabled = false;

        }

        private void VolumeCheckBoxClick(object sender, RoutedEventArgs e)
        {
            vertic_histogr_type = VertcalHistogramType.Volume;
            VolumeCheckBox.IsChecked = true;

            textbox_4.IsEnabled = true;
            textbox_5.IsEnabled = true;
            textbox_6.IsEnabled = true;

            CumulativeDeltaCheckBox.IsChecked = false;
        }

    }
}
