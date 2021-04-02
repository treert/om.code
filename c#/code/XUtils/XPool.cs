using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/*
 * 1. 管理new的使用
 * 2. 可做性能优化
 * 
 * 注意！！
 * 需要对称调用Delete回收的
 */
namespace XUtils
{
    public class XPool<T> where T: new()
    {
        private static Stack<T> _pool = new Stack<T>(100);
        private static int _new_count = 0;
        public static T Get()
        {
            if(_pool.Count > 0)
            {
                return _pool.Pop();
            }
            else
            {
                _new_count++;
                return new T();
            }
        }

        public static void Recycle(T obj)
        {
            _pool.Push(obj);
        }
    }
}
