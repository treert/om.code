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
        public string m_web_root_file_dir = "";
        public string m_save_dir = "";
        public LinkedList<string> m_log_list = new LinkedList<string>();
        public HashSet<string> m_urls_set = new HashSet<string>();
        public string m_status = "";
        public int m_thread_cnt = 0;
        int m_thread_limit = 1;
        int m_error_url_cnt = 0;
        int m_waring_cnt = 0;
        public bool m_bao_shou_mode = false;

        void Warn(string msg)
        {
            m_waring_cnt++;
            Log("warn", msg);
        }

        void Error(string msg)
        {
            m_error_url_cnt++;
            Log("error", msg);
        }

        void Exception(string msg)
        {
            m_error_url_cnt++;
            Log("exception", msg);
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
                stop_finish = m_thread_cnt == 0 && exit;// 最后一个线程退出时刷新状态。
                var total_cnt = m_urls_set.Count;
                var left_cnt = m_xurl_list.Count;
                m_status = $" left:{left_cnt,-7} total:{total_cnt,-7} Thread:{m_thread_cnt,-3} error:{m_error_url_cnt,-7} warn:{m_waring_cnt, -7}";
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

            // webclient超时问题，连接数不够
            // > https://www.cnblogs.com/i80386/archive/2013/01/11/2856490.html
            ServicePointManager.DefaultConnectionLimit = Math.Max(thread_limit*2 + 3, ServicePointManager.DefaultConnectionLimit);

            m_xurl_list.Clear();
            m_urls_set.Clear();
            m_real_url_dic.Clear();

            m_thread_cnt = 0;
            m_error_url_cnt = 0;
            m_waring_cnt = 0;
            m_thread_limit = thread_limit;
            m_save_dir = save_dir;
            m_log_list.Clear();
            m_force_download = force_download;

            m_web_root_url_dir = "";
            m_web_root_file_dir = "";
            var xurl = _GetRealXUrl(start_url);
            if(xurl == null || xurl.m_is_html == false)
            {
                Log("error", $"{start_url} does not support, must start with 'http://' or 'https://' and end with '/' or '.html'");
                return;
            }

            m_web_root_file_dir = xurl.m_file_dir;
            m_web_root_url_dir = xurl.m_url_dir;

            AddUrl(xurl);

            Log("start:", $"download website {xurl.m_origin_url}");
            // 开线程下载
            m_stoped = false;
            TryAddTheadToDownLoad();
        }

        bool m_stoped = true;
        public void AbortDownload(bool quite = true)
        {
            //if (m_stoped == false)
            //{
            //    if(quite == false) Log("stop", "wait for thread to exit");
            //    m_stoped = true;
            //}
            //bool valid = false;
            //lock (this)
            //{
            //    if (m_stoped == false)
            //    {
            //        valid = true;
            //        m_stoped = true;
            //    }
            //}
            //if (valid) { Log("stop", "wait for thread to exit"); }

            // 粗暴打断
            lock (this)
            {
                foreach (var thread in m_thread_list)
                {
                    if (thread.IsAlive) thread.Abort();
                }
                m_thread_list.Clear();
            }
            m_thread_cnt = 0;
            RefreshStatus();
        }

        public bool IsWorking()
        {
            return m_thread_cnt > 0;
            //lock (this)
            //{
            //    foreach (var th in m_thread_list)
            //    {
            //        if (th.IsAlive) return true;
            //    }
            //    return false;
            //}
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

        Queue<XUrl> m_xurl_list = new Queue<XUrl>();
        
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
                var cnt = m_xurl_list.Count;
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

            XWebClient client = new XWebClient();
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
                var xurl = GetUrl();
                RefreshStatus();
                if (xurl == null) break;
                try
                {
                    //var url_path = GetAbsoluteUrlPath(url);
                    string status_str = "[?]";
                    var save_file = GetSaveFileNameFromUrl(xurl);
                    bool need_down = m_force_download || File.Exists(save_file) == false;
                    if (need_down)
                    {
                        var result = client.GetData(xurl.m_url_full);
                        if(result.error != null)
                        {
                            status_str = $"【Error:{result.error}";
                            m_error_url_cnt++;
                        }
                        else if (result.type == XWebResult.XContentType.Html)
                        {
                            // 特殊处理，提取url，并且修改url
                            HandleHtml(result.text, xurl);
                            status_str = "OK";
                        }
                        else if (result.type == XWebResult.XContentType.Css)
                        {
                            // 特殊处理，提取url，并修改url
                            var content = HandleCss(result.text, xurl);

                            if (m_stoped) throw new Exception("Has Be Stoped, dont save css");

                            Directory.CreateDirectory(Path.GetDirectoryName(save_file));
                            File.WriteAllText(save_file, content);
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
                        if (save_file.EndsWith(".html") == true)
                        {
                            HandleLocalHtml(save_file, xurl);
                        }
                        else if(save_file.EndsWith(".css") == true)
                        {
                            HandleLocalCssFile(save_file, xurl);
                        }
                        status_str = "cached";
                    }
                    Log("down=>", $"{xurl.m_url_full} {status_str}");
                    TryAddTheadToDownLoad();
                }
                catch (Exception e)
                {
                    Log("exception", $"{xurl.m_url_full} msg:{e.Message}");
                    m_error_url_cnt++;
                }
            }
            lock (this) { m_thread_cnt--; }
            client.Dispose();
            RefreshStatus(true);
        }

        string _GetFullUrl(XUrl xurl_root, string url, bool from_file = false)
        {
            if (url.StartsWith("//")) return xurl_root.m_uri.Scheme + ':' + url;
            else if (url.StartsWith("/")) return xurl_root.m_url_host + url;
            else if (XUrl.IsSchemeUrl(url)) return url;
            else if (url.StartsWith("#")) return xurl_root.m_url_path + url;
            else
            {
                if (from_file)
                    return xurl_root.m_url_dir + XUrl.ConvertPathToUrlFrag(url);
                else
                    return xurl_root.m_url_dir + url;
            }
        }

        Dictionary<string, string> m_real_url_dic = new Dictionary<string, string>();
        object m_lock_get_real_url = new object();
        XUrl _GetRealXUrl(string full_url)
        {
            var xurl = XUrl.TryParser(full_url);
            if (xurl == null) return null;

            // 做些限制
            if (xurl.m_has_query) return null;

            // 保守处理，不在根目录下都不下载
            if (m_web_root_url_dir != "" && xurl.m_url_full.StartsWith(m_web_root_url_dir) == false) return null;

            if (m_bao_shou_mode == false)
            {
                return xurl;// 不是保守模式，就不要去真的获取真实地址了。
            }


            string url_without_fragment = xurl.m_url_path;
            string fragment = xurl.m_uri.Fragment;

            //if (fragment != "")
            //{
            //    url_without_fragment = full_url.Substring(0, full_url.IndexOf('#'));// 不添加后缀的。
            //}

            bool aready_handle = false;
            bool error = false;
            string ret = null;
            lock (m_lock_get_real_url) {
                aready_handle = m_real_url_dic.TryGetValue(url_without_fragment, out ret);

                if (aready_handle == false)
                {
                    ret = XWebClient.GetHttpResponseUrl(url_without_fragment);// cost time, but need wait here or wait for lock
                    if (ret == null) error = true;
                    m_real_url_dic.Add(url_without_fragment, ret);
                }
            };

            if (ret != null)
            {
                ret += fragment;
                return XUrl.TryParser(ret);
            }
            else
            {
                if(error) {
                    Log("warning", "GetHeader Fail url: " + url_without_fragment);
                }
                return null;
            }
            
        }

        string _GetRelateUrl(XUrl xurl_root, XUrl xurl)
        {
            // 能成功转相对url，就是打算下载的，这个做个通用限制
            // 1. 不能有query
            if (xurl.m_has_query) return null;

            // 保守处理，不在根目录下都不下载
            if (xurl.m_url_full.StartsWith(m_web_root_url_dir) == false) return null;

            if (xurl.m_url_full == xurl_root.m_url_full) return "#";

            string href = null;
            if (xurl.m_file_path.StartsWith(xurl_root.m_file_dir))
            {
                href = xurl.m_file_path.Substring(xurl_root.m_file_dir.Length);
            }
            else
            {
                var t1 = xurl.m_file_path.Substring(m_web_root_file_dir.Length);
                var t2 = xurl_root.m_file_path.Substring(m_web_root_file_dir.Length);
                while (t2.LastIndexOf('/') > 0)
                {
                    t1 = "../" + t1;
                    t2 = t2.Substring(0, t2.LastIndexOf('/'));
                }
                href = t1;
            }
            href += xurl.m_uri.Fragment;
            return href;
        }

        [ThreadStatic]
        static Regex m_reg_css_url = new Regex(@"\burl\((\S*)\)");
        string HandleCss(string content, XUrl xurl_root, bool from_file = false)
        {
            if(m_reg_css_url == null) m_reg_css_url = new Regex(@"\burl\((\S*)\)");

            StringBuilder sb = new StringBuilder();
            var matches = m_reg_css_url.Matches(content);
            List<Match> list = new List<Match>(matches.Count);
            foreach(Match match in matches)
            {
                list.Add(match);
            }

            list.Sort((a, b)=>{ return a.Groups[0].Index - b.Groups[0].Index; });

            Func<string, string> get_full_url = (url) =>
            {
                return _GetFullUrl(xurl_root, url, from_file);
            };
            
            Func<string, string> downloader = (url) =>
            {
                var full = get_full_url(url);
                var xurl = _GetRealXUrl(full);
                if (xurl == null) return full;
                var relate = _GetRelateUrl(xurl_root, xurl);
                if (relate == null) return xurl.m_origin_url;

                AddUrl(xurl);
                return relate;
            };


            int last_idx = 0;
            foreach(Match match in list)
            {
                var cap = match.Groups[1];
                var href = cap.Value;
                href = href.Trim().Trim('"', '\'');

                sb.Append(content, last_idx, cap.Index - last_idx);
                var url = downloader(href);
                sb.Append($"'{url}'");
                last_idx = cap.Index + cap.Length;
            }
            sb.Append(content, last_idx, content.Length - last_idx);

            return sb.ToString();
        }

        string HandleLocalCssFile(string file, XUrl xurl_root)
        {
            var content = File.ReadAllText(file);
            return HandleCss(content, xurl_root, true);
        }


        void HandleHtml(HtmlDocument doc, XUrl xurl_root, bool from_file = false)
        {
            {
                // base url 不支持
                var node = doc.DocumentNode.SelectSingleNode("/html/head/base[@href]");
                if (node != null) return;
            }
            // > https://www.w3schools.com/tags/ref_attributes.asp
            // 1. 优先下载: link[@href],script[@src],img[@src],input[@type='image'][@src],img[@srcset] 
            // 2. 转化成绝对url: audio[@src],embed[@src],video[@src],source[@src],source[@srcset],track[@src] 
            // 3. 检查html链接：a[@href],area[@href],iframe[@src]

            Func<string, string> get_full_url = (url) =>
            {
                return _GetFullUrl(xurl_root, url);
            };
            
            Func<string, string> downloader = (url) =>
            {
                var full = get_full_url(url);
                var xurl = _GetRealXUrl(full);
                if (xurl == null) return full;
                var relate = _GetRelateUrl(xurl_root, xurl);
                if (relate == null) return xurl.m_origin_url;

                AddUrl(xurl);
                return relate;
            };

            Func<string, string> handle_html = (url) =>
            {
                var full = get_full_url(url);
                var xurl = _GetRealXUrl(full);
                if (xurl == null || xurl.m_is_html == false) return full;// 必须是html
                var relate = _GetRelateUrl(xurl_root, xurl);
                if (relate == null) return xurl.m_origin_url;

                AddUrl(xurl);
                return relate;
            };

            EnumUrls(doc, "//link", "href", downloader);
            EnumUrls(doc, "//script", "src", downloader);
            EnumUrls(doc, "//img", "src", downloader);
            EnumUrls(doc, "//input[@type='image']", "src", downloader);
            EnumUrls(doc, "//img", "srcset", downloader);

            EnumUrls(doc, "//audio", "src", get_full_url);
            EnumUrls(doc, "//embed", "src", get_full_url);
            EnumUrls(doc, "//video", "src", get_full_url);
            EnumUrls(doc, "//source", "src", get_full_url);
            EnumUrls(doc, "//source", "srcset", get_full_url);
            EnumUrls(doc, "//track", "src", get_full_url);

            EnumUrls(doc, "//a", "href", handle_html);
            EnumUrls(doc, "//area", "href", handle_html);
            EnumUrls(doc, "//iframe", "src", handle_html);
        }


        void HandleHtml(string html, XUrl xurl)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            HandleHtml(doc, xurl);

            if (m_stoped) throw new Exception("Has Be Stoped, dont save html");

            // save html
            var save_file = GetSaveFileNameFromUrl(xurl);
            Directory.CreateDirectory(Path.GetDirectoryName(save_file));
            doc.Save(save_file);
        }

        void HandleLocalHtml(string file, XUrl xurl)
        {
            var doc = new HtmlDocument();
            doc.Load(file);

            HandleHtml(doc, xurl, true);
        }

        string GetSaveFileNameFromUrl(XUrl xurl)
        {
            var file = Path.Combine(m_save_dir, xurl.m_file_path.Substring(m_web_root_file_dir.Length));
            return file;
        }

        void EnumUrls(HtmlDocument doc, string xpath, string attr_name, Func<string,string> handler)
        {
            var nodes = doc.DocumentNode.SelectNodes($"{xpath}[@{attr_name}]");
            if (nodes == null) return;
            foreach(var node in nodes)
            {
                if (m_stoped) break;
                var attr = node.Attributes[attr_name];
                var url = attr.Value;
                try
                {
                    attr.Value = handler(url);
                }
                catch (Exception e)
                {
                    Log("exception", $"EnumUrls handler href:{url} error:{e.StackTrace} ");
                }
            }
        }

        XUrl GetUrl()
        {
            lock (this)
            {
                if(m_stoped == false && m_xurl_list.Count > 0)
                {
                    return m_xurl_list.Dequeue();
                }
                else
                {
                    return null;
                }
            }
        }
        void AddUrl(XUrl xurl)
        {
            bool changed = false;
            lock (this)
            {
                if (m_urls_set.Contains(xurl.m_url_path) == false)
                {
                    m_xurl_list.Enqueue(xurl);
                    m_urls_set.Add(xurl.m_url_path);
                    changed = true;
                }
            }
            if(changed) RefreshStatus();
        }

        #endregion Html
    }
}
