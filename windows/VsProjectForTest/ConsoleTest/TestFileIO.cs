/**
 * 创 建 者：treertzhu
 * 创建日期：2018/8/20 19:12:02
**/

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace ConsoleTest
{
    class TestFileIO
    {
        public static void TestParallelReadAndWrite()
        {
            //string file = Path.GetTempFileName();
            string file = "test.txt";
            // 先创建个
            File.WriteAllText(file, "test");

            Func<FileAccess,FileShare,FileStream> open_exist = (access, share) =>
            {
                return File.Open(file, FileMode.Open, access, share);
            };
            List<string> ok_info = new List<string>();
            Console.WriteLine("TestParallelReadAndWrite: ");
            foreach(FileAccess access in Enum.GetValues(typeof(FileAccess)))
                foreach (FileShare share in Enum.GetValues(typeof(FileShare)))
                    foreach (FileAccess access2 in Enum.GetValues(typeof(FileAccess)))
                        foreach (FileShare share2 in Enum.GetValues(typeof(FileShare)))
                        {
                            FileStream f1 = null, f2 = null;
                            bool ok = true;
                            try
                            {
                                //f1 = open_exist(access, share);
                                //f2 = open_exist(access2, share2);
                                var t1 = Task.Run(()=> {
                                    f1 = open_exist(access, share);
                                });
                                var t2 = Task.Run(() => {
                                    f2 = open_exist(access2, share2);
                                });

                                Task.WaitAll(t1, t2);
                            }
                            catch (Exception e)
                            {
                                ok = false;
                            }
                            finally
                            {
                                if (f1 != null) f1.Close();
                                if (f2 != null) f2.Close();
                            }
                            Console.ForegroundColor = ok ? ConsoleColor.Green : ConsoleColor.Red;
                            var info = $"{access,-16}{share,-16}{access2,-16}{share2,-16}{ok}";
                            if (ok) ok_info.Add(info);
                            Console.WriteLine(info);
                            
                        }
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"TestParallelReadAndWrite OK List:");
            Console.ForegroundColor = ConsoleColor.Green;
            for(var i = 0; i < ok_info.Count; i++)
            //foreach(var info in ok_info)
            {
                Console.WriteLine($"{i+1,-10}{ok_info[i]}");
            }
            Console.ForegroundColor = ConsoleColor.White;

            Console.WriteLine(@"找到了规律：
1. 只有Read,Write,ReadWrite组合可以兼容
2. 第二次打开时的不出错条件: share2 Contain acess1, share1 Contain access2");
            string[] names = new string[] { "Read", "Write", "ReadWrite" };
            int idx = 0;
            foreach (var access_s in names)
                foreach (var share_s in names)
                    foreach (var access2_s in names)
                        foreach (var share2_s in names)
                        {
                            if (!(share2_s.Contains(access_s) && share_s.Contains(access2_s)))
                            {
                                continue;
                            }
                            FileStream f1 = null, f2 = null;
                            FileAccess access = (FileAccess)Enum.Parse(typeof(FileAccess), access_s);
                            FileAccess access2 = (FileAccess)Enum.Parse(typeof(FileAccess), access2_s);
                            FileShare share = (FileShare)Enum.Parse(typeof(FileShare), share_s);
                            FileShare share2 = (FileShare)Enum.Parse(typeof(FileShare), share2_s);

                            bool ok = true;
                            try
                            {
                                f1 = open_exist(access, share);
                                f2 = open_exist(access2, share2);
                            }
                            catch (Exception e)
                            {
                                ok = false;
                            }
                            finally
                            {
                                if (f1 != null) f1.Close();
                                if (f2 != null) f2.Close();
                            }
                            idx++;
                            Console.ForegroundColor = ok ? ConsoleColor.Green : ConsoleColor.Red;
                            var info = $"{access,-16}{share,-16}{access2,-16}{share2,-16}{ok}";
                            //if (ok) ok_info.Add(info);
                            Console.WriteLine($"{idx,-10}{info}");

                        }

            Console.ForegroundColor = ConsoleColor.White;
        }

        public static void TestCreateBigFile(long len = 1024*1024*100)
        {
            var f = File.Open("test.txt",FileMode.OpenOrCreate);
            f.SetLength(len);
            f.Close();
        }
    }
}
