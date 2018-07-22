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
        }
        // > https://www.cnblogs.com/sun8134/archive/2010/07/05/1771187.html
        public static Result GetData(string url)
        {
            Result result = new Result();
            using (WebClient client = new WebClient())
            {
                result.bytes = client.DownloadData(url);
                result.ContentType = "";
                if (client.ResponseHeaders != null)
                {
                    result.ContentType = client.ResponseHeaders.Get("Content-Type") ?? "";
                }

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
            return result;
        }
    }
}
