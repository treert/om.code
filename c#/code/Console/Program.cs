using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using XUtils;

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

        XSignalOne.RegisterHandle(Test);
        XSignalOne.TriggerAll();
        
    }

    static void Test(object[] args)
    {
        Test1.singleton.OnGetEvent(new XEventOne());
        XEventTwo.New().FireTo(Test1.singleton);
    }
}

