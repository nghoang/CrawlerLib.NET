using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using System.Reflection;
using System.Web;
using System.Xml.XPath;
using System.Xml;
using System.Drawing;

namespace CrawlerLib.Net
{
    public class Utility
    {
        [DllImport("advapi32.dll", SetLastError = true)]
        static extern int RegDisableReflectionKey(IntPtr hBase);

        /// <summary>
        /// Function to download Image from website
        /// </summary>
        /// <param name="_URL">URL address to download image</param>
        /// <returns>Image</returns>
        public static Image DownloadImage(string _URL)
        {
            Image _tmpImage = null;

            try
            {
                // Open a connection
                System.Net.HttpWebRequest _HttpWebRequest = (System.Net.HttpWebRequest)System.Net.HttpWebRequest.Create(_URL);

                _HttpWebRequest.AllowWriteStreamBuffering = true;

                // You can also specify additional header values like the user agent or the referer: (Optional)
                _HttpWebRequest.UserAgent = "Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 5.1)";
                _HttpWebRequest.Referer = "http://www.google.com/";

                // set timeout for 20 seconds (Optional)
                _HttpWebRequest.Timeout = 20000;

                // Request response:
                System.Net.WebResponse _WebResponse = _HttpWebRequest.GetResponse();

                // Open data stream:
                System.IO.Stream _WebStream = _WebResponse.GetResponseStream();

                // convert webstream to image
                _tmpImage = Image.FromStream(_WebStream);

                // Cleanup
                _WebResponse.Close();
                _WebResponse.Close();
            }
            catch (Exception _Exception)
            {
                // Error
                Console.WriteLine("Exception caught in process: {0}", _Exception.ToString());
                return null;
            }

            return _tmpImage;
        }


        static public long GetCurrentUnixTime()
        {
            TimeSpan ts = (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0));
            double unixTime = ts.TotalSeconds;
            return (long)unixTime;
        }

        static public bool CheckInternetAvailable()
        {
            try
            {
                System.Net.IPHostEntry objIPHE = System.Net.Dns.GetHostEntry("www.google.com");
            }
            catch
            {
                return false;
            }
            return true;
        }
        static public string EncodeTo64(string toEncode)
        {
            byte[] toEncodeAsBytes
                  = System.Text.ASCIIEncoding.ASCII.GetBytes(toEncode);
            string returnValue
                  = System.Convert.ToBase64String(toEncodeAsBytes);
            return returnValue;
        }

        static public List<string> ReadAttribueXpath(string content, string xpath, string attr)
        {
            List<string> res = new List<string>();
            XmlDocument xml = new XmlDocument();
            xml.LoadXml(content);
            XmlNodeList xnList = xml.SelectNodes(xpath);
            foreach (XmlNode xn in xnList)
            {
                res.Add(xn.Attributes[attr].Value);
            }
            return res;
        }

        static public List<string> ReadInnerTextXpath(string content, string xpath)
        {
            List<string> res = new List<string>();
            XmlDocument xml = new XmlDocument();
            xml.LoadXml(content);
            XmlNodeList xnList = xml.SelectNodes(xpath);
            foreach (XmlNode xn in xnList)
            {
                res.Add(xn.InnerText);
            }
            return res;
        }

        static public string ReadInnerTextXpathSingle(string content, string xpath)
        {
            List<string> res = ReadInnerTextXpath( content,  xpath);
            if (res.Count == 0)
                return "";
            return res[0];
        }

        static public string HtmlDecode(string input)
        {
            return HttpUtility.HtmlDecode(input);
        }

        static public string HtmlEncode(string input)
        {
            return HttpUtility.HtmlEncode(input);
        }

        static public void WriteAppRegistry(string application_name, string key_name, string key_value)
        {
            WriteRegistry("SOFTWARE\\" + application_name, key_name, key_value, "user");
        }

        static public string ReadAppRegistry(string application_name, string key_name)
        {
            return ReadRegistry("SOFTWARE\\" + application_name, key_name, "user");
        }

        static public void WriteRegistry(string keyLocation, string key_name, string key_value, string type)
        {
            RegistryKey key = null;
            if (type == "machine")
            {
                key = Registry.LocalMachine.CreateSubKey(keyLocation);
            }
            else if (type == "user")
            {
                key = Registry.CurrentUser.CreateSubKey(keyLocation);
            }

            try
            {
                Type Regtype = typeof(RegistryKey);
                FieldInfo fi = Regtype.GetField(
                "hkey",
                BindingFlags.NonPublic | BindingFlags.Instance);
                SafeHandle handle = (SafeHandle)fi.GetValue(key);
                IntPtr realHandle = handle.DangerousGetHandle();
                int errorCode = RegDisableReflectionKey(handle.DangerousGetHandle());
            }
            catch (Exception ex)
            { }

            key.SetValue(key_name, key_value);
        }

        static public string ReadRegistry(string keyLocation, string key_name, string type)
        {
            RegistryKey key = null;
            if (type == "machine")
            {
                key = Registry.LocalMachine.CreateSubKey(keyLocation);
            }
            else if (type == "user")
            {
                key = Registry.CurrentUser.CreateSubKey(keyLocation);
            }

            try
            {
                Type Regtype = typeof(RegistryKey);
                FieldInfo fi = Regtype.GetField(
                "hkey",
                BindingFlags.NonPublic | BindingFlags.Instance);
                SafeHandle handle = (SafeHandle)fi.GetValue(key);
                IntPtr realHandle = handle.DangerousGetHandle();
                int errorCode = RegDisableReflectionKey(handle.DangerousGetHandle());
            }
            catch (Exception ex)
            { }

            string res = (string)key.GetValue(key_name);
            if (res != null)
                return res;
            else
                return "";
        }

        static public string DecodeTo64(string data)
        {
            try
            {
                System.Text.UTF8Encoding encoder = new System.Text.UTF8Encoding();
                System.Text.Decoder utf8Decode = encoder.GetDecoder();

                byte[] todecode_byte = Convert.FromBase64String(data);
                int charCount = utf8Decode.GetCharCount(todecode_byte, 0, todecode_byte.Length);
                char[] decoded_char = new char[charCount];
                utf8Decode.GetChars(todecode_byte, 0, todecode_byte.Length, decoded_char, 0);
                string result = new String(decoded_char);
                return result;
            }
            catch (Exception e)
            {
                throw new Exception("Error in base64Decode" + e.Message);
            }
        }

        static public string md5String(string Value)
        {
            System.Security.Cryptography.MD5CryptoServiceProvider x = new System.Security.Cryptography.MD5CryptoServiceProvider();
            byte[] data = System.Text.Encoding.ASCII.GetBytes(Value);
            data = x.ComputeHash(data);
            string ret = "";
            for (int i = 0; i < data.Length; i++)
                ret += data[i].ToString("x2").ToLower();
            return ret;
        }

        static public string URLEncode(String v)
        {
            return System.Web.HttpUtility.UrlEncode(v);
        }

        static public string URLDecode(String v)
        {
            return System.Web.HttpUtility.UrlDecode(v);
        }

        static public void WriteFile(string fn, string content, bool append)
        {
            System.IO.StreamWriter file = new System.IO.StreamWriter(fn, append);
            file.Write(content);
            file.Close();
        }

        static public string ReadFile(string fn)
        {
            StreamReader SR;
            string res = "";
            string S;
            SR = File.OpenText(fn);
            S = SR.ReadLine();
            while (S != null)
            {
                res += S;
                S = SR.ReadLine();
            }
            SR.Close();
            return res;
        }

        static public bool DownloadFile(string url, string fn)
        {
            WebclientX Client = new WebclientX();
            try
            {
                Client.DownloadFile(url, fn);
            }
            catch (Exception ex)
            {
                WriteLog(ex.Message);
                return false;
            }
            return true;
        }

        static public void WriteLog(string mes)
        {
            WriteFile("log.txt", "\n" + DateTime.Now + " >> " + mes, true);
        }

        static public void WriteLog(string mes, int count)
        {
            string content = ReadFileString("log.txt");
            string[] lines = content.Split('\n');
           
            string newContent = "";
            for (int i = lines.Length - 1; i >= 0; i--)
            {
                newContent = lines[i] + "\n" + newContent;
                count--;
                if (count < 0)
                    break;
            }
            newContent += "\n" + mes;
            WriteFile("log.txt", newContent, false);
        }

        public static string ReadFileString(string path)
        {
            if (IsFileExist(path) == false)
                return "";
            string content = System.IO.File.ReadAllText(path, Encoding.UTF8);
            return content;
        }

        public static string[] ListFileInFolder(string path)
        {
            string[] filePaths = Directory.GetFiles(path);
            return filePaths;
        }

        public static string StripTags(string source)
        {
            char[] array = new char[source.Length];
            int arrayIndex = 0;
            bool inside = false;

            for (int i = 0; i < source.Length; i++)
            {
                char let = source[i];
                if (let == '<')
                {
                    inside = true;
                    continue;
                }
                if (let == '>')
                {
                    inside = false;
                    continue;
                }
                if (!inside)
                {
                    array[arrayIndex] = let;
                    arrayIndex++;
                }
            }
            return new string(array, 0, arrayIndex);
        }

        public static List<string> SimpleRegex(string pattern, string content, int group, RegexOptions options)
        {
            Regex exp = new Regex(pattern, options);
            MatchCollection MatchList = exp.Matches(content);
            List<string> res = new List<string>();
            foreach (Match m in MatchList)
            {
                res.Add(m.Groups[group].Value);
            }
            return res;
        }

        public static string SimpleRegexSingle(string pattern, string content, int group, RegexOptions options)
        {
            List<string> res = SimpleRegex(pattern, content, group, options);
            if ((res.Count > 0))
            {
                return res[0];
            }
            else
            {
                return "";
            }
        }

        public static List<string> SimpleRegex(string pattern, string content, int group)
        {
            return SimpleRegex(pattern, content, group, RegexOptions.Singleline);
        }

        public static string SimpleRegexSingle(string pattern, string content, int group)
        {
            return SimpleRegexSingle(pattern, content, group, RegexOptions.Singleline);
        }


        public static bool IsFileExist(string p)
        {
            return File.Exists(p);
        }
    }
}
