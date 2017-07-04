using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


/*
 * 单例模式，最简单的模式
 * 1. 提供方便的全局访问入口
 * 2. 提供一个简单的对象管理
 * 
 * 注意！！！
 * 1. 容易导致代码耦合严重
 * 2. 基本是不能用于多线程的程序的，需要加锁
 * 3. **多组单例可以通过修改GetInstance()的代码实现**
 *
 * Example
 * class TestSingleton:XSingleton<TestSingleton>{}
 * 
 * 之后使用：TestSingleton.singleton
 */
namespace XUtils
{
    public abstract class XBaseSingleton
    {
        public abstract bool Init();
        public abstract void Uninit();
    }

    /// <summary>
    /// 单例模版
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class XSingleton<T>: XBaseSingleton where T: new()
    {
        // add restriction
        protected XSingleton()
        {
            if(_instance != null)
            {
                throw new Exception(_instance.ToString() + @" can not be created again.");
            }

            Init();
        }

        private static readonly T _instance = new T();

        public static T singleton
        {
            get
            {
                return _instance;
            }
        }

        public override bool Init() { return true; }
        public override void Uninit() { }
    }
}
