using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FileEncode
{
    class WorkModel
    {
        public readonly static WorkModel singleton = new WorkModel();

        public delegate void UpdateFileList();
        public UpdateFileList m_updateFileList;

        public delegate void UpdateProcesss(int percent);
        public UpdateProcesss m_updateProcess;

        public delegate void ShowMessage(string msg);
        public ShowMessage m_showMessage;

        private bool _is_find_all_files = false;
        private bool _is_converting_files = false;
        private Thread _work_thread = null;


        public void AddFiles(string[] paths)
        {
            if(CheckCanWork())
            {
                _work_thread = new Thread(new ParameterizedThreadStart(DFSAll));
                _work_thread.IsBackground = true;
                _work_thread.Start(paths);
            }
        }

        public void ConvertToUtf8()
        {
            if (CheckCanWork())
            {
                _work_thread = new Thread(new ParameterizedThreadStart(_ConvertTo));
                _work_thread.IsBackground = true;
                _work_thread.Start(new UTF8Encoding(true,true));
            }
        }

        public void Clear()
        {
            _file_info_list.Clear();
            m_updateFileList();
        }

        bool CheckCanWork()
        {
            if(_is_converting_files)
            {
                MessageBox.Show("正在转换格式");
                return false;
            }
            if(_is_find_all_files)
            {
                MessageBox.Show("正在搜索文件列表");
                return false;
            }
            if (_work_thread != null)// 异常
            {
                _work_thread.Abort();
                _work_thread = null;
                _is_converting_files = false;
                _is_find_all_files = false;
                MessageBox.Show("异常 系统已重启 重新尝试");
                return false;
            }
            return true;
        }

        private void _ConvertTo(object encoding_)
        {
            var encoding = (Encoding)encoding_;
            _is_converting_files = true;
            try
            {
                m_updateProcess(0);
                for (int i = 0; i < _file_info_list.Count; ++i)
                {
                    var item = _file_info_list[i];
                    if (item.is_ok)
                    {
                        Utils.ConvertFile(item.file_name, item.encoding, encoding);
                        item.encoding = encoding;
                    }
                    m_updateProcess(Utils.GetPercent(i, _file_info_list.Count));
                }
                m_updateProcess(-1);
                m_updateFileList();
                m_showMessage("转码完成");
            }
            catch(Exception e){
                m_showMessage("转码失败：" + e.Message);
            }
            finally
            {
                _is_converting_files = false;
                _work_thread = null;
            }
        }

        private List<ItemData> _file_info_list = new List<ItemData>();
        public List<ItemData> GetFileInfoList()
        {
            return _file_info_list;
        }

        private void DFSAll(object paths_)
        {
            _is_find_all_files = true;
            try
            {
                m_updateProcess(0);
                var file_info_list = new List<ItemData>();
                var file_list = new List<string[]>();
                string[] paths = (string[])paths_;
                foreach (var path in paths)
                {
                    DFS(path, file_list);
                }
                for (int i = 0; i < file_list.Count; ++i)
                {
                    var file = file_list[i][0] + file_list[i][1];
                    var item = new ItemData();
                    item.file_name = file;
                    item.file_short_name = file_list[i][1];
                    try
                    {
                        item.encoding = Utils.GetFileEncodeType(file);
                        file_info_list.Add(item);
                    }
                    catch (Exception e)
                    {
                        item.is_ok = false;
                        item.not_support = (e is NotSupportedException);
                        item.err_msg = e.Message;
                        file_info_list.Add(item);
                    }
                    m_updateProcess(Utils.GetPercent(i + 1, file_list.Count));
                }
                m_updateProcess(-1);

                _file_info_list = file_info_list;
                m_updateFileList();
            }
            finally
            {
                _is_find_all_files = false;
                _work_thread = null;
            }
        }

        private void DFS(string root_path, List<string[]> file_list)
        {
            if (File.Exists(root_path))
            {
                file_list.Add(new string[] { "", root_path });
                return;
            }
            Stack<string> stack = new Stack<string>();
            stack.Push(root_path);
            while (stack.Count > 0)
            {
                var path = stack.Pop();
                if (File.Exists(path))
                {
                    file_list.Add(new string[] { root_path , path.Replace(root_path,"")});
                }
                else if (Directory.Exists(path))
                {
                    foreach (var file in Directory.GetFileSystemEntries(path))
                    {
                        stack.Push(file);
                    }
                }
            }
        }
    }

    class ItemData{
        public string file_name = string.Empty;
        public string file_short_name = string.Empty;
        public Encoding encoding = Encoding.UTF8;
        public bool is_ok = true;
        public bool not_support = false;
        public string err_msg = "err";
        public string encoding_name{
            get{
                if(is_ok)
                {
                    return Utils.GetEncodingName(encoding);
                }
                else
                {
                    return err_msg;
                }
            }
        }
    }

    class Utils{

        static Dictionary<Encoding, int[]> s_bom_map = new Dictionary<Encoding, int[]>(){
                {new UTF8Encoding(true,true), new int[]{0xEF,0xBB,0xBF}},// utf-8-bom
                {new UnicodeEncoding(true,true,true), new int[]{0xEF,0xFF}},// utf-16-big
                {new UnicodeEncoding(false,true,true), new int[]{0xFF, 0xFE}},// utf-16-small
                {new UTF32Encoding(true, true, true), new int[]{0,0,0xFE, 0xFF}}, // utf-32-big
                {new UTF32Encoding(false, true, true), new int[]{0xFF, 0xFE,0,0}}, // utf-32-small
            };

        static Dictionary<Encoding, string> s_encoding_name_map = new Dictionary<Encoding, string>(){
                {new UTF8Encoding(false,true), "utf-8"},// utf-8
                {new UTF8Encoding(true,true), "utf-8-bom"},// utf-8-bom
                {new UnicodeEncoding(true,true,true), "utf-16-big"},// utf-16-big
                {new UnicodeEncoding(false,true,true), "utf-16-small"},// utf-16-small
                {new UTF32Encoding(true, true, true), "utf-32-big"}, // utf-32-big
                {new UTF32Encoding(false, true, true), "utf-32-small"}, 
            };

        public static System.Text.Encoding GetFileEncodeType(string filename)
        {
            FileInfo fi = new FileInfo(filename);
            if(!fi.Exists)
                throw new Exception("不存在");

            if(fi.Length > 1024*1024*32)// 32M
                throw new NotSupportedException(">32M");

            //if(fi.Length < 4) // to short
            //    throw new NotSupportedException("<4B");

            Byte[] buffer;
            using(FileStream fs = fi.OpenRead())
            using(BinaryReader br = new BinaryReader(fs))
            {
                buffer = br.ReadBytes(4);
            }

            foreach (var item in s_bom_map)
            {
                var bom = item.Value;
                bool ok = false;
                if(buffer.Length >= bom.Length)
                {
                    ok = true;
                    for(int i = 0; i < bom.Length; ++i)
                    {
                        if(bom[i] != (int)buffer[i])
                        {
                            ok = false;
                            break;
                        }
                    }
                }
                if(ok)
                {
                    return item.Key;
                }
            }

            // wtf defaut or utf-8
            try
            {
                var encoding = new UTF8Encoding(false,true);
                File.ReadAllText(filename, encoding);
                return encoding;
            }
            catch { }

            try
            {
                var encoding = System.Text.Encoding.Default;
                File.ReadAllText(filename, encoding);
                return encoding;
            }
            catch { }

            throw new Exception("Unkown");
        }

        public static void ConvertFile(string filename, Encoding from, Encoding to)
        {
            if (from == to) return;
            FileInfo fi = new FileInfo(filename);
            if (!fi.Exists)
                throw new Exception("不存在");

            if (fi.Length > 1024 * 1024 * 32)// 32M
                throw new NotSupportedException(">32M");

            var text = File.ReadAllText(filename, from);
            File.WriteAllText(filename, text, to);
        }

        public static int GetPercent(int cur, int total)
        {
            if (total == 0)
                return 100;
            int per = cur * 100 / total;
            per = Math.Min(100, per);
            return per;
        }

        public static string GetEncodingName(Encoding encoding)
        {
            if(s_encoding_name_map.ContainsKey(encoding))
            {
                return s_encoding_name_map[encoding];
            }
            else
            {
                return encoding.WebName;
            }
        }
    }
}
