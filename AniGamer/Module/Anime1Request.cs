using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Net;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using System.Collections.Specialized;
using System.Linq;
using System.Data.SQLite;
using System.Data;

namespace Module
{
    static class Anime1Request
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

        static public byte[] AesKey;

        static public void GetChromeCookies(string ValuePath)
        {
            try
            {
                Cookies = new CookieContainer();

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
                    string commandText = @"select * from cookies where host_key like '%v.anime1.me%' ";
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
                            if (Dr.Field<string>("path") != ValuePath) continue;
                            string Key = Dr.Field<string>("name");
                            string val = AesGcm256.ChromeCookies(Dr.Field<byte[]>("encrypted_value"), AesKey);
                            Cookie cookie = new Cookie(Key, val, Dr.Field<string>("path"), Dr.Field<string>("host_key"));
                            Cookies.Add(cookie);
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                string s = ex.Message;
            
            
            }

        }

        public static HttpWebRequest NewRequset(String Url, string sn)
        {
            HttpWebRequest request = HttpWebRequest.Create(Url) as HttpWebRequest;
            request.Method = "GET";
            request.ContentType = "application/x-www-form-urlencoded";
            request.Timeout = 30000;
            request.UserAgent = @"Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/72.0.3626.109 Safari/537.36";
            request.Referer = @"https://anime1.me/" + sn;
            request.Headers.Add("origin", @"https://anime1.me");
            request.CookieContainer = Cookies;
            request.UseDefaultCredentials = true;
            if ( Local.ProxyIP != "")
            {
                request.Proxy = Proxy;
            }

            return request;
        }

        static public String GetTitle(String sn)
        {
            try
            {
                HttpWebRequest request = HttpWebRequest.Create(@"https://anime1.me/" + sn + "#acpwd-" +sn) as HttpWebRequest;
                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";
                request.Timeout = 30000;
                request.UserAgent = @"Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/72.0.3626.109 Safari/537.36";
                request.Referer = @"https://anime1.me/" + sn;
                request.Headers.Add("origin", @"https://anime1.me");
                request.CookieContainer = Cookies;
                string result = "";
                using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                {
                    using (StreamReader sr = new StreamReader(response.GetResponseStream()))
                    {
                     
                        result = sr.ReadToEnd();
                        if (result != "" && result != null)
                        {

                            Regex rx = new Regex("<title>(.*)</title>");
                            MatchCollection m = rx.Matches(result);

                            if (m.Count > 0)
                            {
                                string name = m[0].Value.Replace("<title>", "").Replace("</title>", "");
                                return name.Remove(name.IndexOf("&#8211;")).Replace(" ", "").Replace("/", "_").Replace(":", "："); 
                            }
                            else
                                return "";
                        }
                        return "";
                    }
                }
                
            }
            catch (Exception EX)
            {
                WPFMessageBox.Show("網路連線出現異常", EX.Message);
                return "";
            }
        }


        //static public String SendPass()
        //{
        //    HttpWebRequest request = HttpWebRequest.Create(@"https://anime1.me/category/2019%e5%b9%b4%e6%98%a5%e5%ad%a3/mix#acpwd-9274") as HttpWebRequest;
        //    request.Method = "POST";
        //    request.ContentType = "application/x-www-form-urlencoded";
        //    request.Timeout = 30000;
        //    request.UserAgent = @"Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/72.0.3626.109 Safari/537.36";
        //    request.Referer = @"https://anime1.me/";
        //    request.Headers.Add("origin", @"https://anime1.me");
        //    request.UseDefaultCredentials = true;
        //    if (Cookies == null) Cookies = new CookieContainer();

        //    request.CookieContainer = Cookies;

        //    NameValueCollection postParams = System.Web.HttpUtility.ParseQueryString(string.Empty);
        //    postParams.Add("acpwd-pass", "anime1.me");

        //    if (Local.ProxyIP != "")
        //    {
        //        request.Proxy = Proxy;
        //    }

            
        //    byte[] byteArray = Encoding.UTF8.GetBytes(postParams.ToString());
        //    using (Stream reqStream = request.GetRequestStream())
        //    {
        //        reqStream.Write(byteArray, 0, byteArray.Length);
        //    }

        //    string result = "";
        //    using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
        //    {
        //        using (StreamReader sr = new StreamReader(response.GetResponseStream()))
        //        {
        //            Cookies = request.CookieContainer;
        //            result = sr.ReadToEnd();
        //            return "";
        //        }
        //    }
        //}

        static public String GetM3U8(String sn)
        {
            HttpWebRequest request = NewRequset(@"https://anime1.me/" + sn, sn);

            string result = "";
            using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
            {
                using (StreamReader sr = new StreamReader(response.GetResponseStream()))
                {
                    Cookies = request.CookieContainer;
                    result = sr.ReadToEnd();
                    if (result != "" && result != null)
                    {

                        Regex rx = new Regex("https://i.animeone.me/(.*)?autoplay=1");
                        MatchCollection m = rx.Matches(result);

                        if (m.Count > 0)
                        {
                            string name = m[0].Value.Replace("?autoplay=1", "");
                            return name + ".m3u8";
                        }

                        rx = new Regex("class=\"vframe\" src=\"(.*)\" width");
                        m = rx.Matches(result);
                        if (m.Count > 0)
                        {
                            string name = m[0].Value.Replace("class=\"vframe\" src=\"", "").Replace("\" width", "");
                            return name;
                        }

                        rx = new Regex("data-apireq=\"(.*)\" data-vid=");
                        m = rx.Matches(result);
                        if (m.Count > 0)
                        {
                            string name =  m[0].Value.Replace("data-apireq=\"", "").Replace("\" data-vid=", "");
                            return "v" + name;
                        }

                        return "";
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
                StreamWriter SW = new StreamWriter(file);
                using (StreamReader sr = new StreamReader(response.GetResponseStream()))
                {
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
            HttpWebRequest request = HttpWebRequest.Create(@"https://v.anime1.me/api") as HttpWebRequest;
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


            byte[] byteArray = Encoding.UTF8.GetBytes("d="+d);
            using (Stream reqStream = request.GetRequestStream())
            {
                reqStream.Write(byteArray, 0, byteArray.Length);
            }

            string result = "";
            using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
            {
                using (StreamReader sr = new StreamReader(response.GetResponseStream()))
                {
                    Cookies = new CookieContainer();
                    for (int i = 0; i < response.Headers.Count; i++)
                    {
                        string name = response.Headers.GetKey(i);
                        string value = response.Headers.Get(i);
                        if (name == "Set-Cookie")
                        {
                            Match match = Regex.Match(value, "e=(.+?);");
                            if (match.Captures.Count > 0)
                            {
                                Cookies.Add(new Cookie(match.Value.Split('=')[0], match.Value.Split('=')[1].Trim(';'), "/", request.Host));
                            }
                            match = Regex.Match(value, ",h=(.+?);");
                            if (match.Captures.Count > 0)
                            {
                                    Cookies.Add(new Cookie(match.Value.Split('=')[0].Trim(','), match.Value.Split('=')[1].Trim(';'), "/", request.Host));
                            }
                            match = Regex.Match(value, "p=(.+?);");
                            if (match.Captures.Count > 0)
                            {
                               Cookies.Add(new Cookie(match.Value.Split('=')[0], match.Value.Split('=')[1].Trim(';'), "/", request.Host));
                            }
                        }
                    }

                    result = sr.ReadToEnd();
                    if (result != "" && result != null)
                    {
                        JObject obj = JObject.Parse(result);
                        foreach (var x in obj)
                        {
                            if (x.Key == "s")
                            {
                                var match = Regex.Matches(x.Value.ToString(), "//(.+?)mp4");
                                if (match.Count > 0)
                                {
                                    return match[0].Value;
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
