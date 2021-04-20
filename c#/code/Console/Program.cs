using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using XUtils;
using XBinarySerializer = XSerialize.XBinarySerializer;
using XBinarySerializer2 = XSerialize.XBinarySerializer2;
using XXmlSerializer = XSerialize.XXmlSerializer;
using XXmlDump = XSerialize.XXmlDump;
using System.Diagnostics;
using System.Reflection;

class Program
{

    #region XSignal
    class XSignalOne:XSignal<XSignalOne>
    {
    }

    #endregion

    #region XEvent
    class XEventOne : XEvent
    {

    }

    class XEventTwo:XPoolEvent<XEventTwo>
    {

    }
    #endregion

    #region XSingleton
    class Test1: XSingleton<Test1> , IEventReceiveObject
    {
        private XEventContain _event_contain = new XEventContain();

        public void OnGetEvent(XEvent e)
        {
            _event_contain.OnGetEvent(e);
        }

        public override bool Init()
        {
            _event_contain.RegisterHandle<XEventOne>(HandleOneAndTwo);
            _event_contain.RegisterHandle<XEventTwo>(HandleOneAndTwo);
            return true;
        }

        void HandleOneAndTwo(XEvent e)
        {
            Console.WriteLine("recive event {0}", e.GetType().Name);
        }
    }

    #endregion

    enum EnumXX
    {
        A,
        B,
    }


    class TestXmlSerializer
    {
        EnumXX x;
        int b;
        public List<int[]> abc = new List<int[]>() { new int[] { 0, 1 }, new int[] { 2, 3 } };
    }

    class TestXmlSerializer2 : TestXmlSerializer
    {
        EnumXX x = EnumXX.B;
        int b;
    }

    static void Main(string[] args)
    {
        int a = 1;
        while (a == 1) { };
        Console.WriteLine($"Assembly.FullName = {Assembly.GetCallingAssembly().FullName}");
        Console.WriteLine("press any key to start test");
        //Console.ReadKey();
        XLibTool.singleton.Init("test");

        //var a22 = Assembly.LoadFrom("XSerialize.dll");
        //var a222 = Assembly.LoadFrom(Path.GetFullPath("XSerialize.dll"));

        TestXSerialize();

        var bytes = File.ReadAllBytes("test/XSerialize.dll");
        var a3 = Assembly.LoadFrom("test/XSerialize.dll");
        var a33 = Assembly.LoadFrom("test/XSerialize.dll");
        var a4 = Assembly.Load(bytes);
        var a5 = Assembly.Load(bytes);

        var a1 = Assembly.Load("XSerialize");
        var a2 = Assembly.Load("XSerialize");

        //TestHttpDownLoader();
        //TestXEvent();
    }

    static void TestXEvent()
    {
        Console.WriteLine("test singleton");
        XSignalOne.RegisterHandle(Test);
        XSignalOne.TriggerAll();
    }

    static void TestXSerialize()
    {
        {
            List<object[]> xx = new List<object[]>() { new object[] { 0, 1 }, new object[] { 2, 3 } };
            xx[0][1] = xx;
            var serializer = new XBinarySerializer(xx.GetType());
            using (var stream = new MemoryStream())
            {
                serializer.Serialize(stream, xx);
                stream.Seek(0, SeekOrigin.Begin);
                var yy = serializer.Deserialize(stream) as List<object[]>;
                Console.WriteLine(yy[0][1]);
            }
            Console.WriteLine(typeof(EnumXX).IsValueType);
            Console.WriteLine(typeof(object).IsPrimitive);
            Console.WriteLine(typeof(object).IsValueType);
            var arr = Array.CreateInstance(typeof(int), 0, 0);
            arr = new int[0, 0];
            Console.WriteLine("{0}, {1}", arr.GetLowerBound(0), arr.GetUpperBound(1));
            Console.WriteLine(arr.GetType().FullName);
        }
        {
            List<object[]> xx = new List<object[]>() { new object[] { 0, 1 }, new object[] { 2, 3 } };
            xx[0][1] = xx;
            var serializer = new XBinarySerializer2();
            using (var stream = new MemoryStream())
            {
                serializer.Serialize(stream, xx);
                serializer.Serialize(stream, null);
                stream.Seek(0, SeekOrigin.Begin);
                var yy = serializer.Deserialize(stream) as List<object[]>;
                Console.WriteLine(yy[0][1]);
            }
            Console.WriteLine(typeof(EnumXX).IsValueType);
            Console.WriteLine(typeof(object).IsPrimitive);
            Console.WriteLine(typeof(object).IsValueType);
            var arr = Array.CreateInstance(typeof(int), 0, 0);
            arr = new int[0, 0];
            Console.WriteLine("{0}, {1}", arr.GetLowerBound(0), arr.GetUpperBound(1));
            Console.WriteLine(arr.GetType().FullName);
        }
        {
            XXmlSerializer serializer = new XXmlSerializer();

            Action<object> test_func = (object obj) =>
            {
                Type type;
                if (obj == null)
                    type = typeof(object);
                else
                    type = obj.GetType();
                string xx = serializer.SerializeToString(obj);
                Console.WriteLine(xx);
                object yy = serializer.DeserializeFromString(xx, type);
                Console.WriteLine(yy);
            };

            DateTime time = DateTime.Now;
            DateTime utc = DateTime.UtcNow;
            object x = (byte)2;
            byte y = (byte)x;
            test_func(null);
            test_func(1);
            test_func(EnumXX.A);
            test_func((sbyte)-2);
            test_func((long)1123456789123456789);
            test_func(123456789123456789123456789m);
            test_func(new TestXmlSerializer());
            test_func(new TestXmlSerializer2());
        }
        {
            XXmlDump serializer = new XXmlDump();
            Action<object> test_dump = (object obj) =>
            {
                string xx = serializer.Dump(obj);
                Console.WriteLine(xx);
            };
            test_dump(typeof(int[]));
            test_dump(Encoding.UTF8);
        }
    }

    static void TestHttpDownLoader()
    {
        Console.Write("选择下载目标目录：1. Desktop/tmp/ 其他. 当前目录 ：");
        var dir_opt = Console.ReadLine();
        Console.Write("设置线程数（默认 1）：");
        var num_str = Console.ReadLine();
        int num = 1;
        if (int.TryParse(num_str, out num) == false) num = 1;
        Console.Write("是否重新下载（Y/N）(默认N)：");
        var redownload_opt = Console.ReadLine();


        string path = Environment.CurrentDirectory;
        if(dir_opt == "1")
        {
            path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            path = Path.Combine(path, "tmp");
        }

        Directory.CreateDirectory(path);
        path = $@"{path}/Bundle.zip";
        if(File.Exists(path) && redownload_opt.ToLower() == "y")
        {
            File.Delete(path);
        }

        XHttpDownLoader.singleton.DownloadThreadCount = num;
        XHttpDownLoader.singleton.StartDownLoadBigFile(@"https://update.dnt.123u.com/Temp/Bundle.zip", path);
        Console.WriteLine($"DownLoad {XHttpDownLoader.singleton.m_total_size} bytes");
        long start_size = XHttpDownLoader.singleton.m_current_size;
        Stopwatch sw = new Stopwatch();
        sw.Start();
        while (XHttpDownLoader.singleton.CheckIsFinished() == false)
        {
            long size = XHttpDownLoader.singleton.m_current_size - start_size;
            Console.Write($"\rTime:{sw.ElapsedMilliseconds, -20:N0} Speed:{size / (sw.ElapsedMilliseconds+1)*1000.0,-10:N0} {XHttpDownLoader.singleton.m_downloaded_percent:P2}");
            Thread.Sleep(1000);
        }
        sw.Stop();
        Console.WriteLine($"\ncost time: {sw.ElapsedMilliseconds}");

        Console.WriteLine("Start UnZip");

        sw.Restart();
        XHttpDownLoader.MultiThreadUnZip(path, path + ".files/", (percent) => {
            Console.Write($"\rTime:{sw.ElapsedMilliseconds,-20:N0} {percent:P2}");
        });
        sw.Stop();
        Console.WriteLine($"\ncost time: {sw.ElapsedMilliseconds}");
        Console.ReadKey();


    }

    static void Test(object[] args)
    {
        Test1.singleton.OnGetEvent(new XEventOne());
        XEventTwo.Get().FireTo(Test1.singleton);
    }
}