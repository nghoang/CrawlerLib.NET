using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Collections.Specialized;
using System.IO.Compression;
using System.IO;

namespace CrawlerLib.Net
{
    public class WebclientX : WebClient
    {
        CookieContainer _cookie_container = new CookieContainer();
        string _error = "";
        string _error_code = "";

        public string UploadFileEx(string uploadfile, string url,
            string fileFormName, string contenttype, NameValueCollection querystring)
        {
            if ((fileFormName == null) ||
                (fileFormName.Length == 0))
            {
                fileFormName = "file";
            }

            if ((contenttype == null) ||
                (contenttype.Length == 0))
            {
                contenttype = "application/octet-stream";
            }


            string postdata;
            postdata = "?";
            if (querystring != null)
            {
                foreach (string key in querystring.Keys)
                {
                    postdata += key + "=" + querystring.Get(key) + "&";
                }
            }
            Uri uri = new Uri(url + postdata);


            string boundary = "----------" + DateTime.Now.Ticks.ToString("x");
            HttpWebRequest webrequest = (HttpWebRequest)WebRequest.Create(uri);
            webrequest.CookieContainer = _cookie_container;
            webrequest.ContentType = "multipart/form-data; boundary=" + boundary;
            webrequest.Method = "POST";


            // Build up the post message header
            StringBuilder sb = new StringBuilder();
            sb.Append("--");
            sb.Append(boundary);
            sb.Append("\r\n");
            sb.Append("Content-Disposition: form-data; name=\"");
            sb.Append(fileFormName);
            sb.Append("\"; filename=\"");
            sb.Append(Path.GetFileName(uploadfile));
            sb.Append("\"");
            sb.Append("\r\n");
            sb.Append("Content-Type: ");
            sb.Append(contenttype);
            sb.Append("\r\n");
            sb.Append("\r\n");

            string postHeader = sb.ToString();
            byte[] postHeaderBytes = Encoding.UTF8.GetBytes(postHeader);

            // Build the trailing boundary string as a byte array
            // ensuring the boundary appears on a line by itself
            byte[] boundaryBytes =
                   Encoding.ASCII.GetBytes("\r\n--" + boundary + "\r\n");

            FileStream fileStream = new FileStream(uploadfile,
                                        FileMode.Open, FileAccess.Read);
            long length = postHeaderBytes.Length + fileStream.Length +
                                                   boundaryBytes.Length;
            webrequest.ContentLength = length;

            Stream requestStream = webrequest.GetRequestStream();

            // Write out our post header
            requestStream.Write(postHeaderBytes, 0, postHeaderBytes.Length);

            // Write out the file contents
            byte[] buffer = new Byte[checked((uint)Math.Min(4096,
                                     (int)fileStream.Length))];
            int bytesRead = 0;
            while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) != 0)
                requestStream.Write(buffer, 0, bytesRead);

            // Write out the trailing boundary
            requestStream.Write(boundaryBytes, 0, boundaryBytes.Length);
            WebResponse responce = webrequest.GetResponse();
            Stream s = responce.GetResponseStream();
            StreamReader sr = new StreamReader(s);
            fileStream.Close();
            return sr.ReadToEnd();
        }

        public string GetLastError()
        {
            return _error;
        }

        public void Debug()
        {
            this.SetProxyString("127.0.0.1:8888::");
        }

        public WebclientX()
        {
            ServicePointManager.Expect100Continue = false;
            this.Headers.Add("User-Agent", "Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1; WOW64; Trident/5.0)");
        }

        public CookieContainer GetCookieContainer()
        {
            return _cookie_container;
        }

        public void SetCookieContainer(CookieContainer cookieC)
        {
            _cookie_container = cookieC;
        }

        public void SetProxyFile(string file)
        {
            string data = Utility.ReadFileString(file);
            string[] lines = data.Split('\n');
            while (true)
            {
                if (lines.Length == 0)
                    break;
                Random r = new Random();
                string line = lines[r.Next(lines.Length)];
                if (line.Trim() != "")
                {
                    this.SetProxyString(line.Trim());
                    break;
                }
            }
        }

        public void SetProxyString(string px)
        {
            Console.WriteLine("change proxy: " + px);
            string pxReg = "([^:]*):([^:]*):{0,1}([^:]*):{0,1}([^:]*)";
            string ip = Utility.SimpleRegexSingle(pxReg, px, 1);
            string port = Utility.SimpleRegexSingle(pxReg, px, 2);
            string username = Utility.SimpleRegexSingle(pxReg, px, 3);
            string password = Utility.SimpleRegexSingle(pxReg, px, 4);
            WebProxy wp = new WebProxy(ip + ":" + port);
            if (!string.IsNullOrEmpty(username) & !string.IsNullOrEmpty(password))
            {
                wp.Credentials = new NetworkCredential(username, password);
            }
            this.Proxy = wp;
        }

        byte[] Decompress(byte[] gzip)
        {
            using (GZipStream stream = new GZipStream(new MemoryStream(gzip), CompressionMode.Decompress))
            {
                const int size = 4096;
                byte[] buffer = new byte[size];
                using (MemoryStream memory = new MemoryStream())
                {
                    int count = 0;
                    do
                    {
                        count = stream.Read(buffer, 0, size);
                        if (count > 0)
                        {
                            memory.Write(buffer, 0, count);
                        }
                    }
                    while (count > 0);
                    return memory.ToArray();
                }
            }
        }

        public string GetMethod(string url)
        {
            _error = "";
            _error_code = "";
            string content = "";
           
            try
            {
                byte[] data = this.DownloadData(url);
                if (ResponseHeaders["Content-Encoding"] == null || ResponseHeaders["Content-Encoding"] == "")
                {
                    content = Encoding.UTF8.GetString(data);
                }
                else if (ResponseHeaders["Content-Encoding"] == "gzip")
                {
                    data = Decompress(data);
                    content = Encoding.UTF8.GetString(data);
                }
            }
            catch (WebException ex)
            {
                _error = ex.Message;
                CreateErrorCode(ex);
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
            return content;
        }

        private void CreateErrorCode(WebException ex)
        {
            if (ex.Response is HttpWebResponse)
            {
                switch (((HttpWebResponse)ex.Response).StatusCode)
                {
                    case HttpStatusCode.NotFound:
                        _error_code = "404";
                        break;
                    case HttpStatusCode.Forbidden:
                        _error_code = "403";
                        break;
                    case HttpStatusCode.BadGateway:
                        _error_code = "502";
                        break;
                    case HttpStatusCode.BadRequest:
                        _error_code = "400";
                        break;
                    case HttpStatusCode.Conflict:
                        _error_code = "409";
                        break;
                    case HttpStatusCode.ExpectationFailed:
                        _error_code = "417";
                        break;
                    case HttpStatusCode.GatewayTimeout:
                        _error_code = "504";
                        break;
                    case HttpStatusCode.RequestTimeout:
                        _error_code = "408";
                        break;
                    case HttpStatusCode.NonAuthoritativeInformation:
                        _error_code = "203";
                        break;
                    case HttpStatusCode.Unauthorized:
                        _error_code = "401";
                        break;
                    default:
                        throw ex;
                }
            }
        }

        public string GetResponseStatusCode()
        {
            return _error_code;
        }

        public string PostMethod(string url, NameValueCollection prms)
        {
            _error = "";
            _error_code = "";
            string content = "";
            try
            {
                byte[] data = this.UploadValues(url, prms);
                if (ResponseHeaders["Content-Encoding"] == null || ResponseHeaders["Content-Encoding"] == "")
                {
                    content = Encoding.UTF8.GetString(data);
                }
                else if (ResponseHeaders["Content-Encoding"] == "gzip")
                {
                    data = Decompress(data);
                    content = Encoding.UTF8.GetString(data);
                }
            }
            catch (WebException ex)
            {
                _error = ex.Message;
                CreateErrorCode(ex);
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
            return content;
        }

        WebResponse _last_response = null;
        protected override WebResponse GetWebResponse(WebRequest request)
        {
            try
            {
                WebResponse response = base.GetWebResponse(request);
                last_url = response.ResponseUri.AbsoluteUri;
                _last_response = response;
                return response;
            }
            catch (Exception ex)
            {
                return null;
            }
        }


        protected override WebRequest GetWebRequest(Uri address)
        {
            WebRequest request = base.GetWebRequest(address);
            if (request is HttpWebRequest)
            {
                (request as HttpWebRequest).CookieContainer = _cookie_container;
            }
            return request;
        }

        string last_url = "";

        public WebResponse GetLastResponse()
        {
            return _last_response;
        }

        public string GetLastUrl()
        {
            return last_url;
        }
    }
}
