using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DownloadWebsite
{
    //public class XHttp
    //{
    //    public enum ResponceType
    //    {
    //        Other,
    //        Html,// Content-Type start with text/html
    //        Css,// text/css
    //        Js,// application/javascript
    //        Text,// text/
    //        Image,// image/
    //    }
    //    public class Result{
    //        public string ContentType;
    //        public byte[] bytes;
    //        public string text;
    //        public ResponceType type;
    //        public string error;
    //    }
    //    // > https://www.cnblogs.com/sun8134/archive/2010/07/05/1771187.html
    //    public static Result GetData(string url, WebProxy proxy = null)
    //    {
    //        Result result = new Result();
    //        try
    //        {
    //            using (WebClient client = new XWebClient())
    //            {
    //                if (proxy != null) client.Proxy = proxy;

    //                result.bytes = client.DownloadData(url);
    //                if (result.bytes == null)
    //                {
    //                    result.error = $"XHttp.error msg:get none data";
    //                }
    //                result.ContentType = "";
    //                if (client.ResponseHeaders != null)
    //                {
    //                    result.ContentType = client.ResponseHeaders.Get("Content-Type") ?? "";
    //                }
    //                else
    //                {
    //                    result.error = $"XHttp.error  msg:get none Headers";
    //                }

    //                if(result.error == null)
    //                {
    //                    if (result.ContentType == "text/html")
    //                    {
    //                        result.type = ResponceType.Html;
    //                        result.text = Encoding.UTF8.GetString(result.bytes);
    //                    }
    //                    else if (result.ContentType == "text/css")
    //                    {
    //                        result.type = ResponceType.Css;
    //                        result.text = Encoding.UTF8.GetString(result.bytes);
    //                    }
    //                    else if (result.ContentType == "application/javascript")
    //                    {
    //                        result.type = ResponceType.Js;
    //                        result.text = Encoding.UTF8.GetString(result.bytes);
    //                    }
    //                    else if (result.ContentType.StartsWith("text/"))
    //                    {
    //                        result.type = ResponceType.Text;
    //                        result.text = Encoding.UTF8.GetString(result.bytes);
    //                    }
    //                    else if (result.ContentType.StartsWith("image/"))
    //                    {
    //                        result.type = ResponceType.Image;
    //                    }
    //                    else
    //                    {
    //                        result.type = ResponceType.Other;
    //                    }
    //                }
    //            }
    //        }
    //        catch(Exception e)
    //        {
    //            result.error = $"XHttp.exception msg: {e.Message}";
    //        }

    //        return result;
    //    }
    //}

    // 包装Uri,按照自己的需要解析url
    // 这儿主要需要
    // 1. 比较Url，确定是否是同一个网站地址
    // 2. 获取path和dir，方便转换成本地file路径
    public class XUrl
    {
        // 后缀类型 > https://www.cnblogs.com/SingleCat/p/5141716.html
        public static readonly string[] FileExtImg = new string[] { "jpg","png","gif","ico",""}; 

        #region Attr
        public Uri m_uri;
        public string m_origin_url;// 创建时传入的url
        
        // 最终会转义非法字符
        public string m_file_dir;// [/path]/
        public string m_file_path;// [/path]/{file.ext} 会做格式处理，如果不满足file.ext，就添加index.html
        public string m_file_name;// {file.ext}

        public string m_url_host;// http[s]://[user[:pwd]@]host[:port]
        public string m_url_dir;// {m_url_host}{m_file_dir}
        public string m_url_path;// {m_url_host}{m_file_path} = {m_url_dir}{m_file_name}
        public string m_url_full;// {m_url_path}[?query][#fragment] 应该等同于Uri.AbsoluteUri，但是可能增加了index.html
        #endregion Attr
        #region Flag
        public bool m_add_index;// 是否添加了index.html
        public bool m_is_html;// m_file_name.EndWith(".html")
        public bool m_has_query;// 是否带有query参数
        #endregion

        // 出现异常返回null
        public static XUrl TryParser(string url)
        {
            try
            {
                return Parser(url);
            }
            catch (Exception)
            {
                return null;
            }
        }

        // 只支持http[s]://xxx，会抛异常的
        public static XUrl Parser(string url)
        {
            if (IsHttpUrl(url) == false) return null;
            Uri uri = new Uri(url);// url 格式不对，这行就抛异常了
            var ret = new XUrl();
            ret.InitInfoFromUri(uri);

            return ret;
        }

        public void InitInfoFromUri(Uri uri)
        {
            m_uri = uri;
            m_origin_url = uri.OriginalString;
            m_file_path = uri.AbsolutePath;
            m_add_index = false;
            if (m_file_path.EndsWith("/"))
            {
                m_file_path += "index.html";
                m_add_index = true;
            }
            m_file_dir = m_file_path.Substring(0, m_file_path.LastIndexOf('/') + 1);
            m_file_name = m_file_path.Substring(m_file_dir.Length);
            
            m_url_host = uri.Scheme + "://" + uri.UserInfo + (uri.UserInfo.Length > 0 ? "@" : "") + uri.Authority;
            m_url_dir = m_url_host + m_file_dir;
            m_url_path = m_url_host + m_file_path;
            m_url_full = m_url_path + uri.Query + uri.Fragment;

            m_is_html = m_file_name.EndsWith(".html");
            m_has_query = uri.Query != "";

            m_file_path = ConvertUrlFragToPath(m_file_path);
            m_file_dir = ConvertUrlFragToPath(m_file_dir);
            m_file_name = ConvertUrlFragToPath(m_file_name);
        }

        // 判断是否是Scheme型的协议url，弱判断，只判断头部
        [ThreadStatic]
        static Regex m_reg_scheme_head = new Regex(@"^[a-zA-Z][\w-]{0,20}:");
        public static bool IsSchemeUrl(string url)
        {
            if(m_reg_scheme_head == null) m_reg_scheme_head = new Regex(@"^[a-zA-Z][\w-]{0,20}:");
            return m_reg_scheme_head.IsMatch(url);
        }

        [ThreadStatic]
        static Dictionary<char,string> s_invalid_chars_in_file_name = new Dictionary<char,string>();
        static void TryInitInvalidCharsInFileName()
        {
            if(s_invalid_chars_in_file_name == null) s_invalid_chars_in_file_name = new Dictionary<char, string>();

            if (s_invalid_chars_in_file_name.Count == 0)
            {
                foreach (var ch in Path.GetInvalidFileNameChars())
                {
                    int i = (int)ch;
                    s_invalid_chars_in_file_name.Add(ch,$"${i:X2}");
                }
            }
        }
        [ThreadStatic]
        static StringBuilder s_common_sb = new StringBuilder();
        static void TryInitCommonSb()
        {
            if(s_common_sb == null) s_common_sb = new StringBuilder();
        }
        public static string ConvertUrlFragToPath(string url_frag)
        {
            url_frag = Uri.UnescapeDataString(url_frag);
            TryInitCommonSb();
            StringBuilder sb = s_common_sb;
            sb.Clear();
            TryInitInvalidCharsInFileName();
            foreach (var ch in url_frag)
            {
                if (ch == '/') sb.Append(ch);
                else if(s_invalid_chars_in_file_name.ContainsKey(ch))
                {
                    sb.Append(s_invalid_chars_in_file_name[ch]);
                }
                else
                {
                    sb.Append(ch);
                }
            }
            return sb.ToString();
        }

        public static string ConvertPathToUrlFrag(string path)
        {
            TryInitCommonSb();
            StringBuilder sb = s_common_sb;
            sb.Clear();
            TryInitInvalidCharsInFileName();
            for(var i = 0; i < path.Length; i++)
            {
                var ch = path[i];
                if (ch == '/') sb.Append(ch);
                else if(ch == '#')
                {
                    sb.Append(path, i, path.Length - i);
                }
                else if (ch == '$' && i + 2 < path.Length && Uri.IsHexDigit(path[i+1]) && Uri.IsHexDigit(path[i + 2]))
                {
                    int num = Uri.FromHex(path[i + 1]) * 16 + Uri.FromHex(path[i + 2]);
                    sb.Append((char)num);
                    i += 2;
                }
                else
                {
                    sb.Append(ch);
                }
            }
            return sb.ToString();
        }

        public static bool IsHttpUrl(string url)
        {
            return url.StartsWith("http://") || url.StartsWith("https://");
        }

        public static string Combine(string baseurl, string relateurl)
        {
            return baseurl.TrimEnd('/') + "/" + relateurl.TrimStart('/');
        }
    }

    // XWebClient返回的结果
    public class XWebResult
    {
        // > https://www.cnblogs.com/SingleCat/p/5141716.html
        public enum XContentType
        {
            Other,
            Html = 1,// Content-Type start with text/html
            Css,// text/css
            Js,// application/javascript or application/ecmascript
            Json,// application/json
            Text,// text/
            Image,// image/
            Audio,// audio/
            Video,// video/
            // 下面两个不知道干啥的
            Message,// message/
            Drawing,// drawing/
        }

        public string contentType;
        public byte[] bytes;
        public string text;
        public XContentType type;
        public string error;
    }

    // 让WebClient支持超时时间
    // > https://blog.csdn.net/shellching/article/details/78354029
    // 设置代理
    // > https://blog.csdn.net/alangshan/article/details/30487037
    // 只获取头部
    // > https://stackoverflow.com/questions/6237734/how-to-request-only-the-http-header-with-c
    public class XWebClient : WebClient
    {
        private int _timeout = 60 * 1000;// 默认60秒
        public int Timeout { get { return _timeout; } set { _timeout = value; } }

        public XWebClient()
        {
    
        }

        public static string GetHttpResponseUrl(string url)
        {
            try
            {
                var req = WebRequest.CreateHttp(url);
                req.AllowAutoRedirect = true;// 最终会获取到实际响应的url地址
                req.Method = "HEAD";
                req.Timeout = 10 * 1000;// 只获取头部，应该很快
                using (var res = (HttpWebResponse)req.GetResponse())
                {
                    if(res.StatusCode == HttpStatusCode.OK)
                    {
                        return res.ResponseUri.AbsoluteUri;
                    }
                    else
                    {
                        return null;// 只接受200
                    }
                }
            }
            catch (Exception)
            {
                return null;// 发生错误，不管它
            }
        }

        public XWebResult GetData(string url)
        {
            XWebResult result = new XWebResult();
            try
            {
                result.bytes = DownloadData(url);
                if (result.bytes == null)
                {
                    result.error = $"XHttp.error msg:get none data";
                }
                result.contentType = "";
                if (ResponseHeaders != null)
                {
                    result.contentType = ResponseHeaders.Get("Content-Type") ?? "";
                }
                else
                {
                    result.error = $"XHttp.error  msg:get none Headers";
                }

                if (result.error == null)
                {
                    if (result.contentType == "text/html")
                    {
                        result.type = XWebResult.XContentType.Html;
                        result.text = Encoding.UTF8.GetString(result.bytes);
                    }
                    else if (result.contentType == "text/css")
                    {
                        result.type = XWebResult.XContentType.Css;
                        result.text = Encoding.UTF8.GetString(result.bytes);
                    }
                    else if (result.contentType == "application/javascript" || result.contentType == "application/ecmascript")
                    {
                        result.type = XWebResult.XContentType.Js;
                        result.text = Encoding.UTF8.GetString(result.bytes);
                    }
                    else if (result.contentType.StartsWith("text/"))
                    {
                        result.type = XWebResult.XContentType.Text;
                        result.text = Encoding.UTF8.GetString(result.bytes);
                    }
                    else if (result.contentType.StartsWith("image/"))
                    {
                        result.type = XWebResult.XContentType.Image;
                    }
                    else
                    {
                        result.type = XWebResult.XContentType.Other;
                    }
                }
            }
            catch (Exception e)
            {
                result.error = $"XHttp.exception msg: {e.Message}";
            }

            return result;
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
