using om.utils;
using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

/// <summary>
/// 扩展阅读 https://stackoverflow.com/questions/23481181/whats-the-algorithm-used-in-net-to-generate-random-numbers
/// </summary>
namespace FileEncrypt
{
    [Option("filepkg", tip = "Test command")]
    class TestCmd : CmdLine.ICmd
    {
        const int DefaultSeed = 123;// 默认种子

        [Option("", tip = "files or dirs", required = true)]
        List<string> m_paths = new List<string>();
        [Option("unpkg", alias = "u", tip = "default=false。解码文件")]
        bool m_unpkg = false;
        [Option("seed", alias = "s", tip = "Random Seed, default=123")]
        int m_seed = DefaultSeed;
        [Option("keep", alias = "k", tip = "保持文件名，只改后缀。default=false")]
        bool m_keepFileName = false;
        [Option("origin", alias = "o", tip = "如果编码后的文件名太长，那就不编码。default=false")]
        bool m_useOriginNameWhen = false;

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

        Regex m_reg = new Regex(@"^([-]?\d+)@([^/]+)$");
        bool TryParseFileName(string name, out string filename, out int seed)
        {
            var match = m_reg.Match(name);
            if (match.Success)
            {
                seed = int.Parse(match.Groups[1].Value);
                var rest = match.Groups[2].Value;
                if ((rest.Length & 1) == 0 && Regex.IsMatch(rest, "^[0-9a-fA-F]+$"))
                {
                    var filenamebytes = Convert.FromHexString(rest);
                    var originname = Encoding.UTF8.GetString(filenamebytes);
                    filename = originname;
                }
                else
                {
                    filename = rest;
                }
            }
            else
            {
                filename = name;
                seed = DefaultSeed;
            }
            return true;
        }

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
            Random rand = new Random(seed);
            

            string targetpath;
            if (m_keepFileName)
            {
                targetpath = $"{dir}/{seed}@{filename}{s_pkg_suffix}";
            }
            else
            {
                var filenamebytes = Encoding.UTF8.GetBytes(filename);
                var filenamehex = Convert.ToHexString(filenamebytes);
                targetpath = $"{dir}/{seed}@{filenamehex}{s_pkg_suffix}";
                if (filenamehex.Length > 128)
                {
                    Console.WriteLine($"[err] file name too long when encode filename, {path}");
                    if (m_useOriginNameWhen)
                    {
                        targetpath = $"{dir}/{seed}@{filename}{s_pkg_suffix}";
                    }
                    else
                    {
                        return;
                    }
                }
            }

            Console.WriteLine($"[info] pkg file: {path}");
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
            TryParseFileName(onlyname, out var originname, out var seed);
            //var seds = onlyname.Split("@");
            //int seed = int.Parse(seds[0]);
            //var filenamebytes = Convert.FromHexString(seds[1]);
            //var originname = Encoding.UTF8.GetString(filenamebytes);
            Console.WriteLine($" ==> {originname}");

            Random rand = new Random(seed);
           
            // rename file to tmp
            var tmppath = $"{dir}/{originname}{s_pkg_suffix}{s_tmp_suffix}";
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
