using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using om.utils;

namespace Test
{
    [Option("test", tip = "test command")]
    class TestCmd
    {
        public int m_args;
        public int _a = 2;
        public bool b;
        string c;
        public int A { get; set; } = 3;

        public int[] F { get;}

         int this[int idx]
        {
            get { return 0; }
            set { }
        }

        public void f()
        {

        }
    }

    class Program
    {
        public static bool exit = false;
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            Thread th = new Thread(Work);
            th.Start();

            var type = typeof(TestCmd);
            var fields = type.GetFields();
            var properties = type.GetProperties();

            var ff = type.GetProperty("F");
            var xx = ff.GetIndexParameters();

            TestCmd t = new TestCmd();


            while (true)
            {
                Console.Write("> ");
                var line = Console.ReadLine();
                if (line == "exit")
                {
                    return;
                }
                Console.WriteLine("< {0}", line);
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
