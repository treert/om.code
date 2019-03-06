/**
 * 创 建 者：treertzhu
 * 创建日期：2019/3/4 10:42:50
**/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace om.utils
{
    /// <summary>
    /// 替代 IEnumerator<T> ，几个原因
    /// 1. 没有判断当前是否有值的接口，MoveNext会移动指针。MyItor增加了HasValue
    /// 2. 初始化时，指针指向第一个元素之前，个人希望指向第一个元素。
    /// 3. Current 不会报错，当没有值后，Current返回default(T)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class MyItor<T> : IDisposable
    {
        IEnumerator<T> m_itor;

        public IEnumerator<T> Enumerator { get { return m_itor; } }
        public bool HasValue { get; private set; }
        public MyItor(IEnumerable<T> args)
        {
            Debug.Assert(args != null, "args can not be null");
            m_itor = args.GetEnumerator();
            MoveNext();
        }

        public MyItor(T arg)
        {
            List<T> list = new List<T>() { arg };
            m_itor = list.GetEnumerator();
            MoveNext();
        }

        public T Current
        {
            get
            {
                return HasValue ? m_itor.Current : default(T);
            }
        }

        public bool MoveNext()
        {
            return HasValue = m_itor.MoveNext();
        }

        public void Dispose()
        {
            if (m_itor != null)
            {
                m_itor.Dispose();
                m_itor = null;
            }
        }

        public void Reset()
        {
            m_itor.Reset();
            MoveNext();
        }
    }
}
