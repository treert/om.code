using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var b1 = File.Exists(@"E:\MyGit\github\script_utils\c#\other");
            var b2 = Directory.Exists(@"E:\MyGit\github\script_utils\c#\other");
            var b3 = File.Exists(@"E:\MyGit\github\script_utils\c#\other\Readme.md");
            var b4 = Directory.Exists(@"E:\MyGit\github\script_utils\c#\other\Readme.md");
            Console.WriteLine($"{b1} {b2} {b3} {b4}");

            TestFileIO.TestParallelReadAndWrite();
            TestFileIO.TestCreateBigFile();
            TestFileIO.TestCreateBigFile(1024*1024*50);
        }
    }
}
