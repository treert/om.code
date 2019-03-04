/**
 * 创 建 者：treertzhu
 * 创建日期：2019/3/4 11:07:46
**/

using System;
using System.Collections.Generic;
using System.Text;

namespace om.utils
{
    /// <summary>
    /// 为了和系统的异常做个区分
    /// </summary>
    public class MyException : Exception
    {
        public MyException(string message) : base(message)
        {
        }
    }
}
