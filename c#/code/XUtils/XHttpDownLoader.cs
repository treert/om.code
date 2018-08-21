/**
 * 创 建 者：treertzhu
 * 创建日期：2018/8/21 15:12:16
**/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using System.IO;

namespace XUtils
{
    public class XHttpDownLoader: XSingleton<XHttpDownLoader>
    {
        const int BlockSize = 1024 * 1024 * 4;
        const int DownloadThreadCount = 10;
        const int TimeoutWait = 5*1000;
        const int ReadWriteTimeOut = 2 * 1000;
        const int MaxTryTime = -1;// 单个block下载出错，重新尝试次数限制，-1无限次

        #region ChunkInfo
        class ChunkInfo
        {
            public long from;
            public long to;
            public long curr;
            public long meta_offset;
            public int left_try_time;// 尝试次数

            public bool Finished { get { return curr == to+1; } }
            
            public void Write(FileStream file, byte[] bytes, int len)
            {
                if (curr + len > to + 1)
                {
                    throw new Exception("WTF");
                }
                file.Seek(curr, SeekOrigin.Begin);
                file.Write(bytes, 0, len);
                curr += len;
                file.Seek(meta_offset, SeekOrigin.Begin);
                file.Write(BitConverter.GetBytes(curr), 0, 8);// 当前块已下载的大小，用于断点续传
            }
        }
        int m_next_chunk_info_idx = 0;
        List<ChunkInfo> m_chunk_info_list = new List<ChunkInfo>();

        ChunkInfo LockGetNextChunkInfo()
        {
            lock (m_chunk_info_list)
            {
                ChunkInfo chunk = null;
                if(m_next_chunk_info_idx < m_chunk_info_list.Count)
                {
                    chunk = m_chunk_info_list[m_next_chunk_info_idx++];
                }
                return chunk;
            }
        }

        void WriteToChunk(FileStream file, ChunkInfo chunk, byte[] bytes, int len)
        {
            chunk.Write(file, bytes, len);
            lock (this)
            {
                m_current_size += len;
            }
            m_downloaded_percent = (float)(m_current_size * 1.0 / m_total_size);
        }

        void InitChunkList(long size)
        {
            m_next_chunk_info_idx = 0;
            m_chunk_info_list.Clear();

            int cnt = (int)((size + BlockSize - 1) / BlockSize);
            for(var i = 0; i < cnt; i++)
            {
                var chunk = new ChunkInfo();
                chunk.from = BlockSize * i;
                chunk.to = Math.Min(size-1, chunk.from + BlockSize - 1);
                //chunk.curr = chunk.from;// 从文件里读取
                chunk.meta_offset = size + 8 * i;
                chunk.left_try_time = MaxTryTime;
                m_chunk_info_list.Add(chunk);
            }
            // 初始化下载的文件
            // 1. 文件不存在，新建文件，并在结尾处添加meta信息（初始化为全0 + 一个233的byte）
            // 2. 文件已存在，长度=size + metaLen，校验下最后一个字节是否是233;
            //    1. 校验失败，清空meta信息，当成新文件
            //    2. 校验成功，读取meta信息，设置到chunk里
            // 3. 文件已存在，长度=size，下载成功了。【填充最终的meta信息，方便后续扩展md5校验】
            using(var file = File.Open(m_dst_path,FileMode.OpenOrCreate))
            {
                bool need_clear_meta_info = false;
                if(file.Length == size)
                {
                    file.SetLength(size + 8 * cnt + 1);
                    file.Seek(size, SeekOrigin.Begin);
                    for (var i = 0; i < cnt; i++)
                    {
                        m_chunk_info_list[i].curr = m_chunk_info_list[i].to+1;
                        file.Write(BitConverter.GetBytes(m_chunk_info_list[i].curr), 0, 8);
                    }
                    file.WriteByte(233);
                    m_current_size = size;
                }
                else if(file.Length == size + 8*cnt + 1)
                {
                    file.Seek(size + 8 * cnt, SeekOrigin.Begin);
                    int b = file.ReadByte();
                    if (b == 233)
                    {
                        file.Seek(size, SeekOrigin.Begin);
                        byte[] array = new byte[8];
                        for (var i = 0; i < cnt; i++)
                        {
                            file.Read(array, 0, 8);
                            var chunk = m_chunk_info_list[i];
                            chunk.curr = BitConverter.ToInt64(array, 0);
                            m_current_size += chunk.curr - chunk.from;
                            // 校验下区间
                            if (chunk.curr < chunk.from || chunk.curr > chunk.to + 1)
                            {
                                need_clear_meta_info = true;// meta错误
                                break;
                            }
                        }
                    }
                    else
                    {
                        need_clear_meta_info = true;
                    }
                }
                else
                {
                    need_clear_meta_info = true;
                }
                if (need_clear_meta_info)
                {
                    file.SetLength(size + 8 * cnt + 1);
                    file.Seek(size, SeekOrigin.Begin);
                    for (var i = 0; i < cnt; i++)
                    {
                        m_chunk_info_list[i].curr = m_chunk_info_list[i].from;
                        file.Write(BitConverter.GetBytes(m_chunk_info_list[i].curr), 0, 8);
                    }
                    file.WriteByte(233);
                    m_current_size = 0;
                }
            }
            
        }

        #endregion ChunkInfo

        public float m_downloaded_percent = 0;
        public long m_total_size = 0;
        public long m_current_size = 0;

        string m_url = "";
        string m_dst_path = "";
        List<Thread> m_thread_list = new List<Thread>(); 

        void Reset()
        {
            AbortDownloadBigFile();
            m_downloaded_percent = 0;
            m_total_size = 0;
            m_current_size = 0;
        }

        /// <summary>
        /// 下载大的文件
        /// 1. 多线程分块并行下载
        /// 2. 支持断点续传
        /// 3. 支持查询进度，和打断
        /// 4. 使用FileShare.ReadWrite，支持同时读写文件
        /// </summary>
        public void StartDownLoadBigFile(string url,string dst_path)
        {
            ServicePointManager.DefaultConnectionLimit = DownloadThreadCount + 100;
            Reset();
            m_url = url;
            m_dst_path = dst_path;
            // 获取文件长度
            m_total_size = GetWebFileSize();
            // 分块
            InitChunkList(m_total_size);
            // 开启线程池下载
            for(var i = 0; i < DownloadThreadCount; i++)
            {
                var th = new Thread(DownloadChunkStart);
                m_thread_list.Add(th);
                th.Start();
            }
        }

        long GetWebFileSize()
        {
            var req = WebRequest.CreateHttp(m_url);
            req.AllowAutoRedirect = true;// 最终会获取到实际响应的url地址
            req.Method = "HEAD";
            req.Timeout = 10 * 1000;// 只获取头部，应该很快
            using (var res = (HttpWebResponse)req.GetResponse())
            {
                return res.ContentLength;
            }
        }

        void DownloadChunkStart()
        {
            byte[] buffer = new byte[16 * 1024];
            for (;;)
            {
                var chunk = LockGetNextChunkInfo();
                if (chunk == null) break;
                
                for (;;)
                {
                    if (chunk.Finished) break;
                    using(var file = File.Open(m_dst_path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
                    {
                        try
                        {
                            var req = WebRequest.CreateHttp(m_url) as HttpWebRequest;
                            req.Timeout = TimeoutWait;
                            req.ReadWriteTimeout = ReadWriteTimeOut;
                            req.AddRange(chunk.curr, chunk.to);
                            using (var res = (HttpWebResponse)req.GetResponse())
                            {
                                if(chunk.to - chunk.curr +1 != res.ContentLength)
                                {
                                    // todo@om error
                                    break;
                                }
                                var res_s = res.GetResponseStream();
                                int read_size = res_s.Read(buffer, 0, buffer.Length);
                                while (read_size > 0)
                                {
                                    WriteToChunk(file, chunk, buffer, read_size);
                                    read_size = res_s.Read(buffer, 0, buffer.Length);
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            if (chunk.left_try_time > 0) { chunk.left_try_time--; }
                            else if (chunk.left_try_time == 0)
                            {
                                // todo@om 要报错了
                                break;
                            }
                        }
                    }
                }
            }
        }

        public void AbortDownloadBigFile()
        {
            foreach(var th in m_thread_list)
            {
                if (th.IsAlive) th.Abort();
            }
            m_thread_list.Clear();
        }

        public bool CheckIsFinished()
        {
            bool ret = m_current_size == m_total_size;
            if (ret)
            {
                foreach(var th in m_thread_list)
                {
                    if (th.IsAlive) th.Join();
                }
                using (var file = File.Open(m_dst_path, FileMode.Open, FileAccess.Write,FileShare.ReadWrite))
                {
                    file.SetLength(m_current_size);
                }
            }

            return ret;
        }
    }
}
