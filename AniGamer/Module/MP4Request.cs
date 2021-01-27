using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Net;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

namespace Module
{
    static class MP4Request
    {
        static public CookieContainer Cookies { set; get; }

        static public WebProxy Proxy { set; get; }


        static HttpWebRequest NewRequset(String Url)
        {
            HttpWebRequest request = HttpWebRequest.Create(Url) as HttpWebRequest;
            request.Method = "GET";
            request.ContentType = "application/x-www-form-urlencoded";
            request.Timeout = 30000;
            request.UserAgent = @"Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/72.0.3626.109 Safari/537.36";
            request.Referer = @"https://anime1.me/";
            request.Headers.Add("origin", @"https://anime1.me/");
            request.CookieContainer = Cookies;

            if( Local.ProxyIP != "")
            {
                request.Proxy = Proxy;
            }

            return request;
        }



        static public String GetTitle(String sn)
        {
            try
            {
                WebClient x = new WebClient();
                x.Encoding = Encoding.UTF8;

                if (Proxy != null) x.Proxy = Proxy;
                
                string html = x.DownloadString(sn);
                Regex rx = new Regex("iframe src=\"(.*) width=");
                MatchCollection m = rx.Matches(html);

                if (m.Count > 0)
                {
                    return m[0].Value.Replace("iframe src=\"", "").Replace("\" width=", "");
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

        static public String GetMP4(String sn)
        {
            try
            {
                WebClient x = new WebClient();
                x.Encoding = Encoding.UTF8;

                if (Proxy != null) x.Proxy = Proxy;

                string html = x.DownloadString(sn);
                Regex rx = new Regex("file:\"(.*)?h=");
                MatchCollection m = rx.Matches(html);

                if (m.Count > 0)
                {
                    return m[0].Value.Replace("file:\"", "").Replace("?h=", "");
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

        static public Boolean Download(String URL , FileStream file)
        {
            try
            {
                HttpWebRequest request = NewRequset(URL);
                using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                {
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
