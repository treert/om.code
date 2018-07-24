using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace DownloadWebsite
{
    public class XHttp
    {
        public static string GetWebHostFromUrl(string url)
        {
            Uri info = new Uri(url);
            int idx = info.AbsoluteUri.IndexOf(info.AbsolutePath);
            string web_root = info.AbsoluteUri.Substring(0, idx);
            return web_root;
        }

        public static string GetUrlPathFromUrl(string url)
        {
            return GetWebHostFromUrl(url) + GetFilePathFromUrl(url);
        }

        public static string GetFileNameFromUrl(string url)
        {
            var path = GetFilePathFromUrl(url);
            return path.Substring(path.LastIndexOf('/') + 1);
        }

        public static string GetFilePathFromUrl(string url)
        {
            Uri info = new Uri(url);
            var path = info.AbsolutePath;
            if (path.IndexOf('#') > 0) path = path.Substring(0, path.IndexOf('#'));

            if (path.EndsWith("/")) path += "index.html";
            if (path.StartsWith("/") == false) path = "/" + path;

            {
                string file_name = path.Substring(path.LastIndexOf('/') + 1);
                if (file_name.IndexOf('.') < 0) path += "/index.html";
            }
            return path;
        }

        public static string GetFileDirFromUrl(string url)
        {
            var path = GetFilePathFromUrl(url);
            return path.Substring(0, path.LastIndexOf('/') + 1);
        }

        public static string GetUrlDirFromUrl(string url)
        {
            return GetWebHostFromUrl(url) + GetFileDirFromUrl(url);
        }

        public static string CombineUrl(string dir, string path)
        {
            return dir.TrimEnd('/') + '/' + path.TrimStart('/');
        }

        public enum ResponceType
        {
            Other,
            Html,// Content-Type start with text/html
            Css,// text/css
            Js,// application/javascript
            Text,// text/
            Image,// image/
            
        }
        public class Result{
            public string ContentType;
            public byte[] bytes;
            public string text;
            public ResponceType type;
            public string error;
        }
        // > https://www.cnblogs.com/sun8134/archive/2010/07/05/1771187.html
        public static Result GetData(string url, WebProxy proxy = null)
        {
            Result result = new Result();
            try
            {
                using (WebClient client = new XWebClient())
                {
                    if (proxy != null) client.Proxy = proxy;

                    result.bytes = client.DownloadData(url);
                    if (result.bytes == null)
                    {
                        result.error = $"XHttp.error msg:get none data";
                    }
                    result.ContentType = "";
                    if (client.ResponseHeaders != null)
                    {
                        result.ContentType = client.ResponseHeaders.Get("Content-Type") ?? "";
                    }
                    else
                    {
                        result.error = $"XHttp.error  msg:get none Headers";
                    }

                    if(result.error == null)
                    {
                        if (result.ContentType == "text/html")
                        {
                            result.type = ResponceType.Html;
                            result.text = Encoding.UTF8.GetString(result.bytes);
                        }
                        else if (result.ContentType == "text/css")
                        {
                            result.type = ResponceType.Css;
                            result.text = Encoding.UTF8.GetString(result.bytes);
                        }
                        else if (result.ContentType == "application/javascript")
                        {
                            result.type = ResponceType.Js;
                            result.text = Encoding.UTF8.GetString(result.bytes);
                        }
                        else if (result.ContentType.StartsWith("text/"))
                        {
                            result.type = ResponceType.Text;
                            result.text = Encoding.UTF8.GetString(result.bytes);
                        }
                        else if (result.ContentType.StartsWith("image/"))
                        {
                            result.type = ResponceType.Image;
                        }
                        else
                        {
                            result.type = ResponceType.Other;
                        }
                    }
                }
            }
            catch(Exception e)
            {
                result.error = $"XHttp.exception msg: {e.Message}";
            }

            return result;
        }
    }

    // 让WebClient支持超时时间
    // > https://blog.csdn.net/shellching/article/details/78354029
    // 设置代理
    // > https://blog.csdn.net/alangshan/article/details/30487037
    public class XWebClient : WebClient
    {
        private int _timeout = 60 * 1000;// 默认60秒
        public int Timeout { get { return _timeout; } set { _timeout = value; } }

        public XWebClient()
        {
    
        }

        protected override WebRequest GetWebRequest(Uri address)
        {
            var request = base.GetWebRequest(address);
            request.Timeout = Timeout;
            return request;
        }

        public static WebProxy CreateWebProxy(string host, int port, string user = null, string pwd = null)
        {
            var proxy = new WebProxy(host, port);
            if(string.IsNullOrEmpty(user) != false && pwd != null)
            {
                var cre = new NetworkCredential(user, pwd);
                proxy.Credentials = cre;
            }
            return proxy;
        }
    }
}
