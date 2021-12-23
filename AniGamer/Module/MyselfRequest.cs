using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Net;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using System.Collections.Specialized;
using System.IO.Compression;

namespace Module
{
    static class MyselfRequest
    {
        static public CookieContainer Cookies { set; get; }

        static public WebProxy Proxy { set; get; }

        static public void ProxyTest(string IP, int port , string user = "" , string pass = "")
        {
            try
            {
                WebClient x = new WebClient();
                x.Encoding = Encoding.UTF8;

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

        public static HttpWebRequest NewRequset(String Url, string sn)
        {
            HttpWebRequest request = HttpWebRequest.Create(Url) as HttpWebRequest;
            request.Method = "GET";
            request.ContentType = "application/x-www-form-urlencoded";
            request.Timeout = 30000;
            request.UserAgent = @"Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/72.0.3626.109 Safari/537.36";
            request.Referer = @"https://myself-bbs.com/thread-" + sn + "-1-1.html";
            request.Headers.Add("origin", @"https://myself-bbs.com/");
            request.CookieContainer = Cookies;
            request.UseDefaultCredentials = true;
            if ( Local.ProxyIP != "")
            {
                request.Proxy = Proxy;
            }

            return request;
        }

        static public List<String> GetTitle(String sn)
        {
            List<String> list = new List<string>();
            try
            {

                Cookies = new CookieContainer();
                HttpWebRequest request = HttpWebRequest.Create(@"https://myself-bbs.com/thread-" + sn + "-1-1.html") as HttpWebRequest;
                request.Method = "GET";
                request.ContentType = "application/x-www-form-urlencoded";
                request.Timeout = 30000;
                request.UserAgent = @"Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/72.0.3626.109 Safari/537.36";
                request.Referer = @"https://myself-bbs.com/forum-113-1.html";
                request.Headers.Add("origin", @"https://myself-bbs.com/");
                request.CookieContainer = Cookies;
                if (Local.ProxyIP != "")
                {
                    request.Proxy = Proxy;
                }


                string result = "";
                using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                {
                    using (StreamReader sr = new StreamReader(response.GetResponseStream()))
                    {
                        Cookies = request.CookieContainer;
                        result = sr.ReadToEnd();
                        if (result != "" && result != null)
                        {

                            Regex rx = new Regex("<title>(.*)</title>");
                            MatchCollection m = rx.Matches(result);

                            if (m.Count > 0)
                            {
                                string name = m[0].Value.Replace("<title>", "").Replace("</title>", "");
                                list.Add(name.Remove(name.IndexOf("-")).Replace(" ", "").Replace("/", "_"));
                                
                                Regex rx2 = new Regex("data-href=\"(.*)\" target=\"_blank\" class=\"various");
                                m = rx2.Matches(result);
                                int i = 0;
                                foreach(Match ss in m)
                                {
                                    i++;
                                    string span = ss.Value.Replace("data-href=\"", "").Replace("\" target=\"_blank\" class=\"various", "").Replace("\r", "");
                                    list.Add(sn +":" + i.ToString());
                                }

                                return list;
                            }
                        }
                        return null;
                    }
                }
                
            }
            catch (Exception EX)
            {
                WPFMessageBox.Show("網路連線出現異常", EX.Message);
                return null;
            }
        }

        static public String GetM3U8(String sn ,String no)
        {
            HttpWebRequest request = NewRequset(@"https://v.myself-bbs.com/vpx/" + sn+ "/"+ no.PadLeft(3, '0'), sn);

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
                        String font = "";
                        String head = "";
                        foreach (var x in obj)
                        {
                            if (x.Key == "video")
                            {
                                font = x.Value.ToString();
                                font = font.Substring(font.IndexOf("720p") + 8);
                                font = font.Remove(font.IndexOf("\""));
                                
                            }
                            if (x.Key == "host")
                            {      
                          
                                head = x.Value.First.ToString();
                                head = head.Substring(head.IndexOf("host") + 8);
                                head = head.Remove(head.IndexOf("\""));
                            }
                        }
                        return head + font;
                        
                    }
                    return "";
                }
            }

        }

        static public String DownloadM3U8(String URL, String sn, FileStream file, List<String> ChuckList)
        {
            HttpWebRequest request = NewRequset(URL, sn);
            string Key = URL.Remove(URL.LastIndexOf("/") + 1);
            
            string fileName = Path.GetFileName(file.Name);
            using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
            {
                Cookies = request.CookieContainer;
                StreamWriter SW = new StreamWriter(file);
                using (GZipStream g = new GZipStream(response.GetResponseStream(), CompressionMode.Decompress))
                {
                    StreamReader sr = new StreamReader(g);
                    Cookies = request.CookieContainer;
                    
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (line.Contains("https://") )
                        {
                            ChuckList.Add(line);
                            if(line.Contains("/"))
                            line = line.Substring(line.LastIndexOf("/") + 1);
                        }
                        else
                        {
                            if( line.Contains(".ts") )
                            {
                                ChuckList.Add(Key + line);
                            }
                        }
                        SW.WriteLine(line);
                    }
                    sr.Dispose();
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


        static public String GetXMLSrc(String URL , string sn)
        {
            HttpWebRequest request = NewRequset(URL, sn);

            string result = "";
            using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
            {
                using (StreamReader sr = new StreamReader(response.GetResponseStream()))
                {
                    Cookies = request.CookieContainer;
                    result = sr.ReadToEnd();
                    if (result != "" && result != null)
                    {

                        Regex rx = new Regex("x.send(.*);");
                        MatchCollection m = rx.Matches(result);

                        if (m.Count > 0)
                        {
                            string name = m[0].Value.Replace("x.send('", "").Replace("');" ,"");
                            return name;
                        }

                        return "";
                    }
                    return "";
                }
            }
        }

        static public String CallAPI(String d)
        {
            HttpWebRequest request = HttpWebRequest.Create(@"https://v.anime1.me/apiv2") as HttpWebRequest;
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.Timeout = 30000;
            request.UserAgent = @"Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/72.0.3626.109 Safari/537.36";
            request.Referer = @"https://anime1.me/";
            request.Headers.Add("origin", @"https://anime1.me");
            request.CookieContainer = Cookies;
            request.UseDefaultCredentials = true;


            if (Local.ProxyIP != "")
            {
                request.Proxy = Proxy;
            }


            byte[] byteArray = Encoding.UTF8.GetBytes(d);
            using (Stream reqStream = request.GetRequestStream())
            {
                reqStream.Write(byteArray, 0, byteArray.Length);
            }

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
                            if (x.Key == "sources")
                            {
                                foreach (var y in x.Value)
                                {
                                    JObject obj2 = JObject.Parse(y.ToString());
                                    foreach (var z in obj2)
                                    {
                                        
                                         return z.Value.ToString();
                                        
                                    }
                                }
                            }
                        }
                    }
                    return "";
                }
            }
        }

    }
}
