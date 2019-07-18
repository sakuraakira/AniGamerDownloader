using Module;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;
using System.Xml.Linq;
using System.Threading;
using System.IO;
using System.Windows.Media;

namespace WPF
{
    /// <summary>
    /// MainForm.xaml 的互動邏輯
    /// </summary>
    public partial class WPF_MainForm : Window
    {
        List<Model.BahaModel> VideoList { set; get; }

        public WPF_MainForm()
        {
            InitializeComponent();
            Local.MainForm = this;

            VideoList = new List<Model.BahaModel>(); //下載清單的初始化

            if (File.Exists(Local.SetupFile))
            {
                try
                {   //如果有設定檔 載入設定值
                    XDocument xDoc = XDocument.Load(Local.SetupFile);
                    XElement xRoot = xDoc.Root;

                    XElement xWin = xRoot.Element("Windows");
                    if (xWin.Attribute("H") != null) this.Height = double.Parse(xWin.Attribute("H").Value);
                    if (xWin.Attribute("W") != null) this.Width = double.Parse(xWin.Attribute("W").Value);
                    if (xWin.Attribute("T") != null) this.Top = double.Parse(xWin.Attribute("T").Value);
                    if (xWin.Attribute("L") != null) this.Left = double.Parse(xWin.Attribute("L").Value);

                    XElement xDir = xRoot.Element("Dir");
                    if (xDir.Element("Ani") != null)
                    {
                        if (Directory.Exists(xDir.Element("Ani").Value))
                            Local.AniDir = xDir.Element("Ani").Value;
                    }
                    if (xDir.Element("Q") != null) Local.Quality =  xDir.Element("Q").Value;
                }
                catch { }
            }

            if(!Directory.Exists(Local.AniDir))  //確認設置的資料夾是否存在
            {
                Directory.CreateDirectory(Local.AniDir);
            }

            if (!Directory.Exists(Local.TmpDir))
            {
                Directory.CreateDirectory(Local.TmpDir);
            }

            try
            {   //嘗試抓取系統主題色 (Win7以上才有效)
                Grid_下資訊框.Background = new SolidColorBrush(Local.GetThemeColor("ImmersiveStartBackground"));
                Grid_標題框.Background = new SolidColorBrush(Local.GetThemeColor("ImmersiveStartSelectionBackground"));
                Color BG = Local.GetThemeColor("ImmersiveStartBackground");
                BG.A = 0X25;
                Grid_拖曳框.Background = new SolidColorBrush(BG);
            }
            catch { }

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            switch((String)((Button)sender).Name)
            {

                case "Btn_縮小":
                    {
                        this.WindowState = WindowState.Minimized;
                        break;
                    }
                case "Btn_視窗":
                    {
                        Btn_放大.Visibility = Visibility.Visible;
                        Btn_視窗.Visibility = Visibility.Collapsed;
                        this.WindowState = WindowState.Normal;
                        break;
                    }
                case "Btn_放大":
                    {
                        Btn_放大.Visibility = Visibility.Collapsed;
                        Btn_視窗.Visibility = Visibility.Visible;
                        this.WindowState = WindowState.Maximized;
                        break;
                    }
                case "Btn_關閉":
                    {
                        if(VideoList.Where(I=>I.IsIng).Count() > 0)
                        {
                            if (WPFMessageBox.Show("下載工作未尚結束，確定要離開嗎?", WPFMessageBoxButton.YesNo) == WPFMessageBoxResult.No)
                                return;
                        }
                        Application.Current.Shutdown();

                        break;
                    }
                case "Btn_消字":
                    {
                        TB_搜尋.Text = "";
                        break;
                    }
                case "Btn_搜尋":
                    {
                        TB_搜尋_TextChanged(null, null);
                        break;
                    }

                case "Btn_設定":
                    {
                        Border_遮幕.Visibility = Visibility.Visible;
                        WPF_文件設定 WPF = new WPF_文件設定();
                        //WPF_IP列表 WPF = new WPF_IP列表();
                        WPF.ShowDialog();
                        Border_遮幕.Visibility = Visibility.Collapsed;
                        break;
                    }

                case "Btn_開啟":
                    {
                        System.Diagnostics.Process prc = new System.Diagnostics.Process();
                        prc.StartInfo.FileName = Local.AniDir;
                        prc.Start();
                        break;
                    }

                case "Btn_下載":
                    {
                        if (DataGrid_清單.SelectedItem == null) return;
                        Model.BahaModel baha = (Model.BahaModel)DataGrid_清單.SelectedItem;
                        if(!baha.IsOk)
                        {
                            WPFMessageBox.Show("檔案下載完成後才能執行!");
                            return;
                        }

                        Microsoft.Win32.SaveFileDialog save = new Microsoft.Win32.SaveFileDialog()
                        {
                            FileName = baha.Name,
                            Filter = "Media Files (*.mp4;*.MP4)|*mp4;*MP4",
                            DefaultExt = "mp4",
                            OverwritePrompt = true,
                            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                            AddExtension = true
                        };
                        if (true == save.ShowDialog())
                        {
                            try
                            {
                                FileStream file = new FileStream(Local.AniDir + "\\" + baha.Name + ".mp4", FileMode.Open);
                                using (var filestream = new FileStream(save.FileName, FileMode.Create))
                                {
                                    file.CopyTo(filestream);
                                    file.Close();
                                    File.Delete(Local.AniDir + "\\" + baha.Name + ".mp4");
                                }
                                WPFMessageBox.Show("已完成另存檔案。");
                            }
                            catch
                            {
                                WPFMessageBox.Show("原檔案遺失。");
                            }
                            
                        }
                        break;
                    }

                case "Btn_刪除":
                    {
                        if (DataGrid_清單.SelectedItem == null) return;
                        Model.BahaModel baha = (Model.BahaModel)DataGrid_清單.SelectedItem;

                        try
                        {
                            VideoList.Remove(baha);
                            DataGrid_清單.ItemsSource = null;
                            DataGrid_清單.ItemsSource = VideoList;

                            if(File.Exists(Local.AniDir + "\\" + baha.Name + ".mp4"))
                            {
                               if( WPFMessageBox.Show("是否連同資料夾的檔案也刪除?",WPFMessageBoxButton.YesNo) == WPFMessageBoxResult.Yes)
                                {
                                    File.Delete(Local.AniDir + "\\" + baha.Name + ".mp4");
                                }

                            }

                        }
                        catch
                        {
                            WPFMessageBox.Show("原檔案遺失。");
                        }

                        break;
                    }
            }
        }

        private void Grid_標題框_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && e.ClickCount == 2)
            {
                this.Top = 0;
                this.Left = 300;
            }
            try { this.DragMove(); } catch { }
        } 
  

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if( !File.Exists(AppDomain.CurrentDomain.BaseDirectory + "ffmpeg.exe") )
            {
                WPFMessageBox.Show("請先下載轉檔工具ffmpeg.exe 放到同一目錄下。");
                Application.Current.Shutdown();
            }
        }

        private void TB_搜尋_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (TB_搜尋.Text.Length == 5 || TB_搜尋.Text.Length > 40)
            {
                int Sn;
                if (!int.TryParse(TB_搜尋.Text, out Sn))
                {
                    if (TB_搜尋.Text.Contains("sn="))
                    {
                        var s = TB_搜尋.Text.Substring(TB_搜尋.Text.IndexOf("sn=") + 3);
                        if (!int.TryParse(s, out Sn))
                        {
                            WPFMessageBox.Show("不是正確的連結格式。");
                            return;
                        }
                    }
                    else
                    {
                        WPFMessageBox.Show("不是正確的連結格式。");
                        return;
                    }
                }

                Model.BahaModel Baha = new Model.BahaModel();
                //System.Net.ServicePointManager.Expect100Continue = false;
                // WebRequest.Proxy = new System.Net.WebProxy("59.127.168.43", 3128);
                //WebRequest.Proxy.UseDefaultCredentials = true;
                //WebRequest.Proxy.Credentials =  new System.Net.NetworkCredential("vpn", "");

                Baha.SN = Sn.ToString();
                Baha.Name = WebRequest.GetTitle(Baha.SN).Split('-')[0];
                Baha.Status = "排隊中...";
                if (VideoList.Count > 0)
                    Baha.No = VideoList.Max(I => I.No) + 1;
                else
                    Baha.No = 1;
                Baha.Quality = Local.Quality;

                VideoList.Add(Baha);
                DataGrid_清單.ItemsSource = null;
                DataGrid_清單.ItemsSource = VideoList;

                處理影片();
                TB_搜尋.Text = "";
            }
        }

        void 處理影片()
        {
            if (VideoList.Where(I => I.IsIng).Count() > 0) return;

            var q = VideoList.Where(I => !I.IsOk && !I.IsStop);
            if (q.Count() > 0)
            {
                Model.BahaModel Baha = q.OrderBy(I => I.No).First();
                Thread TH = new Thread(new ParameterizedThreadStart(Main));
                TH.Start(Baha);
            }
        }



        void Main(object value)
        {

            Model.BahaModel Baha = (Model.BahaModel)value;
            Baha.IsIng = true;

            try
            {
                Baha.DeviceId = WebRequest.GetDeviceId(Baha.SN);
                if (Baha.DeviceId == "")
                {
                    Baha.Status = "無法取得DeviceId";
                    Baha.IsIng = false;
                    Baha.IsStop = true;
                    處理影片();
                    return;
                }

                if (!WebRequest.GainAccess(Baha.DeviceId, Baha.SN))
                {
                    Baha.Status = "無法取得GainAccess";
                    Baha.IsIng = false;
                    Baha.IsStop = true;
                    處理影片();
                    return;
                }
                WebRequest.Unlock(Baha.SN);
                WebRequest.CheckLock(Baha.DeviceId, Baha.SN);
                WebRequest.Unlock(Baha.SN);
                WebRequest.Unlock(Baha.SN);
                WebRequest.StartAd(Baha.SN);
                for (int i = 8; i > 0; i--)
                {
                    Baha.Status = "等待" + i.ToString() + "秒跳過廣告...";
                    if (!VideoList.Contains(Baha))
                    {
                        處理影片();
                        return;
                    }
                    Thread.Sleep(1000);
                }
                WebRequest.SkipAd(Baha.SN);

                Baha.Status = "解析Key";
                WebRequest.VideoStart(Baha.SN);
                WebRequest.CheckNoAd(Baha.DeviceId, Baha.SN);
                Baha.Url = WebRequest.GetM3U8(Baha.DeviceId, Baha.SN);
                Baha.Res = WebRequest.ParseMasterList(Baha.Url, Baha.SN, Baha.Quality);
                if (Baha.Res == "")
                {
                    this.Dispatcher.BeginInvoke(new Action(() => { WPFMessageBox.Show("資源清單裡找不到" + Baha.Quality + "p 畫質的影片"); }));
                    Baha.IsIng = false;
                    Baha.IsStop = true;
                    處理影片();
                    return;
                }


                Baha.Tmp = "Tmp\\tmp" + Baha.SN;
                String Path = AppDomain.CurrentDomain.BaseDirectory + Baha.Tmp;

                if (!Directory.Exists(Path))
                {
                    Directory.CreateDirectory(Path);
                }


                if (Baha.ChuckList == null || !File.Exists(Path + "\\" + Baha.Res)) // 如果己經有Chuck 就不用再次下載
                {
                    FileStream file = new FileStream(Path + "\\" + Baha.Res, FileMode.Create);
                    Baha.ChuckList = new List<string>();
                    string KeyUri = WebRequest.DownloadM3U8(Baha.Url.Replace("playlist.m3u8", Baha.Res), Baha.SN, file, Baha.ChuckList);

                    FileStream fileKey = new FileStream(Path + "\\" + Baha.Res + "key", FileMode.Create);
                    WebRequest.Download(KeyUri, Baha.SN, fileKey);
                }


                String prefix = Baha.Url.Remove(Baha.Url.IndexOf("playlist.m3u8"));
                Baha.BarMax = Baha.ChuckList.Count;
                Baha.Bar = 0;

                String[] Files = Directory.GetFiles(Path);

                if (Files.Length > 3)
                {
                    Baha.ChuckList.Count();
                    foreach (String file in Files)
                    {
                        if (!file.EndsWith(".ts")) continue;
                        var q = Baha.ChuckList.Where(I => I.Split('?')[0] == System.IO.Path.GetFileName(file));
                        if (q.Count() > 0)
                        {
                            Baha.ChuckList.Remove(q.First());
                            Baha.Bar++;
                        }

                    }

                }

                foreach (string Chuck in Baha.ChuckList)
                {
                    Baha.Bar++;
                    Baha.Status = string.Format("下載中 ({0}/{1})", Baha.Bar, Baha.BarMax);
                    FileStream ChuckFile = new FileStream(Path + "\\" + Chuck.Remove(Chuck.IndexOf("?token=")), FileMode.Create);
                    if (!WebRequest.Download(prefix + Chuck, Baha.SN, ChuckFile))
                    {
                        Baha.IsIng = false;
                        Baha.IsStop = true;
                        this.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            WPFMessageBox.Show("網路異常...");
                        }));
                        return;
                    }

                    if (!VideoList.Contains(Baha))
                    {
                        DeleteSrcFolder(Path);
                        處理影片();
                        return;
                    }
                }

                Baha.Status = "轉檔中";
                System.Diagnostics.Process process = new System.Diagnostics.Process();
                System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
                startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                startInfo.FileName = "ffmpeg.exe";
                startInfo.Arguments = " -allowed_extensions ALL -y -i " + Baha.Tmp + "/" + Baha.Res + " -c copy " + Local.AniDir + "\\" + Baha.Name + ".mp4";
                process.StartInfo = startInfo;
                process.Start();
                process.WaitForExit();
                if (process != null)
                {
                    process.Close();
                }

                Baha.Status = "下載完成";
                Baha.IsOk = true;
                DeleteSrcFolder(Path);
            }
            catch
            {
                Baha.IsIng = false;
                Baha.IsStop = true;
                this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    WPFMessageBox.Show("下載中出現錯誤...");
                }));
                return;
            }

            Baha.IsIng = false;

            處理影片();
        }

        public static void DeleteSrcFolder(string file)
        {
            DirectoryInfo fileInfo = new DirectoryInfo(file);
            fileInfo.Attributes = FileAttributes.Normal & FileAttributes.Directory;

            File.SetAttributes(file, System.IO.FileAttributes.Normal);
            if (Directory.Exists(file))
            {
                foreach (string f in Directory.GetFileSystemEntries(file))
                {
                    if (File.Exists(f))
                    {
                        File.Delete(f);
                    }
                    else
                    {
                        DeleteSrcFolder(f);
                    }
                }
                Directory.Delete(file);
            }
        }

        private void TB_公司名稱_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            System.Diagnostics.Process.Start(@"https://ani.gamer.com.tw/");
        }

        private void Window_Closed(object sender, EventArgs e)
        {
           
            XDocument xDoc;
            XElement xRoot;

            xDoc = new XDocument(new XComment("設定檔"), new XElement("AniGamer"));
            xRoot = xDoc.Root;
            XElement xWin = new XElement("Windows");
            xWin.Add(new XAttribute("H", (int)this.ActualHeight));
            xWin.Add(new XAttribute("W", (int)this.ActualWidth));
            xWin.Add(new XAttribute("T", (int)this.Top));
            xWin.Add(new XAttribute("L", (int)this.Left));
            xRoot.Add(xWin);
            XElement xDir = new XElement("Dir");
            xDir.Add(new XElement("Ani", Local.AniDir));
            xDir.Add(new XElement("Q", Local.Quality));
            xRoot.Add(xDir);

            xDoc.Save(Local.SetupFile);
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if (DataGrid_清單.SelectedItem == null) return;

            ((Button)sender).Visibility = Visibility.Collapsed;
            var Baha = (Model.BahaModel)DataGrid_清單.SelectedItem;
            Baha.IsStop = false;
            處理影片();
        }
    }
}
