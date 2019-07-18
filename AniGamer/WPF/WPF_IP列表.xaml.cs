using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using Module;

namespace WPF
{
    /// <summary>
    /// LoginFrom.xaml 的互動邏輯
    /// </summary>
    public partial class WPF_IP列表 : Window
    {

        public WPF_IP列表()
        {
            InitializeComponent();
            Owner = Local.MainForm;  //視窗綁在主視窗前

            Border_背景.Background = new SolidColorBrush(Local.GetThemeColor("ImmersiveStartSelectionBackground"));


        }


        private void Button_Click(object sender, RoutedEventArgs e)
        {
            switch((String)((Button)sender).Name)
            {
                case "Btn_關閉":
                    {
                        this.Close();
                        break;
                    }

                case "Btn_新增":
                    {
                        try
                        {
                            
                        }
                        catch { }
                        this.Close();
                        break;
                    }
              
            }
        }

        

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            { this.DragMove(); }
            catch
            { }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            HttpWebRequest request = HttpWebRequest.Create(@"http://proxy.moo.jp/ja/tw.html") as HttpWebRequest;
            request.Method = "GET";
            request.ContentType = "application/x-www-form-urlencoded";
            request.Timeout = 30000;
            request.UserAgent = @"Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/72.0.3626.109 Safari/537.36";
            request.Referer = @"http://proxy.moo.jp/ja/country.html";
            request.Headers.Add("origin", @"http://proxy.moo.jp/ja/");
            request.CookieContainer = new CookieContainer();

            string result = "";
            using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
            {
                using (StreamReader sr = new StreamReader(response.GetResponseStream()))
                {
                    result = sr.ReadToEnd();
                    Regex rx = new Regex("<title>(.*)</title>");
                    MatchCollection m = rx.Matches("");


                    LB_IPList.Items.Add(1);
                }
            }
        


           

        }
    }
}
