using om.utils;
using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// 扩展阅读 https://stackoverflow.com/questions/23481181/whats-the-algorithm-used-in-net-to-generate-random-numbers
/// </summary>
namespace FileEncrypt
{
    [Option("filepkg", tip = "Test command")]
    class TestCmd : CmdLine.ICmd
    {
        [Option("", tip = "files or dirs", required = true)]
        List<string> m_paths = new List<string>();
        [Option("unpkg", alias = "u", tip = "default is false, meaning to pkg file")]
        bool m_unpkg = false;
        [Option("seed", alias = "s", tip = "Random Seed, default is 123")]
        int m_seed = 123;

        public void Exec()
        {
            foreach(var p in m_paths)
            {
                WorkDFS(p);
            }
        }

        void WorkDFS(string path)
        {
            if (File.Exists(path))
            {
                if (m_unpkg)
                {
                    UnPkgSingleFile(path);
                }
                else
                {
                    PkgSingleFile(path, m_seed);
                }
                // Console.Out.Flush();
            }
            else if (Directory.Exists(path))
            {
                foreach(var p in Directory.GetFileSystemEntries(path))
                {
                    WorkDFS(p);
                }
            }
            else
            {
                Console.WriteLine($"[error] invalid path: {path}");
            }
        }

        const string s_pkg_suffix = ".ompkg";
        const string s_tmp_suffix = ".omtmp";
        byte[] s_buffer = new byte[4096];// 这个4096是不能改的，不然解密失败
        
        void PkgSingleFile(string path, int seed)
        {
            if(File.Exists(path) == false)
            {
                Console.WriteLine($"[error] file not exist: {path}");
                return;
            }
            var filename = Path.GetFileName(path);
            var dir = Path.GetDirectoryName(path);
            if (filename.EndsWith(s_pkg_suffix))
            {
                return;// do nothing
            }
            if (filename.EndsWith(s_tmp_suffix))
            {
                Console.WriteLine($"[warn] tmp file: {path}");
                return;
            }

            Console.WriteLine($"[info] pkg file: {path}");

            Random rand = new Random(seed);
            // rename file to tmp
            var tmppath = $"{dir}/{filename}{s_tmp_suffix}";
            File.Move(path, tmppath);
            // encode file
            using(var fs = File.Open(tmppath, FileMode.Open, FileAccess.ReadWrite))
            {
                fs.Seek(0, SeekOrigin.Begin);
                var cnt = fs.Read(s_buffer, 0, s_buffer.Length);
                for(var i = 0; i < cnt; i++)
                {
                    s_buffer[i] = (byte)(s_buffer[i] ^ rand.Next());
                }
                fs.Seek(0, SeekOrigin.Begin);
                fs.Write(s_buffer, 0, cnt);
            }
            // rename file to pkg
            var filenamebytes = Encoding.UTF8.GetBytes(filename);
            var filenamehex = Convert.ToHexString(filenamebytes);
            var targetpath = $"{dir}/{seed}@{filenamehex}{s_pkg_suffix}";
            File.Move(tmppath, targetpath);
        }

        void UnPkgSingleFile(string path)
        {
            if (File.Exists(path) == false)
            {
                Console.WriteLine($"[error] file not exist: {path}");
                return;
            }
            var filename = Path.GetFileName(path);
            var dir = Path.GetDirectoryName(path);
            if (filename.EndsWith(s_tmp_suffix))
            {
                Console.WriteLine($"[warn] tmp file: {path}");
                return;
            }
            if (filename.EndsWith(s_pkg_suffix) == false)
            {
                return;// do nothing
            }

            Console.Write($"[info] unpkg file: {path}");
            // analyse file name
            var onlyname = filename.Substring(0, filename.Length - s_pkg_suffix.Length);
            var seds = onlyname.Split("@");
            int seed = int.Parse(seds[0]);
            var filenamebytes = Convert.FromHexString(seds[1]);
            var originname = Encoding.UTF8.GetString(filenamebytes);
            Console.WriteLine($" ==> {originname}");

            Random rand = new Random(seed);
           
            // rename file to tmp
            var tmppath = $"{dir}/{filename}{s_tmp_suffix}";
            File.Move(path, tmppath);
            // encode file
            using (var fs = File.Open(tmppath, FileMode.Open, FileAccess.ReadWrite))
            {
                fs.Seek(0, SeekOrigin.Begin);
                var cnt = fs.Read(s_buffer, 0, s_buffer.Length);
                for (var i = 0; i < cnt; i++)
                {
                    s_buffer[i] = (byte)(s_buffer[i] ^ rand.Next());
                }
                fs.Seek(0, SeekOrigin.Begin);
                fs.Write(s_buffer, 0, cnt);
            }
            // rename file to pkg
            var targetpath = $"{dir}/{originname}";
            File.Move(tmppath, targetpath);
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var cmd_parser = CmdLine.CreateCmdParser<TestCmd>();
            cmd_parser.Parse(args)?.Exec();
        }
    }
}
