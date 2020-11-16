using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using om.utils;

namespace FileCodeTool
{
    class WorkCmd : CmdLine.ICmd
    {
        [Option("file_filter",alias = "f", required = false, tip = "select file by suffix")]
        List<string> file_filter = new List<string>();
        [Option("dir_filter",alias = "d", required = false, tip = "ignore dir by prefix")]
        List<string> dir_filter = new List<string>();
        [Option("change_encode", alias = "t", required = false, tip = @"set if what to change file encode
valid args like: utf-8 utf-16 utf-16be ...")]
        string change_encode = "";
        [Option("",tip = "root files or dirs")]
        List<string> roots = new List<string>();

        Encoding target_encoding = null;
        void DFS(string root)
        {
            var files = Directory.GetFiles(root);
            foreach(var file in files)
            {
                if(file_filter.Count == 0)
                {
                    HandleFile(file);
                    continue;
                }
                foreach(var it in file_filter)
                {
                    if (file.EndsWith(it))
                    {
                        HandleFile(file);
                        break;
                    }
                }
            }
            var dirs = Directory.GetDirectories(root);
            foreach(var dir in dirs)
            {
                bool ok = true;
                foreach(var it in dir_filter)
                {
                    if (dir.StartsWith(it))
                    {
                        ok = false;
                        break;
                    }
                }
                if (ok)
                {
                    DFS(dir);
                }
            }
        }

        void HandleFile(string file)
        {
            var (code, name) = EncodeUtils.DetectEncodeByFileName(file);
            if(code == null)
            {
                Console.WriteLine($"unkown\t{file}");
                return;
            }
            if(target_encoding == null)
            {
                Console.WriteLine($"{name}\t{file}");
            }
            else
            {
                try
                {
                    var data = File.ReadAllText(file, code);
                    File.WriteAllText(file, data, target_encoding);
                    Console.WriteLine($"{name}\t=>\t{change_encode}\t{file}");
                }
                catch(Exception e)
                {
                    Console.WriteLine($"err\t{file}\t{e.Message}");
                }
            }
        }
        public void Exec()
        {
            if(change_encode.IsNullOrEmpty() == false)
            {
                target_encoding = EncodeUtils.GetEncodingByName(change_encode);
                if(target_encoding == null)
                {
                    Console.WriteLine("option change_encode not valid");
                }
            }
            // 遍历文件，做处理。
            foreach(var root in roots)
            {
                if (File.Exists(root))
                {
                    HandleFile(root);// 强制处理文件，无视后缀
                }
                else if (Directory.Exists(root))
                {
                    DFS(root);
                }
            }
        }
    }


    class Program
    {
        static void Main(string[] args)
        {
            var cmd_parser = CmdLine.CreateCmdParser<WorkCmd>();
            var cmd = cmd_parser.Parse(args);
            cmd?.Exec();
        }
    }
}
