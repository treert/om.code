using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;

namespace XUtils
{
    /// <summary>
    /// http下载：
    /// 1. 多线程异步批量下载并保存文件
    ///     - 支持断点续传，需要设置hash值才支持
    ///     - 在提供size和hash时，提供校验功能。
    /// </summary>
    public class XHttpBatchDownLoader
    {
        // 方便使用，
        public static readonly XHttpBatchDownLoader instance = new XHttpBatchDownLoader();

        public enum ItemStatus
        {
            NotStart,
            Started,
            Finished,
            Error,
        }

        public class Item
        {
            public string url;
            public string path;
            public long size = -1;
            public string hash;// 设置了hash值，才支持断点续传，不然重新下载
            public ItemStatus status = ItemStatus.NotStart;
            public string error;// 如果出错，错误原因
        }

        static int OneReadLen = 16384;           // 一次读取长度 16384 = 16*kb
        static int ReadWriteTimeOut = 2 * 1000;  // 超时等待时间
        static int TimeOutWait = 5 * 1000;       // 超时等待时间
        static int AsyncThreadLimit = 5;

        public static void InitStaticConfig()
        {
            ServicePointManager.DefaultConnectionLimit = 100;
            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
        }
        static bool MyRemoteCertificateValidationCallback(System.Object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            // 没找到好的证书校验的讲解，直接返回true，不校验了。
            return true;
        }

        string m_hash_func_name = "sha1";
        int m_hash_result_size = 40;

        public string HashFuncName
        {
            get
            {
                return m_hash_func_name;
            }
            set
            {
                var hash_func_name = value;
                using (var hash = HashAlgorithm.Create(hash_func_name))// 如果不对会抛异常
                {
                    m_hash_func_name = hash_func_name;
                    m_hash_result_size = hash.HashSize / 4;
                }
            }
        }

        public void Reset()
        {
            CleanThread();
            //m_error_list.Clear();
            m_task_list.Clear();
        }

        public void CleanThread()
        {
            foreach (var th in m_threads)
            {
                try
                {
                    if (th.IsAlive) th.Abort();
                }
                catch (Exception)
                {
                }
            }
            m_threads.Clear();
        }

        List<Thread> m_threads = new List<Thread>();
        /// <summary>
        /// 是否已经结束下载。现在用的是下载进程全部结束来判断的。
        /// </summary>
        public bool IsDone
        {
            get
            {
                foreach (var th in m_threads)
                {
                    if (th.IsAlive) return false;
                }
                return true;
            }
        }

        Queue<Item> m_task_list = new Queue<Item>();
        long m_current_download_size = 0;
        //long m_total_need_download_size = 0;// 由于传入的Item.size允许不设置，故而这个值不准
        void AddDownloadSize(long size)
        {
            Interlocked.Add(ref m_current_download_size, size);
        }
        public long CurrentDownloadSize
        {
            get
            {
                return Interlocked.Read(ref m_current_download_size);
            }
        }

        Item TryGetTask()
        {
            lock (m_task_list)
            {
                if (m_task_list.Count > 0)
                {
                    return m_task_list.Dequeue();
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// 开启下载，同时只能开启一个，因为会Reset掉之前的
        /// </summary>
        /// <param name="list"></param>
        public void StartDownLoad(List<Item> list)
        {
            Reset();
            m_current_download_size = 0;
            foreach (var it in list)
            {
                if (it.hash != null)
                {
                    if (it.hash.Length != m_hash_result_size)
                    {
                        throw new Exception(m_hash_func_name + "'s HashSize must be " + m_hash_result_size);
                    }
                    else
                    {
                        it.hash = it.hash.ToUpper();// 预处理下，方便后续做比较
                    }
                }
                it.status = ItemStatus.NotStart;

                m_task_list.Enqueue(it);
            }
            for (int i = 0; i < AsyncThreadLimit; i++)
            {
                Thread th = new Thread(() =>
                {
                    _ThreadDownload();
                });
                th.Start();
                m_threads.Add(th);
            }
        }

        void _ThreadDownload()
        {
            using (HashAlgorithm hash_func = HashAlgorithm.Create(m_hash_func_name))
            {
                StringBuilder strB = new StringBuilder();
                while (true)
                {
                    var it = TryGetTask();
                    if (it == null) return;

                    it.status = ItemStatus.Started;
                    // 有hash值，可以校验下是否已经下载好了
                    if (it.hash != null && File.Exists(it.path))
                    {
                        using (var fs = File.OpenRead(it.path))
                        {
                            if (CheckFileOk(fs, hash_func, it))
                            {
                                // 当成下载好了
                                AddDownloadSize(fs.Length);
                                it.status = ItemStatus.Finished;
                                continue;
                            }
                        }
                    }
                    _ThreadDownload(it, hash_func);
                }
            }
        }
        /// <summary>
        /// 下载方法：下载到.temp临时文件，然后Move，
        /// - 注意的点
        ///     1. .temp支持断点续传
        ///     2. 如果.temp初始大小>0，hash验证失败，会重新完整下载一次
        ///     3. 如果没有hash校验，直接重新下载文件。
        /// - 致命错误
        ///     1. 如果下载完成，hash不对，就比较尴尬了
        ///     2. http返回的content-lenth 和 size不一致，也尴尬了
        /// </summary>
        /// <param name="it"></param>
        /// <param name="hash_func"></param>
        void _ThreadDownload(Item it, HashAlgorithm hash_func)
        {
            long download_size = 0;
            FileStream fs = null;
            try
            {
                if (it.hash == null)
                {
                    // 直接重新下载
                    var dir = Path.GetDirectoryName(it.path);
                    Directory.CreateDirectory(dir);
                    fs = File.Open(it.path, FileMode.Create, FileAccess.Write);
                    _WebDownload(it, fs, 0);
                    return;
                }

                string tmp_file = it.path + ".temp";
                long start_pos = 0;
                if (File.Exists(tmp_file))
                {
                    fs = File.Open(tmp_file, FileMode.Open, FileAccess.ReadWrite);
                    start_pos = fs.Length;
                    if (it.size > 0 && it.size < start_pos)// 大小不一致，可以认为已经凉了
                    {
                        fs.Close();
                        fs = null;
                    }
                }
                if (fs == null)
                {
                    start_pos = 0;
                    var dir = Path.GetDirectoryName(tmp_file);
                    Directory.CreateDirectory(dir);
                    fs = File.Open(tmp_file, FileMode.Create, FileAccess.ReadWrite);
                }

                _WebDownload(it, fs, start_pos);

                bool ok = CheckFileOk(fs, hash_func, it);
                if (!ok && start_pos > 0)
                {
                    // 可能有残留，再给一次机会
                    AddDownloadSize(fs.Length);
                    _WebDownload(it, fs, 0);
                    ok = CheckFileOk(fs, hash_func, it);
                }
                download_size = fs.Length;
                if (!ok)
                {
                    // 凉凉
                    throw new Exception("Fatal! Web file's size or hash does not match.");
                }

                fs.Close();
                fs = null;
                if (File.Exists(it.path)) File.Delete(it.path);
                File.Move(tmp_file, it.path);
                it.status = ItemStatus.Finished;
            }
            catch (Exception e)
            {
                AddDownloadSize(-download_size);
                it.status = ItemStatus.Error;
                it.error = e.Message;
            }
            finally
            {
                if (fs != null) fs.Dispose();
            }
        }

        // http下载文件，出错会抛异常
        void _WebDownload(Item it, FileStream fs, long start_pos)
        {
            long download_size = start_pos;
            AddDownloadSize(download_size);
            HttpWebRequest request = null;
            if (it.size > 0 && it.size == start_pos) return;
            try
            {
                //if(it.size > 0 && it.size < start_pos)
                //{
                //    throw new Exception("should not happened, start_pos > target.size");
                //}
                fs.Seek(start_pos, SeekOrigin.Begin);

                request = WebRequest.Create(it.url) as HttpWebRequest;
                request.ReadWriteTimeout = ReadWriteTimeOut;
                request.Timeout = TimeOutWait;
                if (start_pos > 0) request.AddRange((int)start_pos);

                using (var respone = (HttpWebResponse)request.GetResponse())
                using (var stream = respone.GetResponseStream())
                {
                    if (respone.ContentLength >= 0)
                    {
                        var web_size = respone.ContentLength + start_pos;
                        if (it.size > 0 && web_size != it.size)
                        {
                            throw new Exception("size not match http file " + web_size + " != " + it.size);
                        }
                    }
                    stream.ReadTimeout = TimeOutWait;

                    byte[] bytes = new byte[OneReadLen];
                    int len = stream.Read(bytes, 0, OneReadLen);
                    while (len > 0)
                    {
                        fs.Write(bytes, 0, len);
                        download_size += len;
                        AddDownloadSize(len);

                        len = stream.Read(bytes, 0, OneReadLen);
                    }
                }

            }
            catch (Exception)
            {
                AddDownloadSize(-download_size);// 回滚下载进度
                throw;
            }
            finally
            {
                if (request != null) request.Abort();
            }
        }

        static bool CheckFileOk(FileStream fs, HashAlgorithm hash_func, Item it)
        {
            if (it.size > 0 && it.size != fs.Length) return false;
            if (it.hash != null)
            {
                fs.Seek(0, SeekOrigin.Begin);
                var bytes = hash_func.ComputeHash(fs);
                var hash = BytesToHexString(bytes);
                return it.hash == hash;
            }
            return true;// 没有信息可以检测，既然文件存在，就当它OK
        }

        public static string BytesToHexString(byte[] bytes)
        {
            return BitConverter.ToString(bytes).Replace("-", string.Empty);
        }
    }
}
