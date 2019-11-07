using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
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
    public partial class WPF_文件設定 : Window
    {

        public WPF_文件設定()
        {
            InitializeComponent();
            Owner = Local.MainForm;  //視窗綁在主視窗前

            TB_Email.Text = Local.AniDir;
            TB_ProxyIP.Text = Local.ProxyIP;
            TB_ProxyPort.Text = (Local.ProxyPort == 0 )?  "": Local.ProxyPort.ToString();
            TB_ProxyUser.Text  = Local.ProxyUser ;
            TB_ProxyPass.Password = Local.ProxyPass;

            CB_品質.SelectedItem = Local.Quality;
            Border_背景.Background = new SolidColorBrush(Local.GetThemeColor("ImmersiveStartSelectionBackground"));
        }




        private void Button_Click(object sender, RoutedEventArgs e)
        {
            switch((String)((Button)sender).Name)
            {
                case "Btn_測試":
                    {
                        
                        Local.ProxyIP = TB_ProxyIP.Text;
                        Int32.TryParse(TB_ProxyPort.Text ,out Local.ProxyPort );
                        Local.ProxyUser = TB_ProxyUser.Text;
                        Local.ProxyPass = TB_ProxyPass.Password;

                        if(TB_ProxyIP.Text == "" || Local.ProxyPort == 0)
                        {
                            WPFMessageBox.Show("ProxyIP或Port輸入錯誤");
                            return;
                        }
                        Module.BahaRequest.ProxyTest(Local.ProxyIP, Local.ProxyPort, Local.ProxyUser, Local.ProxyPass);
                        break;
                    }

                case "Btn_關閉":
                    {
                        this.Close();
                        break;
                    }

                case "Btn_新增":
                    {
                        try
                        {
                            Local.AniDir = TB_Email.Text;

                            Local.ProxyIP = TB_ProxyIP.Text;
                            Int32.TryParse(TB_ProxyPort.Text, out Local.ProxyPort);
                            Local.ProxyUser = TB_ProxyUser.Text;
                            Local.ProxyPass = TB_ProxyPass.Password;

                            if (CB_品質.SelectedItem != null)
                                Local.Quality = (String)CB_品質.SelectedItem;
                            else
                                Local.Quality = "720";

                            XDocument xDoc;
                            XElement xRoot;
                            if (File.Exists(Local.SetupFile))
                            {
                                xDoc = XDocument.Load(Local.SetupFile);
                            }
                            else
                            {
                                xDoc = new XDocument(new XComment("設定檔"), new XElement("AniGamer"));
                            }

                            xRoot = xDoc.Root;

                            if (xRoot.Element("Dir") != null)
                                xRoot.Elements("Dir").Remove();

                            XElement xDir = new XElement("Dir");
                            xDir.Add(new XElement("Ani", Local.AniDir));
                            xDir.Add(new XElement("Q", Local.Quality));
                            xRoot.Add(xDir);

                            if (xRoot.Element("Proxy") != null)
                                xRoot.Elements("Proxy").Remove();

                            if (Local.ProxyIP != "")
                            {
                                XElement xProxy = new XElement("Proxy");
                                xProxy.Add(new XElement("IP", Local.ProxyIP));
                                xProxy.Add(new XElement("Port", Local.ProxyPort));
                                xRoot.Add(xProxy);
                            }

                            if (Local.ProxyUser != "")
                            {
                                XElement xUser = new XElement("ProxyUser");
                                xUser.Add(new XElement("User", Local.ProxyUser));
                                xUser.Add(new XElement("Pass", Local.ProxyPass));
                                xRoot.Add(xUser);
                            }

                            xDoc.Save(Local.SetupFile);
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

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog path = new System.Windows.Forms.FolderBrowserDialog();
            path.ShowDialog();
            this.TB_Email.Text = path.SelectedPath;
        }

        private void Expander_Expanded(object sender, RoutedEventArgs e)
        {
            Grid_Main.Height = 462;
        }

        private void Expander_Collapsed(object sender, RoutedEventArgs e)
        {
            Grid_Main.Height = 342;
        }
    }
}
