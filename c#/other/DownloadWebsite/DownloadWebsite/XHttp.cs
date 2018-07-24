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
        public static Result GetData(string url)
        {
            Result result = new Result();
            try
            {
                using (WebClient client = new XWebClient())
                {
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

    }
}
