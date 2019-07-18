using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Net;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

namespace Module
{
    static class WebRequest
    {
        static public CookieContainer Cookies { set; get; }

        static public WebProxy Proxy { set; get; }

        static HttpWebRequest NewRequset(String Url, string sn)
        {
            HttpWebRequest request = HttpWebRequest.Create(Url) as HttpWebRequest;
            request.Method = "GET";
            request.ContentType = "application/x-www-form-urlencoded";
            request.Timeout = 30000;
            request.UserAgent = @"Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/72.0.3626.109 Safari/537.36";
            request.Referer = @"https://ani.gamer.com.tw/animeVideo.php?sn=" + sn;
            request.Headers.Add("origin", @"https://ani.gamer.com.tw");
            request.CookieContainer = Cookies;

            if(Proxy != null)
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
                WebClient x = new WebClient();
                x.Encoding = Encoding.UTF8;

                if (Proxy != null) x.Proxy = Proxy;
                
                string html = x.DownloadString(@"https://ani.gamer.com.tw/animeVideo.php?sn=" + sn);
                Regex rx = new Regex("<title>(.*)</title>");
                MatchCollection m = rx.Matches(html);

                if (m.Count > 0)
                {
                    return m[0].Value.Replace("<title>", "").Replace("</title>", "").Split('-')[0].Trim().Replace(" ", ",");
                }
                else
                    return sn;
            }
            catch (Exception EX)
            {
                WPFMessageBox.Show("網路連線出現異常", EX.Message);
                return sn;
            }
        }

        static public String GetDeviceId(String sn)
        {
            Cookies = new CookieContainer();
            HttpWebRequest request = NewRequset(@"https://ani.gamer.com.tw/ajax/getdeviceid.php?id=", sn);
            string result = "";
            using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
            {
                using (StreamReader sr = new StreamReader(response.GetResponseStream()))
                {
                    Cookies = request.CookieContainer;
                    result = sr.ReadToEnd();
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
            string STR = @"https://ani.gamer.com.tw/ajax/videoCastcishu.php?sn=" + sn + "&s=194699";
            String Rep = Request(STR, sn);
        }

        public static void SkipAd(String sn)
        {
            string STR = @"https://ani.gamer.com.tw/ajax/videoCastcishu.php?sn=" + sn + "&s=194699&ad=end";
            String Rep = Request(STR, sn);
        }

        public static void Unlock(String sn)
        {
            string STR = @"https://ani.gamer.com.tw/ajax/unlock.php?sn=" + sn + "&ttl=0";
            String Rep = Request(STR, sn);
        }

        public static void CheckLock(String rid, String sn)
        {
            string STR = @"https://ani.gamer.com.tw/ajax/checklock.php?device="+ rid + "&sn=" + sn ;
            String Rep = Request(STR, sn);
        }

        public static void VideoStart(String sn)
        {
            string STR = @"https://ani.gamer.com.tw/ajax/videoStart.php?sn=" + sn;
            String Rep = Request(STR, sn);
        }

        static public String GetM3U8(String rid, String sn)
        {
            HttpWebRequest request = NewRequset(@"https://ani.gamer.com.tw/ajax/m3u8.php?sn=" + sn + "&device=" + rid, sn);

            string result = "";
            using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
            {
                using (StreamReader sr = new StreamReader(response.GetResponseStream()))
                {
                    Cookies = request.CookieContainer;
                    result = sr.ReadToEnd();
                    if (result != "" && result != null)
                    {
                        JObject obj = JObject.Parse(result);
                        foreach (var x in obj)
                        {
                            if (x.Key == "src")
                            {
                                return "https:" + x.Value.ToString();
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
                    string line;
                    Boolean GetURI = false;
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (!GetURI && line.Contains("URI="))
                        {
                            Key = line.Substring(line.IndexOf("URI=") + 4).Trim('\"');
                            line = line.Remove(line.IndexOf("URI=") + 4);
                            line += "\"" + fileName + "key\"";
                        }
                        if (line.Contains("?token="))
                        {
                            ChuckList.Add(line);
                            line = line.Remove(line.IndexOf("?token="));
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
                        size = dataStream.Read(buffer, 0, buffer.Length);
                        if (size > 0)
                            file.Write(buffer, 0, size);
                    } while (size > 0);
                    file.Close();
                    return true;
                }
            }
            catch { return false; }

        }

    }
}
