using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Net;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using System.Collections.Specialized;
using Brotli;
using System.IO.Compression;

namespace Module
{
    static class HAnimeRequest
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
            request.Timeout = 30000;
            request.UserAgent = @"Mozilla/5.0(Windows NT 10.0; Win64; x64) AppleWebKit/537.36(KHTML, like Gecko) Chrome/96.0.4664.110 Safari/537.36";
            request.Accept = @"text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9";
            request.Headers.Add("accept-encoding", "gzip, deflate, br");
            request.Headers.Add("accept-language", "zh-TW,zh;q=0.9,ja;q=0.8,en-US;q=0.7,en;q=0.6,ja-JP;q=0.5");
            request.MaximumAutomaticRedirections = 100;
            request.AllowAutoRedirect = false;

            return request;
        }

        static public String GetTitle(String sn)
        {
            try
            {
                //Cookies = new CookieContainer();
                //HttpRequestClient request = new HttpRequestClient();
                //string heads = @"
                //accept:text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9
                //accept-encoding:gzip, deflate, br
                //accept-language:zh-TW,zh;q=0.9,ja;q=0.8,en-US;q=0.7,en;q=0.6,ja-JP;q=0.5
                //user-agent:Mozilla/5.0(Windows NT 10.0; Win64; x64) AppleWebKit/537.36(KHTML, like Gecko) Chrome/96.0.4664.110 Safari/537.36";
                //string url = @"https://hanime1.me/watch?v=" + sn;
                //string result = request.httpGet(url, heads);
                string result ="";
                HttpWebRequest request = NewRequset(@"https://hanime1.me/watch?v=" + sn, sn);
                using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                {
                    using (BrotliStream stream = new BrotliStream(response.GetResponseStream(), CompressionMode.Decompress))
                    {
                        using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                        {
                            result = reader.ReadToEnd();
                        }
                    }
                }


                if (result != "" && result != null)
                {
                    Regex rx = new Regex("property=\"og:title\" content=\"(.*)\"");
                    MatchCollection m = rx.Matches(result);

                    if (m.Count > 0)
                    {
                        string name = m[0].Value.Replace("property=\"og:title\" content=\"", "").Replace("/", "_").Replace("\"", "").Replace(":", "：");
                        return name;
                    }
                       
                }
                return "";
            }
            catch (Exception EX)
            {
                WPFMessageBox.Show("網路連線出現異常", EX.Message);
                return "";
            }
        }

        static public String GetM3U8(String sn)
        {
            HttpWebRequest request = NewRequset(@"https://hanime1.me/watch?v=" + sn, sn);

            string result = "";
            using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
            {
                using (BrotliStream stream = new BrotliStream(response.GetResponseStream(), CompressionMode.Decompress))
                {
                    using (StreamReader sr = new StreamReader(stream, Encoding.UTF8))
                    {
                        result = sr.ReadToEnd();
                        if (result != "" && result != null)
                        {

                            Regex rx = new Regex("contentUrl\": \"https://(.*)\",");
                            MatchCollection m = rx.Matches(result);
                            if (m.Count > 0)
                            {
                                string name = m[0].Value.Replace("contentUrl\": \"", "").Replace("\",", "");
                                return name;
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
            string Key = URL.Remove(URL.LastIndexOf("/") + 1);

            string fileName = Path.GetFileName(file.Name);
            using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
            {
                StreamWriter SW = new StreamWriter(file);
                using (StreamReader sr = new StreamReader(response.GetResponseStream()))
                {
                    Cookies = request.CookieContainer;
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (line.Contains("https://"))
                        {
                            ChuckList.Add(line);
                            if (line.Contains("/"))
                                line = line.Substring(line.LastIndexOf("/") + 1);
                        }
                        else if (line.Contains(".ts"))
                        {
                           ChuckList.Add(Key + line);
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
