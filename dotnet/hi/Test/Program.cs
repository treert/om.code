using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using om.utils;

namespace Test
{
    [Option("test", tip = "Test command")]
    class TestCmd:CmdLine.ICmd
    {
        [Option("",tip = "Args is a int")]
        public int num;
        public bool help;
        [Option("message",alias = "m", required = true, tip = "Test option message")]
        public string Message { get; set; }

        public void Exec()
        {
            Console.WriteLine($"input {help} {num} {Message}");
        }
    }

    class Program
    {
        public static bool exit = false;
        static void Main(string[] args)
        {
            object a = null;
            Console.WriteLine($" a={a} Hello World!");
            Console.WriteLine(string.Format("a={0}",a));

            //Thread th = new Thread(Work);
            //th.Start();

            var cmd_parser = CmdLine.CreateCmdParser<TestCmd>();


            while (true)
            {
                Console.Write("> ");
                var line = Console.ReadLine();
                if (line == "exit")
                {
                    return;
                }
                cmd_parser.Parse(line.Split(' '))?.Exec();
                // Console.WriteLine("< {0}", line);
            }
        }

        static void Work()
        {

            while (true)
            {
                Thread.Sleep(5000);
                Console.WriteLine();
                Console.WriteLine(DateTime.UtcNow);
                Console.Write("> ");
            }
        }
    }
}
