using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


/*
 * 事件模式可以很好的做到解耦，细分类型较多，核心是注册回调
 * 
 * 1. 订阅者模式
 * 2. 事件模式
 * 3. 消息模式
 * 
 */
namespace XUtils
{

    
    public abstract class XEvent
    {
        
    }

    public abstract class XPoolEvent<T> : XEvent where T:class,new()
    {
        protected XPoolEvent() { }
        private static Stack<T> _pool = new Stack<T>(100);
        public static T Get()
        {
            if (_pool.Count > 0)
            {
                return _pool.Pop();
            }
            else
            {
                return new T();
            }
        }

        public static void Recycle(T obj)
        {
            _pool.Push(obj);
        }

        public void FireTo(IEventReceiveObject receive_obj)
        {
            receive_obj.OnGetEvent(this);
            Recycle(this as T);
        }
    }

    public interface IEventReceiveObject
    {
        void OnGetEvent(XEvent e);
    }

    public delegate void XEventHandler(XEvent e);

    /// <summary>
    /// 对象基类，可以接受事件
    /// </summary>
    public class XEventContain : IEventReceiveObject
    {
        private Dictionary<Type, XEventHandler> _event_handles = new Dictionary<Type, XEventHandler>();

        public void RegisterHandle<T>(XEventHandler handle) where T:XEvent
        {
            _event_handles[typeof(T)] = handle;
        }

        public void RemoveHandle<T>() where T :XEvent
        {
            _event_handles.Remove(typeof(T));
        }

        public void OnGetEvent(XEvent e)
        {
            XEventHandler handle = null;
            if(_event_handles.TryGetValue(e.GetType(), out handle))
            {
                handle(e);
            }
        }
    } 


    public delegate void XSignalHandler(object[] args);
    /// <summary>
    /// 全局信号系统，解耦用
    /// 1. 立即触发
    /// 2. 无延时
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class XSignal<T> where T:class
    {
        protected XSignal()
        {
            // no instance
            throw new Exception("signal can not new instance");
        }

        static HashSet<XSignalHandler> s_handles = new HashSet<XSignalHandler>();
        static HashSet<XSignalHandler> s_delay_removes = new HashSet<XSignalHandler>();
        static bool s_is_runing = false;

        public static void RegisterHandle(XSignalHandler handler)
        {
            if(s_handles.Contains(handler))
            {
                throw new Exception("can not register a handle twice");
            }
            s_handles.Add(handler);
        }
        public static void RemoveHandle(XSignalHandler handler)
        {
            if(s_is_runing)
            {
                s_delay_removes.Add(handler);
            }
            else
            {
                s_handles.Remove(handler);
            }
        }
        public static void Reset()
        {
            s_handles.Clear();
            s_delay_removes.Clear();
        }

        public static void TriggerAll(params object[] args)
        {
            s_is_runing = true;
            foreach (var handler in s_handles)
            {
                if (s_delay_removes.Count > 0 && s_delay_removes.Contains(handler))
                    continue;
                handler(args);
            }
            s_is_runing = false;

            if(s_delay_removes.Count > 0)
            {
                foreach (var handler in s_delay_removes)
                {
                    s_handles.Remove(handler);
                }
                s_delay_removes.Clear();
            }
        }
    }
}
