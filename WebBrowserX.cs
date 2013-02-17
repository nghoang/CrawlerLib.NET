using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Collections.Specialized;
using System.Runtime.InteropServices;
using System.Net;

namespace CrawlerLib.Net
{

    public class WebBrowserX : WebBrowser
    {
        Object authObject = null;
        public struct INTERNET_PROXY_INFO
        {
            public int dwAccessType;
            public IntPtr proxy;
            public IntPtr proxyBypass;
        };

        [DllImport("wininet.dll", SetLastError = true)]
        private static extern bool InternetSetOption(IntPtr hInternet,
        int dwOption, IntPtr lpBuffer, int lpdwBufferLength);

        private void RefreshIESettings(string strProxy)
        {
            const int INTERNET_OPTION_PROXY = 38;
            const int INTERNET_OPEN_TYPE_PROXY = 3;

            INTERNET_PROXY_INFO struct_IPI;

            // Filling in structure
            struct_IPI.dwAccessType =
            INTERNET_OPEN_TYPE_PROXY;
            struct_IPI.proxy =
            Marshal.StringToHGlobalAnsi(strProxy);
            struct_IPI.proxyBypass =
     Marshal.StringToHGlobalAnsi("local");

            // Allocating memory
            IntPtr intptrStruct =
     Marshal.AllocCoTaskMem(Marshal.SizeOf(struct_IPI));

            // Converting structure to IntPtr
            Marshal.StructureToPtr(struct_IPI, intptrStruct, true);

            bool iReturn = InternetSetOption(IntPtr.Zero,
     INTERNET_OPTION_PROXY,
     intptrStruct,
     Marshal.SizeOf(struct_IPI));
        }

        public void GetMethod(string url)
        {
            if (authObject != null)
                this.Navigate(url, null, null, (string)authObject);
            else
                this.Navigate(url, null, null, null);
        }
        
        public CookieContainer GetCookieContainer(string path, string domain)
        {
            CookieContainer container = new CookieContainer();

            foreach (string cookie in this.Document.Cookie.Split(';'))
            {
                string name = cookie.Split('=')[0];
                string value = cookie.Substring(name.Length + 1);
                container.Add(new Cookie(name.Trim(), value.Trim(), path, domain));
            }

            return container;
        }

        public void PostMethod(string url, NameValueCollection prms)
        {
            string req = "";
            for (int i = 0; i < prms.Count; i++)
            {
                if (i == 0)
                    req += prms.GetKey(i) + "=" + Utility.URLEncode(prms.GetValues(i).GetValue(0).ToString());
                else
                    req += "&" + prms.GetKey(i) + "=" + Utility.URLEncode(prms.GetValues(i).GetValue(0).ToString());
            }
            if (authObject != null)
                this.Navigate(url, null, ASCIIEncoding.ASCII.GetBytes(req), (string)authObject);
            else
                this.Navigate(url, null, ASCIIEncoding.ASCII.GetBytes(req), null);
        }

        public void SetProxyString(string px)
        {
            string pxReg = "([^:]*):([^:]*):{0,1}([^:]*):{0,1}([^:]*)";
            string ip = Utility.SimpleRegexSingle(pxReg, px, 1);
            string port = Utility.SimpleRegexSingle(pxReg, px, 2);
            string username = Utility.SimpleRegexSingle(pxReg, px, 3);
            string password = Utility.SimpleRegexSingle(pxReg, px, 4);
            if (!string.IsNullOrEmpty(username) & !string.IsNullOrEmpty(password))
            {
                var credentialStringValue = "user:pass";
                var credentialByteArray = ASCIIEncoding.ASCII.GetBytes(credentialStringValue);
                var credentialBase64String = Convert.ToBase64String(credentialByteArray);

                Object nullObject = 0;
                Object nullObjectString = "";
                authObject = string.Format("Proxy-Authorization: Basic {0}{1}", credentialBase64String, Environment.NewLine);

            }
            RefreshIESettings(ip + ":" + port);
        }

        public void SetProxyFile(string pxf)
        {
            string data = Utility.ReadFileString(pxf);
            string[] lines = data.Split(Environment.NewLine.ToCharArray());
            while (true)
            {
                if (lines.Length == 0)
                    break;
                Random r = new Random();
                string line = lines[r.Next(lines.Length)];
                if (line.Trim() != "")
                {
                    this.SetProxyString(line);
                    break;
                }
            }
        }

        
        //Declare the WIN32 API calls to get the entries from IE's history cache  
        [DllImport("wininet.dll", SetLastError = true)]
        public static extern IntPtr FindFirstUrlCacheEntry(string lpszUrlSearchPattern, IntPtr lpFirstCacheEntryInfo, out UInt32 lpdwFirstCacheEntryInfoBufferSize);

        [DllImport("wininet.dll", SetLastError = true)]
        public static extern long FindNextUrlCacheEntry(IntPtr hEnumHandle, IntPtr lpNextCacheEntryInfo, out UInt32 lpdwNextCacheEntryInfoBufferSize);

        [DllImport("wininet.dll", SetLastError = true)]
        public static extern long FindCloseUrlCache(IntPtr hEnumHandle);

        [DllImport("Wininet.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern Boolean GetUrlCacheEntryInfo(String lpxaUrlName, IntPtr lpCacheEntryInfo, ref int lpdwCacheEntryInfoBufferSize);
        [StructLayout(LayoutKind.Sequential)]
        public struct INTERNET_CACHE_ENTRY_INFO
        {
            public UInt32 dwStructSize;
            public string lpszSourceUrlName;
            public string lpszLocalFileName;
            public UInt32 CacheEntryType;
            public UInt32 dwUseCount;
            public UInt32 dwHitRate;
            public UInt32 dwSizeLow;
            public UInt32 dwSizeHigh;
            public FILETIME LastModifiedTime;
            public FILETIME ExpireTime;
            public FILETIME LastAccessTime;
            public FILETIME LastSyncTime;
            public IntPtr lpHeaderInfo;
            public UInt32 dwHeaderInfoSize;
            public string lpszFileExtension;
            public UInt32 dwExemptDelta;
        };

        public static class Hresults
        {
            public const int ERROR_SUCCESS = 0;
            public const int ERROR_FILE_NOT_FOUND = 2;
            public const int ERROR_ACCESS_DENIED = 5;
            public const int ERROR_INSUFFICIENT_BUFFER = 122;
            public const int ERROR_NO_MORE_ITEMS = 259;
        };
        //private static void getUrlEntriesInHistory(TextWriter writer)  
        public static List<string> getUrlEntriesInHistory(string sourceUrlFilter, string fileExtensionFilter)
        {
            long currentLastAccess = -1;
            List<string> filesList = new List<string>();
            IntPtr buffer = IntPtr.Zero;
            UInt32 structSize;
            const string urlPattern = "Visited:";

            //This call will fail but returns the size required in structSize  
            //to allocate necessary buffer  
            IntPtr hEnum = FindFirstUrlCacheEntry(null, buffer, out structSize);
            try
            {
                if (hEnum == IntPtr.Zero)
                {
                    int lastError = Marshal.GetLastWin32Error();
                    if (lastError == Hresults.ERROR_INSUFFICIENT_BUFFER)
                    {
                        //Allocate buffer  
                        buffer = Marshal.AllocHGlobal((int)structSize);
                        //Call again, this time it should succeed  
                        //hEnum = FindFirstUrlCacheEntry(urlPattern, buffer, out structSize);  
                        hEnum = FindFirstUrlCacheEntry(null, buffer, out structSize);
                    }
                    else if (lastError == Hresults.ERROR_NO_MORE_ITEMS)
                    {
                        Console.Error.WriteLine("No entries in IE's history cache");
                        //return;  
                        return filesList;
                    }
                    else if (lastError != Hresults.ERROR_SUCCESS)
                    {
                        Console.Error.WriteLine("Unable to fetch entries from IE's history cache");
                        //return;  
                        return filesList;
                    }
                }


                INTERNET_CACHE_ENTRY_INFO result = (INTERNET_CACHE_ENTRY_INFO)Marshal.PtrToStructure(buffer, typeof(INTERNET_CACHE_ENTRY_INFO));
                //writer.WriteLine(result.lpszSourceUrlName);  
                string fileUrl = result.lpszSourceUrlName.Substring(result.lpszSourceUrlName.LastIndexOf('@') + 1);
                
                if (fileUrl.Contains(sourceUrlFilter) && fileUrl.EndsWith(fileExtensionFilter) &&
                    result.LastAccessTime.dwHighDateTime > currentLastAccess)
                {
                    currentLastAccess = result.LastAccessTime.dwHighDateTime;
                    //Console.WriteLine(fileUrl);
                    filesList.Add(fileUrl);
                }


                // Free the buffer  
                if (buffer != IntPtr.Zero)
                {
                    try { Marshal.FreeHGlobal(buffer); }
                    catch { }
                    buffer = IntPtr.Zero;
                    structSize = 0;
                }

                //Loop through all entries, attempt to find matches  
                while (true)
                {
                    long nextResult = FindNextUrlCacheEntry(hEnum, buffer, out structSize);
                    if (nextResult != 1) //TRUE  
                    {
                        int lastError = Marshal.GetLastWin32Error();
                        if (lastError == Hresults.ERROR_INSUFFICIENT_BUFFER)
                        {
                            buffer = Marshal.AllocHGlobal((int)structSize);
                            nextResult = FindNextUrlCacheEntry(hEnum, buffer, out structSize);
                        }
                        else if (lastError == Hresults.ERROR_NO_MORE_ITEMS)
                        {
                            break;
                        }
                    }

                    result = (INTERNET_CACHE_ENTRY_INFO)Marshal.PtrToStructure(buffer, typeof(INTERNET_CACHE_ENTRY_INFO));
                    //writer.WriteLine(result.lpszSourceUrlName);  
                    fileUrl = result.lpszSourceUrlName.Substring(result.lpszSourceUrlName.LastIndexOf('@') + 1);
                    if (fileUrl.Contains(sourceUrlFilter) && fileUrl.EndsWith(fileExtensionFilter)
                        && result.LastAccessTime.dwHighDateTime > currentLastAccess)
                    {
                        currentLastAccess = result.LastAccessTime.dwHighDateTime;
                        //Console.WriteLine(fileUrl);
                        filesList.Add(fileUrl);
                    }


                    if (buffer != IntPtr.Zero)
                    {
                        try { Marshal.FreeHGlobal(buffer); }
                        catch { }
                        buffer = IntPtr.Zero;
                        structSize = 0;
                    }
                }
            }
            finally
            {
                if (hEnum != IntPtr.Zero)
                {
                    FindCloseUrlCache(hEnum);
                }
                if (buffer != IntPtr.Zero)
                {
                    try { Marshal.FreeHGlobal(buffer); }
                    catch { }
                }
            }
            return filesList;
        }

        const int ERROR_FILE_NOT_FOUND = 2;
        
        struct LPINTERNET_CACHE_ENTRY_INFO
        {
            public int dwStructSize;
            IntPtr lpszSourceUrlName;
            public IntPtr lpszLocalFileName;
            int CacheEntryType;
            int dwUseCount;
            int dwHitRate;
            int dwSizeLow;
            int dwSizeHigh;
            FILETIME LastModifiedTime;
            FILETIME Expiretime;
            FILETIME LastAccessTime;
            FILETIME LastSyncTime;
            IntPtr lpHeaderInfo;
            int dwheaderInfoSize;
            IntPtr lpszFileExtension;
            int dwEemptDelta;
        }
        public static string GetPathForCachedFile(string fileUrl)
        {
            int cacheEntryInfoBufferSize = 0;
            IntPtr cacheEntryInfoBuffer = IntPtr.Zero;
            int lastError; Boolean result;
            try
            {
                // call to see how big the buffer needs to be
                result = GetUrlCacheEntryInfo(fileUrl, IntPtr.Zero, ref cacheEntryInfoBufferSize);
                lastError = Marshal.GetLastWin32Error();
                if (result == false)
                {
                    if (lastError == ERROR_FILE_NOT_FOUND) return null;
                }
                // allocate the necessary amount of memory
                cacheEntryInfoBuffer = Marshal.AllocHGlobal(cacheEntryInfoBufferSize);

                // make call again with properly sized buffer
                result = GetUrlCacheEntryInfo(fileUrl, cacheEntryInfoBuffer, ref cacheEntryInfoBufferSize);
                lastError = Marshal.GetLastWin32Error();
                if (result == true)
                {
                    Object strObj = Marshal.PtrToStructure(cacheEntryInfoBuffer, typeof(LPINTERNET_CACHE_ENTRY_INFO));
                    LPINTERNET_CACHE_ENTRY_INFO internetCacheEntry = (LPINTERNET_CACHE_ENTRY_INFO)strObj;
                    //INTERNET_CACHE_ENTRY_INFO internetCacheEntry = (INTERNET_CACHE_ENTRY_INFO)strObj;
                    String localFileName = Marshal.PtrToStringAuto(internetCacheEntry.lpszLocalFileName); return localFileName;
                }
                else return null;// file not found
            }
            finally
            {
                if (!cacheEntryInfoBuffer.Equals(IntPtr.Zero)) Marshal.FreeHGlobal(cacheEntryInfoBuffer);
            }
        }
    }
}
