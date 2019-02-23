using System;
using System.Threading;
using System.Threading.Tasks;

namespace Test
{
    class Program
    {
        public static bool exit = false;
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            MyConsole.Run();

            while (!exit)
            {
                Thread.Sleep(1000);
            }
        }
    }

    public class MyConsole
    {
        public static void Run()
        {
            Task task = Task.Run(()=>_Run());
            task.Wait();
        }

        static void _Run()
        {
            while (true)
            {
                Console.Write("> ");
                var line = Console.ReadLine();
                if(line == "exit")
                {
                    Program.exit = true;
                    return;
                }
            }
        }
    }
}
