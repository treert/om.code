using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/**
 * 序列化工具
 * 
 * Example1: Binary
        List<int[]> xx = new List<int[]>() { new int[]{ 0, 1 }, new int[]{ 2, 3 } };
        var serializer = XSerialize.XSerializer.Create(XSerialize.XSerializeType.Binary, xx.GetType());
        using(var stream = new MemoryStream())
        {
            serializer.Serialize(stream, xx);
            stream.Seek(0, SeekOrigin.Begin);
            var yy = serializer.Deserialize(stream) as List<int[]>;
            Console.WriteLine(yy[0][1]);// output 1
        }
 */
namespace XSerialize
{
    public enum XSerializeType{
        Binary,
        Xml,
        Json,
        Lua,
    }

    public abstract class XSerializer
    {

        public abstract void Serialize(Stream stream, object obj);

        public virtual T Deserialize<T>(Stream stream)
        {
            return (T)Deserialize(stream);
        }

        public abstract object Deserialize(Stream stream);

        public abstract void ResetTypes(params Type[] types);

        public abstract void AddTypes(params Type[] types);

        public static XSerializer Create(XSerializeType type, params Type[] init_types)
        {
            switch(type)
            {
                case XSerializeType.Binary:
                    return new Binary.XBinarySerializer(init_types);
            }
            throw new NotSupportedException(String.Format("do not support {0}", type));
        }
    }
}
