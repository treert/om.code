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

        public event Action<string> m_add_log;
        public event Action m_refresh_status;

        public string m_web_root = "";
        public string m_save_dir = "";
        public LinkedList<string> m_log_list = new LinkedList<string>();
        public HashSet<string> m_urls_set = new HashSet<string>();
        public string m_status = "";
        public int m_thread_cnt = 0;
        int m_thread_limit = 1;

        public void Init()
        {
            m_save_dir = Path.Combine(Environment.CurrentDirectory, "website");
            m_log_list.Clear();
        }

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
                stop_finish = m_stoped && m_thread_cnt == 0 && exit;// 最后一个线程退出时刷新状态。
                var total_cnt = m_urls_set.Count;
                var left_cnt = m_url_list.Count;
                m_status = $" left:{left_cnt,-7} total: {total_cnt,-7} Thread:{m_thread_cnt,-3}";
            }
            if (m_refresh_status != null) m_refresh_status();
            if (stop_finish) Log("stop", "all thread stoped");
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

        public void StartDownload(string web_root, string save_dir, bool force_download, int thread_limit)
        {
            AbortDownload();
            m_thread_limit = thread_limit;
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

            m_url_list.Clear();
            m_urls_set.Clear();

            AddUrl(start_url);

            Log("start:", $"download website {start_url}");
            // 开线程下载
            m_stoped = false;
            TryAddTheadToDownLoad();
        }

        bool m_stoped = true;
        public void AbortDownload()
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
            bool finished = false;
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
                if (cur == 1 && cnt == 0)
                {
                    finished = true;
                    return;
                }
                if(cur < limit)
                {
                    var thread = new Thread(() => { ThreadDownload(); });
                    thread.IsBackground = true;
                    m_thread_list.AddLast(thread);
                    added = true;
                    thread.Start();
                }
            }
            if(finished) Log("finish", "Success");
            if(added) RefreshStatus();
        }

        void ThreadDownload()
        {
            lock (this) { m_thread_cnt++; }
            
            while (true)
            {
                var url = GetUrl();
                if (url == null) break;
                try
                {
                    //var url_path = GetAbsoluteUrlPath(url);
                    string status_str = "[?]";
                    var save_file = Path.Combine(m_save_dir, GetUrlPath(url).Replace(m_web_root, ""));
                    bool need_down = m_force_download || File.Exists(save_file) == false;
                    if (need_down)
                    {
                        var reuslt = XHttp.GetData(url);
                        if(reuslt.error != null)
                        {
                            status_str = $"【Error:{reuslt.error}";
                        }
                        else if (reuslt.type == XHttp.ResponceType.Html)
                        {
                            // 特殊处理，提取url，并且修改url
                            HandleHtml(reuslt.text, url);
                            status_str = "OK";
                        }
                        else
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(save_file));
                            File.WriteAllBytes(save_file, reuslt.bytes);
                            status_str = "OK";
                        }
                    }
                    else
                    {
                        if (save_file.EndsWith(".html") == true)
                        {
                            HandleLocalHtml(save_file, url);
                        }
                        status_str = "cached";
                    }
                    Log("down=>", $"{url} {status_str}");
                    TryAddTheadToDownLoad();
                }
                catch (Exception e)
                {
                    Log("exception", $"{url} {e.Message}");
                }
            }
            lock (this) { m_thread_cnt--; }
            RefreshStatus(true);
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
            if (url == m_web_root) return;
            var name = GetUrlFileName(url);
            if (name.IndexOf('.') < 0) return;
            var url_path = GetAbsoluteUrlPath(url);

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
