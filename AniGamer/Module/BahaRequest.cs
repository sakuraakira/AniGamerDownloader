﻿using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Net;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using System.Data.SQLite;
using System.Data;
using System.Linq;
using System.Web;
using Newtonsoft.Json;

namespace Module
{
    static class BahaRequest
    {
        static public byte[] AesKey;
        static public CookieContainer Cookies { set; get; }


        static public void GetChromeCookies()
        {
            try
            {
                Cookies = new CookieContainer(40);
              
                string UserPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        
                 if (System.IO.File.Exists(UserPath + @"\AppData\Local\Google\Chrome\User Data\Local State"))
                 {
                     using (StreamReader r = new StreamReader(UserPath + @"\AppData\Local\Google\Chrome\User Data\Local State"))
                     {
                         string json = r.ReadToEnd();
                         dynamic array = JValue.Parse(json);
                         dynamic crypt = array.os_crypt;
                         string base64 = crypt.encrypted_key;
                         var base64EncodedBytes = System.Convert.FromBase64String(base64);
                         Byte[] Code = base64EncodedBytes.Skip(5).ToArray();
                         AesKey = Module.DPAPI.Decrypt(Code, null, out string C);
                     }
                 }
                
                string path = UserPath + @"\AppData\Local\Google\Chrome\User Data\Default\Network\Cookies";

                if (AesKey != null && System.IO.File.Exists(path))
                {

                    SQLiteConnection connection = new System.Data.SQLite.SQLiteConnection("Data Source = " + path);
                    connection.Open();
                    string commandText = @"select * from cookies where host_key like '%gamer.com%' ";
                    SQLiteCommand command = new SQLiteCommand(commandText, connection);
                    command.ExecuteNonQuery();
                    SQLiteDataAdapter da = new SQLiteDataAdapter(commandText, connection);
                    DataSet ds = new DataSet();
                    ds.Clear();
                    da.Fill(ds);
                    connection.Close();

                    DataTable DT = ds.Tables[0];
                    if (DT != null && DT.Rows.Count > 0)
                    {
                        foreach (DataRow Dr in DT.Rows)
                        {
                            string Key = Dr.Field<string>("name");
                            string val = AesGcm256.ChromeCookies(Dr.Field<byte[]>("encrypted_value"), AesKey);
                            if (val.Contains("{"))
                            {
                                val = "";
                            }
                            Cookie cookie = new Cookie(Key, val, Dr.Field<string>("path"), Dr.Field<string>("host_key"));
                            
                            //Cookies.SetCookies(new Uri("https://ani.gamer.com.tw"), Key + "=" + val);
                            Cookies.Add(cookie);
                        }

                    }
                }
            }
            catch { }
            
        }

        static public WebProxy Proxy { set; get; }

        static public void ProxyTest(string IP, int port , string user = "" , string pass = "")
        {
            try
            {
                WebClient x = new WebClient
                {
                    Encoding = Encoding.UTF8
                };

                WebProxy proxy = new WebProxy(IP, port);
                if(user != "")
                {
                    ServicePointManager.Expect100Continue = false;
                    proxy.UseDefaultCredentials = true;
                    proxy.Credentials = new NetworkCredential(user, pass);
                }

                x.Proxy = proxy;
                string publicIp = x.DownloadString("https://api.ipify.org");
                WPFMessageBox.Show("連線成功 IP: " + publicIp);
            }
            catch (Exception ex)
            {
                WPFMessageBox.Show(ex.Message);
            }  
        }

        static HttpWebRequest NewRequset(String Url, string sn)
        {
            HttpWebRequest request = HttpWebRequest.Create(Url) as HttpWebRequest;
            request.Method = "GET";
            request.ContentType = "application/x-www-form-urlencoded";
            request.Timeout = 30000;
            request.UserAgent = @"Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/96.0.4664.110 Safari/537.36";
            request.Referer = @"https://ani.gamer.com.tw/animeVideo.php?sn=" + sn;
            request.Headers.Add("origin", @"https://ani.gamer.com.tw");
            request.CookieContainer = Cookies;
           
            if( Local.ProxyIP != "")
            {
                request.Proxy = Proxy;
            }

            return request;
        }

        static String Request(string Url, string sn)
        {
            HttpWebRequest request = NewRequset(Url,sn);
            string result = "";
            using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
            {
                using (StreamReader sr = new StreamReader(response.GetResponseStream()))
                {
                    Cookies = request.CookieContainer;
                    result = sr.ReadToEnd();
                }
                response.Close();
            }
            return result;
        }

        static public String GetTitle(String sn)
        {
            try
            {
                WebClient x = new WebClient
                {
                    Encoding = Encoding.UTF8
                };

                if (Proxy != null) x.Proxy = Proxy;
                
                string html = x.DownloadString(@"https://ani.gamer.com.tw/animeVideo.php?sn=" + sn);
                Regex rx = new Regex("<title>(.*)</title>");
                MatchCollection m = rx.Matches(html);

                if (m.Count > 0)
                {
                    String str = m[0].Value.Replace("<title>", "").Replace("</title>", "");
                    str = HttpUtility.HtmlDecode(str);
                    return str.Remove(str.IndexOf("線上看")).Trim().Replace("/", "_").Replace(":", "：");
                }
                else
                    return "";
            }
            catch (Exception EX)
            {
                WPFMessageBox.Show("網路連線出現異常", EX.Message);
                return "";
            }
        }

        static public String GetDeviceId(String sn)
        {
            
            
            HttpWebRequest request = NewRequset(@"https://ani.gamer.com.tw/ajax/getdeviceid.php?id=", sn);
            using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
            {
                using (StreamReader sr = new StreamReader(response.GetResponseStream()))
                {
                    Cookies = request.CookieContainer;
                    string result = sr.ReadToEnd();
                    if (result != "" && result != null)
                    {
                        JObject obj = JObject.Parse(result);
                        foreach (var x in obj)
                        {
                            if (x.Key == "deviceid")
                            {
                                return x.Value.ToString();
                            }
                        }
                    }
                    return "";
                }
            }
        }


        public static bool GainAccess(String rid, String sn)
        {
            string STR = @"https://ani.gamer.com.tw/ajax/token.php?adID=0&sn=" + sn + "&device=" + rid + "&hash=" + Local.RandomString(12);
            HttpWebRequest request = NewRequset(STR, sn);

            using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
            {
                using (StreamReader sr = new StreamReader(response.GetResponseStream()))
                {
                    Cookies = request.CookieContainer;
                    string result = sr.ReadToEnd();
                    if (result != "" && result != null)
                    {
                        JObject obj = JObject.Parse(result);
                        foreach (var x in obj)
                        {
                            if (x.Key == "error")
                            {
                                return false;
                            }
                        }
                    }else
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public static Boolean CheckNoAd(String rid, String sn)
        {
            string STR = @"https://ani.gamer.com.tw/ajax/token.php?sn=" + sn + "&device=" + rid + "&hash=" + Local.RandomString(12);
            HttpWebRequest request = NewRequset(STR, sn);

            Boolean re;
            using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
            {
                using (StreamReader sr = new StreamReader(response.GetResponseStream()))
                {
                    Cookies = request.CookieContainer;
                    string result = sr.ReadToEnd();
                    if (result != "" && result != null)
                    {
                        JObject obj = JObject.Parse(result);
                        foreach (var x in obj)
                        {
                            if (x.Key == "time")
                            {
                                if (float.Parse(x.Value.ToString()) == 1)
                                {
                                    re = true;
                                }
                            }
                        }
                    }
                    re = false;
                }
                response.Close();
            }

            return re;
        }

        public static void StartAd(String sn)
        {
            string STR = @"https://ani.gamer.com.tw/ajax/videoCastcishu.php?sn=" + sn + "&s=20200513";
            Request(STR, sn);
        }

        public static void SkipAd(String sn)
        {
            string STR = @"https://ani.gamer.com.tw/ajax/videoCastcishu.php?sn=" + sn + "&s=20200513&ad=end";
            Request(STR, sn);
        }

        public static void Unlock(String sn)
        {
            string STR = @"https://ani.gamer.com.tw/ajax/unlock.php?sn=" + sn + "&ttl=0";
             Request(STR, sn);
        }

        public static void CheckLock(String rid, String sn)
        {
            string STR = @"https://ani.gamer.com.tw/ajax/checklock.php?device="+ rid + "&sn=" + sn ;
            Request(STR, sn);
        }

        public static void VideoStart(String sn)
        {
            string STR = @"https://ani.gamer.com.tw/ajax/videoStart.php?sn=" + sn;
            Request(STR, sn);
        }

        static public String GetM3U8(String rid, String sn)
        {
            HttpWebRequest request = NewRequset(@"https://ani.gamer.com.tw/ajax/m3u8.php?sn=" + sn + "&device=" + rid, sn);
            using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
            {
                using (StreamReader sr = new StreamReader(response.GetResponseStream()))
                {
                    Cookies = request.CookieContainer;
                    string result = sr.ReadToEnd();
                    if (result != "" && result != null)
                    {
                        JObject obj = JObject.Parse(result);
                        foreach (var x in obj)
                        {
                            if (x.Key == "src")
                            {
                                return x.Value.ToString();
                            }
                        }
                    }
                    return "";
                }
            }

        }

        static public String ParseMasterList(String MUrl, String sn, String Quality)
        {
            HttpWebRequest request = NewRequset(MUrl, sn);

            using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
            {
                using (StreamReader sr = new StreamReader(response.GetResponseStream()))
                {
                    Cookies = request.CookieContainer;
                    String line;

                    while ((line = sr.ReadLine()) != null)
                    {
                        if (line.StartsWith("#EXT-X-STREAM-INF"))
                        {
                            string q = line.Split('x')[1];
                            if (Quality == q)
                            {
                                String nextLine = sr.ReadLine();
                                return nextLine.Split('?')[0];
                            }
                        }
                    }
                }
            }

            return "";
        }

        static public String DownloadM3U8(String URL, String sn, FileStream file, List<String> ChuckList)
        {
            HttpWebRequest request = NewRequset(URL, sn);
            string Key = "";
            
            string fileName = Path.GetFileName(file.Name);
            using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
            {
                Cookies = request.CookieContainer;
                StreamWriter SW = new StreamWriter(file);
                using (StreamReader sr = new StreamReader(response.GetResponseStream()))
                {
                    Cookies = request.CookieContainer;
                    string line ;
                    Boolean GetURI = false;
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (!GetURI && line.Contains("URI="))
                        {
                            Key = line.Substring(line.IndexOf("URI=") + 4).Trim('\"');
                            line = line.Remove(line.IndexOf("URI=") + 4);
                            line += "\"" + fileName + "key\"";
                        }
                        if (line.Contains(".ts"))
                        {
                            ChuckList.Add(line);
                        }
                        SW.WriteLine(line);
                    }

                }
                
                SW.Dispose();
                file.Close();
                return Key;
            }
        }

        static public Boolean Download(String URL, String sn, FileStream file)
        {
            try
            {
                HttpWebRequest request = NewRequset(URL, sn);
                using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                {
                    Cookies = request.CookieContainer;
                    Stream dataStream = response.GetResponseStream();
                    byte[] buffer = new byte[1024];
                    int size = 0;
                    do
                    {
                        size = dataStream.Read(buffer,0, buffer.Length);
                        if (size > 0)
                            file.Write(buffer, 0, size);
                    } while (size > 0);
                    dataStream.Close();
                    file.Close();
                    return true;
                }
            }
            catch { return false; }

        }

    }
}
