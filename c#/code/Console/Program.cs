using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using XUtils;
using XBinarySerializer = XSerialize.Binary.XBinarySerializer;

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
    static void Main(string[] args)
    {
        Console.WriteLine("test singleton");
        //XSignalOne.RegisterHandle(Test);
        //XSignalOne.TriggerAll();
        List<int[]> xx = new List<int[]>() { new int[]{ 0, 1 }, new int[]{ 2, 3 } };
        //var serializer = new XBinarySerializer(xx.GetType());
        var serializer = XSerialize.XSerializer.Create(XSerialize.XSerializeType.Binary, xx.GetType());
        using(var stream = new MemoryStream())
        {
            serializer.Serialize(stream, xx);
            stream.Seek(0, SeekOrigin.Begin);
            var yy = serializer.Deserialize(stream) as List<int[]>;
            Console.WriteLine(yy[0][1]);
        }
        Console.WriteLine(typeof(decimal).IsPrimitive);
        Console.WriteLine(typeof(DateTime).IsPrimitive);
        var arr = Array.CreateInstance(typeof(int), 0, 0);
        arr = new int[0, 0];
        Console.WriteLine("{0}, {1}", arr.GetLowerBound(0), arr.GetUpperBound(1));
    }

    static void Test(object[] args)
    {
        Test1.singleton.OnGetEvent(new XEventOne());
        XEventTwo.New().FireTo(Test1.singleton);
    }
}

