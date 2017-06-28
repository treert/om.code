using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
