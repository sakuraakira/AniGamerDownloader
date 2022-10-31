﻿using Module;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Data.SQLite;
using System.Windows.Threading;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;
using System.Xml.Linq;
using System.Threading;
using System.IO;
using System.Windows.Media;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Security.Cryptography;
using AniGamer.Module;
using System.Text.RegularExpressions;

namespace WPF
{
    /// <summary>
    /// MainForm.xaml 的互動邏輯
    /// </summary>
    public partial class WPF_MainForm : Window
    {
        List<Model.AnimeModel> VideoList { set; get; }
        Model.AnimeModel Baha;
        Thread TH;
    
        public WPF_MainForm()
        {
            InitializeComponent();
            Local.MainForm = this;
          
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


                    XElement xProxy = xRoot.Element("Proxy");
                    if (xProxy != null && xProxy.HasElements)
                    {
                        Local.ProxyIP = xProxy.Element("IP").Value;
                        Int32.TryParse(xProxy.Element("Port").Value, out Local.ProxyPort);
                    }

                    XElement xUser = new XElement("ProxyUser");
                    if (xUser != null && xUser.HasElements)
                    {
                        Local.ProxyUser = xUser.Element("User").Value;
                        Local.ProxyPass = xUser.Element("Pass").Value;
                    }

                }
                catch { }
            }


            if (!Directory.Exists(Local.AniDir))  //確認設置的資料夾是否存在
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

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (!File.Exists(AppDomain.CurrentDomain.BaseDirectory + "ffmpeg.exe"))
            {
                WPFMessageBox.Show("請先下載轉檔工具ffmpeg.exe 放到同一目錄下。");
                Application.Current.Shutdown();
            }

            VideoList = new List<Model.AnimeModel>(); //下載清單的初始化
            if (File.Exists(Local.ListFile))
            {
                try
                {   //如果有清單檔 加載後自動處理下載
                    XDocument xDoc = XDocument.Load(Local.ListFile);
                    XElement xRoot = xDoc.Root;
                    int no = 0;
                    foreach (XElement xVideo in xRoot.Elements("Video"))
                    {
                        no++;
                        VideoList.Add(new Model.AnimeModel() { No = no , From = xVideo.Attribute("From").Value == "0" ? Model.WebFrom.Baha : Model.WebFrom.Anime , SN = xVideo.Attribute("SN").Value, Name = xVideo.Attribute("Name").Value, Status = xVideo.Attribute("Status").Value });
                    };

                    if (VideoList.Count > 0)
                    {
                        DataGrid_清單.ItemsSource = null;
                        DataGrid_清單.ItemsSource = VideoList;
                        處理下載();
                    }
                }
                catch { }
            }
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

                        if (TH != null) //強制關閉執行緒
                        {
                            TH.Abort();
                            TH = null;
                        }
                        Application.Current.Shutdown();

                        break;
                    }
                case "Btn_消字":
                    {
                        TB_搜尋.Text = "";
                        TB_Title.Text = "";
                        ListBox_話.Visibility = Visibility.Collapsed;
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
                        Model.AnimeModel baha = (Model.AnimeModel)DataGrid_清單.SelectedItem;
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
                        Model.AnimeModel baha = (Model.AnimeModel)DataGrid_清單.SelectedItem;

                        try
                        {

                            if(File.Exists(Local.AniDir + "\\" + baha.Name + ".mp4"))
                            {
                               if( WPFMessageBox.Show("是否連同資料夾的檔案也刪除?",WPFMessageBoxButton.YesNo) == WPFMessageBoxResult.Yes)
                                {
                                    File.Delete(Local.AniDir + "\\" + baha.Name + ".mp4");
                                }

                            }

                            VideoList.Remove(baha);
                            DataGrid_清單.ItemsSource = null;
                            DataGrid_清單.ItemsSource = VideoList;
                        }
                        catch
                        {
                            WPFMessageBox.Show("原檔案被開啟或已遺失，無法刪除。");
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


        private void TB_搜尋_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (TB_搜尋.Text.Length == 5 || TB_搜尋.Text.Length > 20)
            {
                TB_Title.Text = "";
                ListBox_話.Visibility = Visibility.Collapsed;

                if (!int.TryParse(TB_搜尋.Text, out int Sn))
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
                    else if (TB_搜尋.Text.Contains("hanime1.me"))
                    {
                        var s = TB_搜尋.Text.Substring(TB_搜尋.Text.IndexOf("me/watch?v=") + 11);
                        if (!int.TryParse(s, out Sn))
                        {
                            WPFMessageBox.Show("不是正確的連結格式。");
                            return;
                        }


                    }
                    else if (TB_搜尋.Text.Contains("anime1.me"))
                    {
                        var s = TB_搜尋.Text.Substring(TB_搜尋.Text.IndexOf("me/") + 3);
                        if (!int.TryParse(s, out Sn))
                        {
                            WPFMessageBox.Show("不是正確的連結格式。");
                            return;
                        }
                    }
                    else if (TB_搜尋.Text.Contains("myself-bbs.com"))
                    {
                        ListBox_話.Visibility = Visibility.Visible;
                        string s = TB_搜尋.Text.Substring(TB_搜尋.Text.IndexOf("thread-") + 7);
                        s = s.Remove(s.IndexOf("-"));
                        if (!int.TryParse(s, out Sn))
                        {
                            WPFMessageBox.Show("不是正確的連結格式。");
                            return;
                        }


                        List<String> Lists = MyselfRequest.GetTitle(s);
                        TB_Title.Text = Lists[0];
                        if (Lists.Count > 1)
                        {
                            ListBox_話.Items.Clear();
                            for (int i = 1; i < Lists.Count; i++)
                            {
                                Button button = new Button
                                {
                                    Tag = Lists[i],
                                    Content = "第" + i.ToString() + "話",
                                    Style = this.FindResource("Button_Span") as Style
                                };
                                button.Click += Button_Click1;
                                ListBox_話.Items.Add(button);
                            }
                        }
                        TB_搜尋.Text = "";
                        return;
                    }
                    else
                    {
                        WPFMessageBox.Show("不是正確的連結格式。");
                        return;
                    }
                }

                Model.AnimeModel Ani = new Model.AnimeModel();

                if (Local.ProxyIP != "" && Local.ProxyPort != 0) //如果有設定Proxy 
                {
                    BahaRequest.Proxy = new System.Net.WebProxy(Local.ProxyIP, Local.ProxyPort);

                    if (Local.ProxyUser != "")
                    {
                        System.Net.ServicePointManager.Expect100Continue = false;
                        BahaRequest.Proxy.UseDefaultCredentials = true;
                        BahaRequest.Proxy.Credentials =  new System.Net.NetworkCredential(Local.ProxyUser, Local.ProxyPass);
                    }
                }

                Ani.SN = Sn.ToString();

                if (TB_搜尋.Text.Contains("sn="))
                {
                    BahaRequest.GetChromeCookies();
                    Ani.Name = BahaRequest.GetTitle(Ani.SN);
                    Ani.From = Model.WebFrom.Baha;
                }
                else if (TB_搜尋.Text.Contains("hanime1.me"))
                {
                    Ani.Name = HAnimeRequest.GetTitle(Ani.SN);
                    Ani.From = Model.WebFrom.HAnime;
                    
                }
                else
                {
                    Ani.Name = Anime1Request.GetTitle(Ani.SN);
                    Ani.From = Model.WebFrom.Anime;
                }


                if (Ani.Name == "")
                {
                    return;
                }
                if(Ani.Name == "巴哈姆特電玩資訊站")
                {
                    WPFMessageBox.Show("找不到SN為" + Ani.SN + "的影片資料。");
                    return;
                }

                if (VideoList.Where(I => I.SN == Ani.SN).Count() > 0)
                {
                    WPFMessageBox.Show("此影片 "+ Ani.Name + " 己經在清單中...");
                    return;
                }

                if(File.Exists(Local.AniDir + "\\" + Ani.Name + ".mp4"))
                {
                   if( WPFMessageBox.Show("在下載資料夾裡己經有同樣名稱為 " + Ani.Name + " 的影片，是否要重新下載?"　,WPFMessageBoxButton.YesNo) == WPFMessageBoxResult.No)
                        return;
                }
                      
                Ani.Status = "排隊中...";

                if (VideoList.Count > 0)
                    Ani.No = VideoList.Max(I => I.No) + 1;
                else
                    Ani.No = 1;
                Ani.Quality = Local.Quality;

                VideoList.Add(Ani);
                DataGrid_清單.ItemsSource = null;
                DataGrid_清單.ItemsSource = VideoList;


                處理下載();
                TB_搜尋.Text = "";
            }
        }

        private void Button_Click1(object sender, RoutedEventArgs e)
        {
            string T = ((Button)sender).Tag.ToString();

            Model.AnimeModel Ani = new Model.AnimeModel
            {
                SN = T.Split(':')[0],
                Span = T.Split(':')[1],
                Name = TB_Title.Text + '-' + T.Split(':')[1],
                From = Model.WebFrom.Myself,
                Status = "排隊中..."
            };

            if (VideoList.Count > 0)
                Ani.No = VideoList.Max(I => I.No) + 1;
            else
                Ani.No = 1;
            Ani.Quality = Local.Quality;

            VideoList.Add(Ani);
            DataGrid_清單.ItemsSource = null;
            DataGrid_清單.ItemsSource = VideoList;


            處理下載();
        }

        void 處理下載()
        {
            if (VideoList.Count == 0) return;
            SaveList();

            if (VideoList.Where(I => I.IsIng).Count() > 0) return;

            var q = VideoList.Where(I => !I.IsOk && !I.IsStop);
            if (q.Count() > 0)
            {
                Model.AnimeModel Ani = q.OrderBy(I => I.No).First();
                if (Ani.From == Model.WebFrom.Baha)
                {
                    TH = new Thread(new ParameterizedThreadStart(BahaDownload));
                    TH.Start(Ani);
                }
                else if(Ani.From == Model.WebFrom.Anime)
                {
                    TH = new Thread(new ParameterizedThreadStart(Anime1Download));
                    TH.Start(Ani);
                }
                else if(Ani.From == Model.WebFrom.Myself)
                {
                    TH = new Thread(new ParameterizedThreadStart(MyselfDownload));
                    TH.Start(Ani);
                }
                else if (Ani.From == Model.WebFrom.HAnime)
                {
                    TH = new Thread(new ParameterizedThreadStart(HAnimeDownload));
                    TH.Start(Ani);
                }
            }
        }


        void BahaDownload(object value)  //主下載功能 不要用主線程來執行
        {

            Model.AnimeModel Baha = (Model.AnimeModel)value;
            Baha.IsIng = true;

            if (Local.ProxyIP != "" && Local.ProxyPort != 0) //如果有設定Proxy 
            {
                BahaRequest.Proxy = new System.Net.WebProxy(Local.ProxyIP, Local.ProxyPort);

                if (Local.ProxyUser != "")
                {
                    System.Net.ServicePointManager.Expect100Continue = false;
                    BahaRequest.Proxy.UseDefaultCredentials = true;
                    BahaRequest.Proxy.Credentials = new System.Net.NetworkCredential(Local.ProxyUser, Local.ProxyPass);
                }
            }

            try
            {
                BahaRequest.GetChromeCookies();
                Baha.DeviceId = BahaRequest.GetDeviceId(Baha.SN);

                if (Baha.DeviceId == "")
                {
                    Baha.Status = "無法取得DeviceId";
                    Baha.IsIng = false;
                    Baha.IsStop = true;
                    處理下載();
                    return;
                }

                if (!BahaRequest.GainAccess(Baha.DeviceId, Baha.SN))
                {
                    Baha.Status = "無法取得GainAccess";
                    Baha.IsIng = false;
                    Baha.IsStop = true;
                    處理下載();
                    return;
                }
                BahaRequest.Unlock(Baha.SN);
                BahaRequest.CheckLock(Baha.DeviceId, Baha.SN);
                BahaRequest.Unlock(Baha.SN);
                BahaRequest.Unlock(Baha.SN);
                string ad = BahaRequest.GetAd(Baha.SN);
                BahaRequest.StartAd(Baha.SN,ad);
                for (int i = 30; i > 0; i--)
                {
                    Baha.Status = "等待" + i.ToString() + "秒跳過廣告...";
                    if (!VideoList.Contains(Baha))
                    {
                        處理下載();
                        return;
                    }
                    Thread.Sleep(1000);
                }
                BahaRequest.SkipAd(Baha.SN, ad);

                Baha.Status = "解析中";
                BahaRequest.VideoStart(Baha.SN);
                BahaRequest.CheckNoAd(Baha.DeviceId, Baha.SN);
                Baha.Url = BahaRequest.GetM3U8(Baha.DeviceId, Baha.SN);
                if (Baha.Url == "")
                {
                    this.Dispatcher.BeginInvoke(new Action(() => { WPFMessageBox.Show("沒有權限取得M3U8，DeviceId：" + Baha.DeviceId + "，SN:" + Baha.SN); }));
                    Baha.IsIng = false;
                    Baha.IsStop = true;
                    處理下載();
                    return;
                }
                Baha.Res = BahaRequest.ParseMasterList(Baha.Url, Baha.SN, Baha.Quality);
                if (Baha.Res == "")
                {
                    this.Dispatcher.BeginInvoke(new Action(() => { WPFMessageBox.Show("資源清單裡找不到" + Baha.Quality + "P 畫質的影片"); }));
                    Baha.IsIng = false;
                    Baha.IsStop = true;
                    處理下載();
                    return;
                }
                Baha.Url = Baha.Url.Remove(Baha.Url.IndexOf("playlist_basic.m3u8"));
                string FileName = Baha.Res.Substring(Baha.Res.LastIndexOf('/') + 1);
                Baha.Tmp = "Tmp\\tmp" + Baha.SN;
                String Path = AppDomain.CurrentDomain.BaseDirectory + Baha.Tmp;

                if (!Directory.Exists(Path))
                {
                    Directory.CreateDirectory(Path);
                }
                

                if (Baha.ChuckList == null || !File.Exists(Path + "\\" + FileName)) // 如果己經有Chuck 就不用再次下載
                {
                    FileStream file = new FileStream(Path + "\\" + FileName, FileMode.Create);
                    Baha.ChuckList = new List<string>();
                    Baha.Url = Baha.Url + Baha.Res;
                    string KeyUri = BahaRequest.DownloadM3U8(Baha.Url , Baha.SN, file, Baha.ChuckList);
                    if (KeyUri.Contains("\""))
                    {
                        KeyUri = KeyUri.Remove(KeyUri.IndexOf("\""));
                    }
                    
                    FileStream fileKey = new FileStream(Path + "\\" + FileName + "key", FileMode.Create);

                    Baha.Url = Baha.Url.Remove(Baha.Url.LastIndexOf("/") + 1);
                    BahaRequest.Download(Baha.Url+KeyUri, Baha.SN, fileKey);
                }


     
                Baha.BarMax = Baha.ChuckList.Count;
                Baha.Bar = 0;

                String[] Files = Directory.GetFiles(Path);
                if (Files.Length > 3)  //如果有暫存檔案 , Chuck移除減少下載
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
                    Baha.Status = string.Format("已下載 ({0}/{1})", Baha.Bar, Baha.BarMax);
                    FileStream ChuckFile = new FileStream(Path + "\\" + Chuck, FileMode.Create);
                    if (!BahaRequest.Download(Baha.Url + Chuck, Baha.SN, ChuckFile))
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
                        處理下載();
                        return;
                    }
                }

                Baha.Status = "轉檔中";
                System.Diagnostics.Process process = new System.Diagnostics.Process();
                System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
                    FileName = "ffmpeg.exe",
                    Arguments = " -allowed_extensions ALL -y -i " + Baha.Tmp + "/" + FileName + " -c copy \"" + Local.AniDir + "\\" + Baha.Name + ".mp4\""
                };
                process.StartInfo = startInfo;
                process.Start();
                process.WaitForExit();
                if (process != null)
                {
                    process.Close();
                }

                Baha.Status = "下載完成";
                Baha.IsIng = false;
                Baha.IsOk = true;
                DeleteSrcFolder(Path);
                處理下載();
            }
            catch(Exception ex)
            {
                this.Dispatcher.Invoke(() => { WPFMessageBox.Show("出錯", ex.Message); });
                
                Baha.IsIng = false;
                Baha.IsStop = true;
                Baha.Status = "下載中出現錯誤...";
                return;
            }


        }

        
        void Anime1Download(object value)  //主下載功能 不要用主線程來執行
        {

            Baha = (Model.AnimeModel)value;
            Baha.IsIng = true;

            if (Local.ProxyIP != "" && Local.ProxyPort != 0) //如果有設定Proxy 
            {
                Anime1Request.Proxy = new System.Net.WebProxy(Local.ProxyIP, Local.ProxyPort);

                if (Local.ProxyUser != "")
                {
                    System.Net.ServicePointManager.Expect100Continue = false;
                    Anime1Request.Proxy.UseDefaultCredentials = true;
                    Anime1Request.Proxy.Credentials = new System.Net.NetworkCredential(Local.ProxyUser, Local.ProxyPass);
                }
            }

            try
            {
               
                Baha.Status = "解析中";
                Baha.Url = Anime1Request.GetM3U8(Baha.SN);
               
                if (Baha.Url.StartsWith("v"))
                {
                    String Src = Anime1Request.CallAPI(Baha.Url.Substring(1));
                    if (Src.Contains(".mp4"))
                    {
                        using (CookieAwareWebClient webClient_ = new CookieAwareWebClient())
                        {
                            string ValuePath = Src.Substring(Src.IndexOf("me/") + 2);
                           // Anime1Request.GetChromeCookies(ValuePath);
                            //webClient_.CookieContainer = Anime1Request.Cookies;
                            webClient_.CookieContainer = Anime1Request.Cookies;
                            if (webClient_.CookieContainer.Count <= 1)
                            {
                                Baha.IsIng = false;
                                Baha.IsStop = true;
                                Baha.Status = "Cookie載入失敗，請刷新Chrome";
                                return;
                            }
                            webClient_.DownloadProgressChanged += WebClient__DownloadProgressChanged;
                            webClient_.DownloadFileCompleted += WebClient__DownloadFileCompleted;
                            webClient_.DownloadFileTaskAsync("https:" + Src, AppDomain.CurrentDomain.BaseDirectory + "Tmp\\tmp" + Baha.SN +".mp4");
                            Baha.Status = "下載中";
                            Baha.BarMax = 100;
                        }
                        return;
                    }
                    else if (Src.Contains(".m3u8"))
                    {
                        Baha.Url = "https:" + Src;
                    }
                    else
                    {
                        WPFMessageBox.Show("未知的影片格式。");
                    }
                }
                if (Baha.Url.Contains("me/"))
                    Baha.Res = Baha.Url.Substring(Baha.Url.LastIndexOf("me/") + 3);
                if (Baha.Url.Contains("playlist"))
                    Baha.Res = Baha.Url.Substring(Baha.Url.LastIndexOf("playlist"));

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
                    string KeyUri = Anime1Request.DownloadM3U8(Baha.Url, Baha.SN, file, Baha.ChuckList);
                }


                String prefix = Baha.Url.Remove(Baha.Url.LastIndexOf("/"));
                Baha.BarMax = Baha.ChuckList.Count;
                Baha.Bar = 0;

                String[] Files = Directory.GetFiles(Path);
                if (Files.Length > 3)  //如果有暫存檔案 , Chuck移除減少下載
                {
                    Baha.ChuckList.Count();
                    foreach (String file in Files)
                    {
                        if (!file.EndsWith(".ts")) continue;
                        var q = Baha.ChuckList.Where(I => I.Substring(I.LastIndexOf("/") + 1) == System.IO.Path.GetFileName(file));
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
                    Baha.Status = string.Format("已下載 ({0}/{1})", Baha.Bar, Baha.BarMax);
                    FileStream ChuckFile = new FileStream(Path + "\\" + Chuck.Substring(Chuck.LastIndexOf("/") + 1), FileMode.Create);
                    if (!Anime1Request.Download(Chuck, Baha.SN, ChuckFile))
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
                        處理下載();
                        return;
                    }
                }

                Baha.Status = "轉檔中";
                System.Diagnostics.Process process = new System.Diagnostics.Process();
                System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
                    FileName = "ffmpeg.exe",
                    Arguments = " -allowed_extensions ALL -y -i " + Baha.Tmp + "/" + Baha.Res + " -c copy \"" + Local.AniDir + "\\" + Baha.Name + ".mp4\""
                };
                process.StartInfo = startInfo;
                process.Start();
                process.WaitForExit();
                if (process != null)
                {
                    process.Close();
                }

                Baha.Status = "下載完成";
                Baha.IsIng = false;
                Baha.IsOk = true;
                DeleteSrcFolder(Path);
                處理下載();

            } catch
            {
                Baha.IsIng = false;
                Baha.IsStop = true;
                Baha.Status = "下載中出現錯誤...";
                return;
            }

        }

        void HAnimeDownload(object value)  //主下載功能 不要用主線程來執行
        {

            Baha = (Model.AnimeModel)value;
            Baha.IsIng = true;

            if (Local.ProxyIP != "" && Local.ProxyPort != 0) //如果有設定Proxy 
            {
                HAnimeRequest.Proxy = new System.Net.WebProxy(Local.ProxyIP, Local.ProxyPort);

                if (Local.ProxyUser != "")
                {
                    System.Net.ServicePointManager.Expect100Continue = false;
                    HAnimeRequest.Proxy.UseDefaultCredentials = true;
                    HAnimeRequest.Proxy.Credentials = new System.Net.NetworkCredential(Local.ProxyUser, Local.ProxyPass);
                }
            }

            try
            {
                Baha.Status = "解析中";
                Baha.Url = HAnimeRequest.GetM3U8(Baha.SN);


                if (Baha.Url.Contains(".m3u8"))
                {

                }
                else if (Baha.Url.Contains(".mp4"))
                {
                    using (WebClient webClient_ = new WebClient())
                    {
                        webClient_.DownloadProgressChanged += WebClient__DownloadProgressChanged;
                        webClient_.DownloadFileCompleted += WebClient__DownloadFileCompleted;
                        webClient_.DownloadFileTaskAsync(Baha.Url, AppDomain.CurrentDomain.BaseDirectory + "Tmp\\tmp" + Baha.SN + ".mp4");
                        Baha.Status = "下載中";
                        Baha.BarMax = 100;
                    }
                    return;
                }
                else
                {
                    WPFMessageBox.Show("未知的影片格式。");
                    return ;
                }

                Baha.Url = Baha.Url.Replace("master", "index-v1-a1");
                Baha.Res = Baha.Url.Substring( Baha.Url.IndexOf("mp4/")+ 4);

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
                    string KeyUri = HAnimeRequest.DownloadM3U8(Baha.Url, Baha.SN, file, Baha.ChuckList);
                }


                String prefix = Baha.Url.Remove(Baha.Url.LastIndexOf("/"));
                Baha.BarMax = Baha.ChuckList.Count;
                Baha.Bar = 0;

                String[] Files = Directory.GetFiles(Path);
                if (Files.Length > 3)  //如果有暫存檔案 , Chuck移除減少下載
                {
                    Baha.ChuckList.Count();
                    foreach (String file in Files)
                    {
                        if (!file.EndsWith(".ts")) continue;
                        var q = Baha.ChuckList.Where(I => I.Substring(I.LastIndexOf("/") + 1) == System.IO.Path.GetFileName(file));
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
                    Baha.Status = string.Format("已下載 ({0}/{1})", Baha.Bar, Baha.BarMax);
                    FileStream ChuckFile = new FileStream(Path + "\\" + Chuck.Substring(Chuck.LastIndexOf("/") + 1), FileMode.Create);
                    if (!HAnimeRequest.Download(Chuck, Baha.SN, ChuckFile))
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
                        處理下載();
                        return;
                    }
                }

                Baha.Status = "轉檔中";
                System.Diagnostics.Process process = new System.Diagnostics.Process();
                System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
                    FileName = "ffmpeg.exe",
                    Arguments = " -allowed_extensions ALL -y -i " + Baha.Tmp + "/" + Baha.Res + " -c copy \"" + Local.AniDir + "\\" + Baha.Name + ".mp4\""
                };
                process.StartInfo = startInfo;
                process.Start();
                process.WaitForExit();
                if (process != null)
                {
                    process.Close();
                }

                Baha.Status = "下載完成";
                Baha.IsIng = false;
                Baha.IsOk = true;
                DeleteSrcFolder(Path);
                處理下載();
            }
            catch
            {
                Baha.IsIng = false;
                Baha.IsStop = true;
                Baha.Status = "下載中出現錯誤...";
                return;
            }
        }

        private void WebClient__DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {

            Baha.Status = "轉檔中";
            Thread.Sleep(200);
            FileStream file = new FileStream(AppDomain.CurrentDomain.BaseDirectory + "Tmp\\tmp" + Baha.SN + ".mp4", FileMode.Open);
            using (var filestream = new FileStream(Local.AniDir + "\\" + Baha.Name + ".mp4", FileMode.Create))
            {
                file.CopyTo(filestream);
                file.Close();
                File.Delete(AppDomain.CurrentDomain.BaseDirectory + "Tmp\\tmp" + Baha.SN + ".mp4");
            }
            Baha.IsIng = false;
            Baha.IsOk = true;
            Baha.Status = "下載完成";
            處理下載();
        }

        private void WebClient__DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            Baha.Bar = e.ProgressPercentage;
            Baha.Status = string.Format("已下載 {0} %", Baha.Bar);
        }

        void MyselfDownload(object value)  //主下載功能 不要用主線程來執行
        {

            Model.AnimeModel Baha = (Model.AnimeModel)value;
            Baha.IsIng = true;

            if (Local.ProxyIP != "" && Local.ProxyPort != 0) //如果有設定Proxy 
            {
                Anime1Request.Proxy = new System.Net.WebProxy(Local.ProxyIP, Local.ProxyPort);

                if (Local.ProxyUser != "")
                {
                    System.Net.ServicePointManager.Expect100Continue = false;
                    Anime1Request.Proxy.UseDefaultCredentials = true;
                    Anime1Request.Proxy.Credentials = new System.Net.NetworkCredential(Local.ProxyUser, Local.ProxyPass);
                }
            }

            try
            {

                Baha.Status = "解析中";
                Baha.Url = "https://vpx.myself-bbs.com/" + Baha.SN + "/" + Baha.Span + "/720p.m3u8" ;
                

                if (Baha.Url.Contains("bbs.com/"))
                    Baha.Res = Baha.Url.Substring(Baha.Url.IndexOf("bbs.com/") + 8).Replace('/', '_');


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
                    string KeyUri = MyselfRequest.DownloadM3U8(Baha.Url, Baha.SN, file, Baha.ChuckList);
                }


                String prefix = Baha.Url.Remove(Baha.Url.LastIndexOf("/"));
                Baha.BarMax = Baha.ChuckList.Count;
                Baha.Bar = 0;

                String[] Files = Directory.GetFiles(Path);
                if (Files.Length > 3)  //如果有暫存檔案 , Chuck移除減少下載
                {
                    Baha.ChuckList.Count();
                    foreach (String file in Files)
                    {
                        if (!file.EndsWith(".ts")) continue;
                        var q = Baha.ChuckList.Where(I => I.Substring(I.LastIndexOf("/") + 1) == System.IO.Path.GetFileName(file));
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
                    Baha.Status = string.Format("已下載 ({0}/{1})", Baha.Bar, Baha.BarMax);
                    FileStream ChuckFile = new FileStream(Path + "\\" + Chuck.Substring(Chuck.LastIndexOf("/") + 1), FileMode.Create);
                    if (!MyselfRequest.Download(Chuck, Baha.SN, ChuckFile))
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
                        處理下載();
                        return;
                    }
                }

                Baha.Status = "轉檔中";
                string S = " -allowed_extensions ALL -y -i " + Baha.Tmp + "/" + Baha.Res + " -c copy \"" + Local.AniDir + "\\" + Baha.Name.Replace("/", "") + ".mp4\"";
                System.Diagnostics.Process process = new System.Diagnostics.Process();
                System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
                    FileName = "ffmpeg.exe",
                    Arguments = S
                };
                process.StartInfo = startInfo;
                process.Start();
                process.WaitForExit();
                if (process != null)
                {
                    process.Close();
                }

                Baha.Status = "下載完成";
                Baha.IsIng = false;
                Baha.IsOk = true;
                DeleteSrcFolder(Path);
                處理下載();

            }
            catch
            {
                Baha.IsIng = false;
                Baha.IsStop = true;
                Baha.Status = "下載中出現錯誤...";
                return;
            }

        }

        public static void DeleteSrcFolder(string file)
        {
            DirectoryInfo fileInfo = new DirectoryInfo(file)
            {
                Attributes = FileAttributes.Normal & FileAttributes.Directory
            };

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
            #region 儲存設定檔
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

            if (VideoList.Where(I => !I.IsOk).Count() == 0)
            {
                File.Delete(Local.ListFile);
            }

            #endregion
        }

        void SaveList()
        {
            XDocument xDoc = new XDocument(new XComment("下載清單"), new XElement("VideoList"));
            XElement xRoot = xDoc.Root;
            foreach(var Baha in VideoList.Where(I=> !I.IsOk))
            {
                XElement xBaha = new XElement("Video", new XAttribute("From", (int)Baha.From), new XAttribute("SN", Baha.SN) , new XAttribute("Name" , Baha.Name) , new XAttribute("Status", Baha.Status ) );
                xRoot.Add(xBaha);
            }
            xDoc.Save(Local.ListFile);
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if (DataGrid_清單.SelectedItem == null) return;

            ((Button)sender).Visibility = Visibility.Collapsed;
            var Baha = (Model.AnimeModel)DataGrid_清單.SelectedItem;
            Baha.IsStop = false;
            處理下載();
        }

        private void TB_Anime1_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            System.Diagnostics.Process.Start(@"https://anime1.me/");
        }
    }
}
