using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using System.Text.RegularExpressions;
using System.Net;

namespace DownloadWebsite
{
    class Worker
    {
        public static Worker singleton = new Worker();

        public event Action<string> m_add_log;
        public event Action m_refresh_status;

        public string m_web_root_url_dir = "";
        public string m_web_host = "";
        public string m_web_root_file_dir = "";
        public string m_save_dir = "";
        public LinkedList<string> m_log_list = new LinkedList<string>();
        public HashSet<string> m_urls_set = new HashSet<string>();
        public string m_status = "";
        public int m_thread_cnt = 0;
        int m_thread_limit = 1;
        int m_error_url_cnt = 0;

        LinkedListNode<string> Log(string tag, string msg)
        {
            var log = $"{DateTime.Now.ToString("T")} {tag,-10} {msg}";
            LinkedListNode<string> node = null;
            //lock (this)
            //{
                
            //    node = m_log_list.AddFirst(log);
            //}
            AddLogToUI(log);
            return node;
        }

        void RefreshStatus(bool exit = false)
        {
            bool stop_finish = false;
            lock (this)
            {
                stop_finish = m_thread_cnt == 0 && exit;// 最后一个线程退出时刷新状态。
                var total_cnt = m_urls_set.Count;
                var left_cnt = m_url_list.Count;
                m_status = $" left:{left_cnt,-7} error:{m_error_url_cnt,-7} total:{total_cnt,-7} Thread:{m_thread_cnt,-3}";
            }
            if (m_refresh_status != null) m_refresh_status();
            if (stop_finish)
            {
                if (m_stoped)
                    Log("stop", "all thread stoped");
                else
                    Log("finish", "Success");
            }
        }

        void AddLogToUI(string msg)
        {
            if (m_add_log != null)
                m_add_log(msg);
        }

        //void LogToLastLine(LinkedListNode<string> node, string msg)
        //{
        //    lock (this)
        //    {
        //        node.Value += " " + msg;
        //    }
        //    RefreshLog();
        //}
        bool m_proxy_use = false;
        string m_proxy_host;
        int m_proxy_port;
        string m_proxy_user;
        string m_proxy_pwd;
        public void SetProxyInfo(bool use, string host = null, int port = 0, string user = null, string pwd = null)
        {
            m_proxy_use = use;
            m_proxy_host = host;
            m_proxy_port = port;
            m_proxy_user = user;
            m_proxy_pwd = pwd;
            if (string.IsNullOrEmpty(m_proxy_host) || port <= 0) m_proxy_use = false;
        }

        public void StartDownload(string start_url, string save_dir, bool force_download, int thread_limit)
        {
            AbortDownload(quite: false);
            m_error_url_cnt = 0;
            m_thread_limit = thread_limit;
            m_save_dir = save_dir;
            m_log_list.Clear();
            m_force_download = force_download;

            //web_root = GetAbsoluteUrlPath(web_root);
            try
            {
                start_url = XHttp.GetUrlPathFromUrl(start_url);
                m_web_host = XHttp.GetWebHostFromUrl(start_url);
                m_web_root_file_dir = XHttp.GetFileDirFromUrl(start_url);
                m_web_root_url_dir = XHttp.GetUrlDirFromUrl(start_url);
            }
            catch(Exception)
            {
                Log("error", $"url format error {start_url}, do not forget to add 'http://' header");
                return;
            }

            m_url_list.Clear();
            m_urls_set.Clear();

            AddUrl(start_url);

            Log("start:", $"download website {start_url}");
            // 开线程下载
            m_stoped = false;
            TryAddTheadToDownLoad();
        }

        bool m_stoped = true;
        public void AbortDownload(bool quite = true)
        {
            bool valid = false;
            lock (this)
            {
                if (m_stoped == false)
                {
                    valid = true;
                    m_stoped = true;
                }
            }
            if (valid) { Log("stop", "wait for thread to exit"); }
            
            // 粗暴打断
            //foreach (var thread in m_thread_list)
            //{
            //    if (thread.IsAlive) thread.Abort();
            //}
            //m_thread_list.Clear();
        }

        public bool IsWorking()
        {
            //return m_thread_cnt > 0;
            lock (this)
            {
                foreach (var th in m_thread_list)
                {
                    if (th.IsAlive) return true;
                }
                return false;
            }
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
        LinkedList<Thread> m_thread_list = new LinkedList<Thread>();

        void TryAddTheadToDownLoad()
        {
            bool added = false;
            lock (this)
            {
                if (m_stoped)
                {
                    return;
                }
                var cnt = m_url_list.Count;
                //var limit = Math.Log(cnt, 2) + 4;
                int limit = m_thread_limit;
                int cur = 0;
                var itor = m_thread_list.First;
                while(itor != null)
                {
                    if (itor.Value.IsAlive)
                    {
                        cur++;
                        itor = itor.Next;
                    }
                    else
                    {
                        var next = itor.Next;
                        m_thread_list.Remove(itor);
                        itor = next;
                    }
                }
                //m_thread_cnt = cur;
                if(cnt > 0 && cur < limit)
                {
                    var thread = new Thread(() => { ThreadDownload(); });
                    thread.IsBackground = true;
                    m_thread_list.AddLast(thread);
                    added = true;
                    thread.Start();
                }
            }
            if(added) RefreshStatus();
        }

        void ThreadDownload()
        {
            lock (this) { m_thread_cnt++; }

            WebProxy proxy = null;
            if (m_proxy_use)
            {
                try
                {
                    proxy = XWebClient.CreateWebProxy(m_proxy_host, m_proxy_port, m_proxy_user, m_proxy_pwd);
                }
                catch(Exception e)
                {
                    Log("error", $"创建代理失败： {e.Message}");
                }
                
            }
            
            while (true)
            {
                var url = GetUrl();
                RefreshStatus();
                if (url == null) break;
                try
                {
                    //var url_path = GetAbsoluteUrlPath(url);
                    string status_str = "[?]";
                    var save_file = GetSaveFileNameFromUrl(url);
                    bool need_down = m_force_download || File.Exists(save_file) == false;
                    if (need_down)
                    {
                        var result = XHttp.GetData(url, proxy);
                        if(result.error != null)
                        {
                            status_str = $"【Error:{result.error}";
                            m_error_url_cnt++;
                        }
                        else if (result.type == XHttp.ResponceType.Html)
                        {
                            // 特殊处理，提取url，并且修改url
                            HandleHtml(result.text, url);
                            status_str = "OK";
                        }
                        else if (result.type == XHttp.ResponceType.Css)
                        {
                            HandleCss(result.text, url);
                            Directory.CreateDirectory(Path.GetDirectoryName(save_file));
                            File.WriteAllBytes(save_file, result.bytes);
                            status_str = "OK";
                        }
                        else
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(save_file));
                            File.WriteAllBytes(save_file, result.bytes);
                            status_str = "OK";
                        }
                    }
                    else
                    {
                        if (save_file.EndsWith(".html") == true || save_file.EndsWith(".htm") == true)
                        {
                            HandleLocalHtml(save_file, url);
                        }
                        else if(save_file.EndsWith(".css") == true)
                        {
                            HandleCssFile(save_file, url);
                        }
                        status_str = "cached";
                    }
                    Log("down=>", $"{url} {status_str}");
                    TryAddTheadToDownLoad();
                }
                catch (Exception e)
                {
                    Log("exception", $"{url} {e.Message}");
                    m_error_url_cnt++;
                }
            }
            lock (this) { m_thread_cnt--; }
            RefreshStatus(true);
        }

        Regex m_reg_css_url = new Regex(@"\s+url\((\S*)\)");
        Regex m_reg_data_url = new Regex(@"^\w{1,20}:");
        void HandleCss(string content, string url)
        {
            var url_dir = XHttp.GetUrlDirFromUrl(url);
            var matches = m_reg_css_url.Matches(content);
            foreach(Match match in matches)
            {
                var href = match.Groups[1].Value;
                href = href.Trim().Trim('"', '\'');
                if(string.IsNullOrEmpty(href) || href.Contains("//") || m_reg_data_url.IsMatch(href))
                {

                }
                else if (href.StartsWith("/"))
                {
                    AddUrl(m_web_host + href);
                }
                else
                {
                    AddUrl(url_dir + href);
                }
            }
        }

        void HandleCssFile(string file, string url)
        {
            var content = File.ReadAllText(file);
            HandleCss(content, url);
        }


        // xpath语法 http://www.w3school.com.cn/xpath/xpath_syntax.asp
        void HandleHtml(string html, string url)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            HandleLocalHtml(doc, url);
        }

        void HandleLocalHtml(string file, string url)
        {
            var doc = new HtmlDocument();
            doc.Load(file);

            HandleLocalHtml(doc, url);
        }

        void HandleLocalHtml(HtmlDocument doc, string url)
        {
            var url_dir = XHttp.GetUrlDirFromUrl(url);

            HandleHtmlHrefs(doc, url_dir, "//link[@type='text/css']", "href");
            HandleHtmlHrefs(doc, url_dir, "//script[@type='text/javascript']", "src");
            HandleHtmlHrefs(doc, url_dir, "//a", "href");
            HandleHtmlHrefs(doc, url_dir, "//img", "src");

            // save html
            var save_file = GetSaveFileNameFromUrl(url);
            Directory.CreateDirectory(Path.GetDirectoryName(save_file));
            doc.Save(save_file);
        }

        string GetSaveFileNameFromUrl(string url)
        {
            var file = Path.Combine(m_save_dir, XHttp.GetFilePathFromUrl(url).Replace(m_web_root_file_dir, ""));
            return file;
        }


        void HandleHtmlHrefs(HtmlDocument doc, string c_url_dir,string xpath, string attr_name)
        {
            var nodes = doc.DocumentNode.SelectNodes($"{xpath}[@{attr_name}]");
            if (nodes == null) return;
            foreach(var node in nodes)
            {
                if (m_stoped) break;
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
                    Log("exception", $"HandleHtmlHref error:{e.Message} href:{href}");
                }
            }
        }
        string GetUrl()
        {
            lock (this)
            {
                if(m_stoped == false && m_url_list.Count > 0)
                {
                    return m_url_list.Dequeue().url;
                }
                else
                {
                    return null;
                }
            }
        }
        void AddUrl(string url)
        {
            var url_path = XHttp.GetFilePathFromUrl(url);

            bool changed = false;
            lock (this)
            {
                if (m_urls_set.Contains(url_path) == false)
                {
                    m_url_list.Enqueue(new UrlInfo(UrlType.Html, url));
                    m_urls_set.Add(url_path);
                    changed = true;
                }
            }
            if(changed) RefreshStatus();
        }

        void FormatHref(string href, string c_url_dir, out string _url, out string _href)
        {
            if (href.StartsWith("/"))
            {
                href = m_web_host + href;
            }

            if (href.Contains("//"))
            {
                _url = href;
                // 绝对地址
                if (href.StartsWith(c_url_dir))
                {
                    _href = href.Substring(c_url_dir.Length);
                }
                else if (href.StartsWith(m_web_root_url_dir))
                {
                    var t1 = href.Substring(m_web_root_url_dir.Length);
                    var t2 = c_url_dir.Substring(m_web_root_url_dir.Length);
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

        static string GetUrlPath(string url)
        {
            if (url.IndexOf('#') > 0) url = url.Substring(0, url.IndexOf('#'));
            if (url.IndexOf('?') > 0) url = url.Substring(0, url.IndexOf('?'));
            return url;
        }

        #endregion Html
    }
}
