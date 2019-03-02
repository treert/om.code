using System;
using System.Collections.Generic;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            IEnumerable<string> xx = args;
            var itor = xx.GetEnumerator();
            itor.Reset();
            //Console.WriteLine(itor.Current ?? "null");
            while (itor.MoveNext())
            {
                Console.WriteLine(itor.Current ?? "null");
            }
            //Console.WriteLine(itor.Current ?? "null");
            itor.MoveNext();
            //Console.WriteLine(itor.Current ?? "null");
            Console.WriteLine("Hello World!");
        }
    }
}
