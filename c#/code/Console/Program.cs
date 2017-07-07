using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using XUtils;
using XBinarySerializer = XSerialize.Binary.XBinarySerializer;
using XXmlSerializer = XSerialize.Xml.XXmlSerializer;

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
        {
            //Console.WriteLine("test singleton");
            //XSignalOne.RegisterHandle(Test);
            //XSignalOne.TriggerAll();
        }
        {
            //List<object[]> xx = new List<object[]>() { new object[] { 0, 1 }, new object[] { 2, 3 } };
            //xx[0][1] = xx;
            //Console.WriteLine(xx.GetType().FullName);
            ////var serializer = new XBinarySerializer(xx.GetType());
            //var serializer = XSerialize.XSerializer.Create(XSerialize.XSerializeType.Binary, xx.GetType());
            //using (var stream = new MemoryStream())
            //{
            //    serializer.Serialize(stream, xx);
            //    stream.Seek(0, SeekOrigin.Begin);
            //    var yy = serializer.Deserialize(stream) as List<object[]>;
            //    Console.WriteLine(yy[0][1]);
            //}
            //Console.WriteLine(typeof(EnumXX).IsValueType);
            //Console.WriteLine(typeof(object).IsPrimitive);
            //Console.WriteLine(typeof(object).IsValueType);
            //var arr = Array.CreateInstance(typeof(int), 0, 0);
            //arr = new int[0, 0];
            //Console.WriteLine("{0}, {1}", arr.GetLowerBound(0), arr.GetUpperBound(1));
        }

        {
            MemoryStream stream = new MemoryStream();
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

    }

    static void Test(object[] args)
    {
        Test1.singleton.OnGetEvent(new XEventOne());
        XEventTwo.New().FireTo(Test1.singleton);
    }
}

