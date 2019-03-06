/**
 * 创 建 者：treertzhu
 * 创建日期：2019/3/5 18:33:35
**/

using System;
using System.Collections.Generic;
using System.Text;

namespace om.utils
{
    public static class MyConvert
    {
        public static T FromString<T>(string arg)
        {
            return Specializer<T>.Call(arg);// 不支持的就挂掉吧,可惜不能在编译期就挂掉
        }

        static MyConvert()
        {
            Specializer<bool>.Fun = (arg) => Convert.ToBoolean(arg);
            Specializer<char>.Fun = (arg) => Convert.ToChar(arg);
            Specializer<byte>.Fun = (arg) => Convert.ToByte(arg);
            Specializer<sbyte>.Fun = (arg) => Convert.ToSByte(arg);
            Specializer<short>.Fun = (arg) => Convert.ToInt16(arg);
            Specializer<ushort>.Fun = (arg) => Convert.ToUInt16(arg);
            Specializer<int>.Fun = (arg) => Convert.ToInt32(arg);
            Specializer<uint>.Fun = (arg) => Convert.ToUInt32(arg);
            Specializer<long>.Fun = (arg) => Convert.ToInt64(arg);
            Specializer<ulong>.Fun = (arg) => Convert.ToUInt64(arg);
            Specializer<float>.Fun = (arg) => Convert.ToSingle(arg);
            Specializer<double>.Fun = (arg) => Convert.ToDouble(arg);

            Specializer<decimal>.Fun = (arg) => Convert.ToDecimal(arg);
            Specializer<DateTime>.Fun = (arg) => Convert.ToDateTime(arg);

            Specializer<byte[]>.Fun = (arg) => Convert.FromBase64String(arg);
        }

        public class Specializer<T>
        {
            public static Func<string, T> Fun;
            public static T Call(string arg) => Fun(arg);
        }
    }
}
