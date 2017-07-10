using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Diagnostics;


/*
 * 1. List<>
 * 2. Dictionary<,>
 */
namespace XSerialize.Binary
{
    // 序列化泛型的基类，方便写代码
    abstract class XBinarySerializeGenericTypeBase : XBinarySerializerBase
    {
        public override bool Handles(Type type)
        {
            if (!type.IsGenericType)
                return false;

            return type.GetGenericTypeDefinition() == GetWorkGenericType();
        }

        public abstract Type GetWorkGenericType();

        public override IEnumerable<Type> AddSubtypes(IBinarySerializerForHandle serializer, Type type)
        {
            return type.GetGenericArguments();
        }

        public override object Read(IBinarySerializerForHandle serializer, BinaryReader reader, Type type)
        {
            var method_write = this.GetType()
                .GetMethod("ReadGeneric", BindingFlags.Static | BindingFlags.NonPublic)
                .MakeGenericMethod(type.GetGenericArguments());
            return method_write.Invoke(null, new object[] { serializer, reader });
        }

        public override void Write(IBinarySerializerForHandle serializer, BinaryWriter writer, object obj)
        {
            var method_write = this.GetType()
                .GetMethod("WriteGeneric", BindingFlags.Static | BindingFlags.NonPublic)
                .MakeGenericMethod(obj.GetType().GetGenericArguments());
            method_write.Invoke(null, new object[] { serializer, writer, obj });
        }
    }

    class XBinarySerializeList : XBinarySerializeGenericTypeBase
    {
        public override Type GetWorkGenericType()
        {
            return typeof(List<>);
        }

        static object ReadGeneric<T>(IBinarySerializerForHandle serializer, BinaryReader reader)
        {
            int count = reader.ReadInt32();
            List<T> obj = new List<T>(count);
            serializer.InternalAddReadObjToCacheList(obj);
            for (int i = 0; i < count; ++i)
            {
                obj.Add((T)serializer.InternalRead(reader, typeof(T)));
            }
            return obj;
        }

        static void WriteGeneric<T>(IBinarySerializerForHandle serializer, BinaryWriter writer, List<T> obj)
        {
            writer.Write(obj.Count);
            for (int i = 0; i < obj.Count; ++i)
            {
                serializer.InternalWrite(writer, obj[i], typeof(T));
            }
        }
    }

    class XBinarySerializeDictionary : XBinarySerializeGenericTypeBase
    {
        public override Type GetWorkGenericType()
        {
            return typeof(Dictionary<,>);
        }

        static object ReadGeneric<TKey, TValue>(IBinarySerializerForHandle serializer, BinaryReader reader)
        {
            int count = reader.ReadInt32();
            Dictionary<TKey, TValue> obj = new Dictionary<TKey, TValue>(count);
            serializer.InternalAddReadObjToCacheList(obj);
            for (int i = 0; i < count; ++i)
            {
                TKey key = (TKey)serializer.InternalRead(reader, typeof(TKey));
                TValue value = (TValue)serializer.InternalRead(reader, typeof(TValue));
                obj.Add(key, value);
            }
            return obj;
        }

        static void WriteGeneric<TKey, TValue>(IBinarySerializerForHandle serializer, BinaryWriter writer, Dictionary<TKey, TValue> obj)
        {
            writer.Write(obj.Count);
            foreach (var item in obj)
            {
                serializer.InternalWrite(writer, item.Key, typeof(TKey));
                serializer.InternalWrite(writer, item.Value, typeof(TValue));
            }
        }
    }
}
