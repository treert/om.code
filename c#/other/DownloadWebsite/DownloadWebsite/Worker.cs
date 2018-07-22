using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using System.Text.RegularExpressions;

namespace DownloadWebsite
{
    class Worker
    {
        public static Worker singleton = new Worker();

        public event Action m_refresh_log;

        public string m_web_root = "";
        public string m_save_dir = "";
        public LinkedList<string> m_log_list = new LinkedList<string>();
        public HashSet<string> m_urls_set = new HashSet<string>();
        public string m_status = "";
        Thread m_thread = null;

        public void Init()
        {
            m_save_dir = Path.Combine(Environment.CurrentDirectory, "website");
            m_log_list.Clear();
            Log("test", "xxx");
            Log("test", "xxx");
        }

        void Log(string tag, string msg)
        {
            var log = $"{tag,-10} {msg}";
            m_log_list.AddFirst(log);
            RefreshLog();
        }

        void RefreshStatus()
        {
            var total_cnt = m_urls_set.Count;
            var left_cnt = m_url_list.Count;
            m_status = $"url info: left:{left_cnt,8} total: {total_cnt,8}";
        }

        void RefreshLog()
        {
            if (m_refresh_log != null)
                m_refresh_log();
        }

        void LogToLastLine(string msg)
        {
            if(m_log_list.Count > 0)
            {
                m_log_list.First.Value += " " + msg;
            }
            else
            {
                m_log_list.AddFirst(msg);
            }
            RefreshLog();
        }

        public void StartDownload(string web_root, string save_dir, bool force_download)
        {
            AbortDownload();
            m_save_dir = save_dir;
            m_log_list.Clear();
            m_force_download = force_download;

            web_root = GetAbsoluteUrlPath(web_root);
            string start_url = web_root;
            // 处理下web root, 支持有限制，每个url必须是文本，而不是目录。结尾需要是 *.*
            if (web_root.EndsWith("/"))
            {
                start_url += "index.html";
                m_web_root = web_root;
            }
            else
            {
                int idx = web_root.LastIndexOf('/');
                if (idx < 0)
                {
                    Log("error", "url is error");
                    return;
                }
                var name = web_root.Substring(idx + 1);
                m_web_root = web_root.Substring(0, idx + 1);
                if (name.LastIndexOf('.') < 1)
                {
                    if (name == "index")
                    {
                        
                        start_url += ".html";
                    }
                    else
                    {
                        start_url += "/index.html";
                    }
                }
                else
                {
                    start_url = web_root;
                }
            }
            // 开线程下载
            m_thread = new Thread(() =>
            {
                DownLoad(start_url);
            });

            m_thread.Start();
        }

        public void AbortDownload()
        {
            if (m_thread != null)
            {
                if (m_thread.IsAlive) m_thread.Abort();
                m_thread = null;
            }
        }

        public bool IsWorking()
        {
            return m_thread != null && m_thread.IsAlive;
        }

        #region Html
        enum UrlType
        {
            Other,// 下载了事不做处理
            Txt,// 
            Html,
            Js,
            Css,
            Img,
        }
        class UrlInfo
        {
            public UrlType type;
            public string url;
            public UrlInfo(UrlType type, string url)
            {
                this.type = type;
                this.url = url;
            }
        }

        Queue<UrlInfo> m_url_list = new Queue<UrlInfo>();
        bool m_force_download = false;

        void DownLoad(string start_url)
        {
            Directory.CreateDirectory(m_save_dir);
            m_url_list.Clear();
            m_urls_set.Clear();

            AddUrl(start_url);

            Log("start:",$"download website {start_url}");

            while (m_url_list.Count > 0)
            {
                var item = m_url_list.Dequeue();
                var url = item.url;
                try
                {
                    //var url_path = GetAbsoluteUrlPath(url);
                    Log("down=>", url);

                    var save_file = Path.Combine(m_save_dir, GetUrlPath(url).Replace(m_web_root, ""));
                    bool need_down = m_force_download || File.Exists(save_file) == false;
                    if (need_down)
                    {
                        var reuslt = XHttp.GetData(url);
                        if (reuslt.type == XHttp.ResponceType.Html)
                        {
                            // 特殊处理，提取url，并且修改url
                            HandleHtml(reuslt.text, url);
                            LogToLastLine("OK");
                        }
                        else if (reuslt.bytes != null)
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(save_file));
                            File.WriteAllBytes(save_file, reuslt.bytes);
                            LogToLastLine("OK");
                        }
                        else
                        {
                            LogToLastLine("【Error】");
                        }
                    }
                    else
                    {
                        if (save_file.EndsWith(".html") == true)
                        {
                            HandleLocalHtml(save_file, url);
                        }
                        LogToLastLine("cached");
                    }
                }
                catch(Exception e)
                {
                    Log("exception", e.Message);
                }
            }
            Log("finish", "Sucess");
        }


        // xpath语法 http://www.w3school.com.cn/xpath/xpath_syntax.asp
        void HandleHtml(string html, string url)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var url_dir = GetUrlDir(url);
            
            HandleHtmlHrefs(doc, url_dir, "//link[@type='text/css']", "href");
            HandleHtmlHrefs(doc, url_dir, "//script[@type='text/javascript']", "src");
            HandleHtmlHrefs(doc, url_dir, "//a", "href");

            // save html
            var save_file = Path.Combine(m_save_dir, GetUrlPath(url).Replace(m_web_root, ""));
            Directory.CreateDirectory(Path.GetDirectoryName(save_file));
            doc.Save(save_file);
        }

        void HandleLocalHtml(string file, string url)
        {
            var doc = new HtmlDocument();
            doc.Load(file);

            var url_dir = GetUrlDir(url);

            HandleHtmlHrefs(doc, url_dir, "//link[@type='text/css']", "href");
            HandleHtmlHrefs(doc, url_dir, "//script[@type='text/javascript']", "src");
            HandleHtmlHrefs(doc, url_dir, "//a", "href");

            // save html
            var save_file = Path.Combine(m_save_dir, GetUrlPath(url).Replace(m_web_root, ""));
            Directory.CreateDirectory(Path.GetDirectoryName(save_file));
            doc.Save(save_file);
        }

        void HandleHtmlHrefs(HtmlDocument doc, string c_url_dir,string xpath, string attr_name)
        {
            var nodes = doc.DocumentNode.SelectNodes($"{xpath}[@{attr_name}]");
            foreach(var node in nodes)
            {
                var attr = node.Attributes[attr_name];
                var href = attr.Value;
                try
                {
                    string _url = "";
                    string _href = "";
                    FormatHref(href, c_url_dir, out _url, out _href);
                    if(string.IsNullOrEmpty(_url) == false)
                    {
                        AddUrl(_url);
                    }
                    attr.Value = _href;
                }
                catch(Exception e)
                {
                    Log("exception", $"HandleHtmlHref error {href}");
                }
            }
        }

        void AddUrl(string url)
        {
            if (url == m_web_root) return;
            var name = GetUrlFileName(url);
            if (name.IndexOf('.') < 0) return;
            var url_path = GetAbsoluteUrlPath(url);
            if (m_urls_set.Contains(url_path) == false)
            {
                m_url_list.Enqueue(new UrlInfo(UrlType.Html, url));
                m_urls_set.Add(url_path);
                RefreshStatus();
            }
        }

        void FormatHref(string href, string c_url_dir, out string _url, out string _href)
        {
            if (href.Contains("//"))
            {
                _url = href;
                // 绝对地址
                if (href.StartsWith(c_url_dir))
                {
                    _href = href.Substring(c_url_dir.Length);
                }
                else if (href.StartsWith(m_web_root))
                {
                    var t1 = href.Substring(m_web_root.Length);
                    var t2 = c_url_dir.Substring(m_web_root.Length);
                    while (t2.LastIndexOf('/') >= 0)// 应该只要 > 就行
                    {
                        t1 = "../" + t1;
                        t2 = t2.Substring(0, t2.LastIndexOf('/'));
                    }
                    _href = t1;
                }
                else
                {
                    _url = null; // 不下载
                    _href = href;
                }

                if(string.IsNullOrWhiteSpace(_href) || _href.StartsWith("#"))
                {
                    _url = null;
                }
            }
            else
            {
                _href = href;
                Regex reg = new Regex(@"^\w{1,10}:");
                // 已经是相对地址了，
                if (string.IsNullOrWhiteSpace(href) || href.StartsWith("#") || reg.IsMatch(href))
                {
                    _url = null;
                }
                else
                {
                    _url = c_url_dir + href;
                }
            }
        }

        static string GetUrlDir(string url)
        {
            url = GetUrlPath(url);
            int idx = url.LastIndexOf('/');
            if (idx >= 0)
            {
                return url.Substring(0, idx+1);
            }
            else
            {
                return null;
            }
        }

        static string GetUrlFileName(string url)
        {
            url = GetUrlPath(url);
            int idx = url.LastIndexOf('/');
            if(idx >= 0)
            {
                return url.Substring(idx + 1);
            }
            else
            {
                return null;
            }
        }

        static string GetUrlPath(string url)
        {
            if (url.IndexOf('#') > 0) url = url.Substring(0, url.IndexOf('#'));
            if (url.IndexOf('?') > 0) url = url.Substring(0, url.IndexOf('?'));
            return url;
        }

        static string GetAbsoluteUrlPath(string url)
        {
            Uri info = new Uri(url);
            return GetUrlPath(info.AbsoluteUri);
        }

        #endregion Html
    }
}
