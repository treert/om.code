using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Diagnostics;
using System.Xml;

/*
 * 1. List<>
 * 2. Dictionary<,>
 */
namespace XSerialize.Xml
{
    // 序列化泛型的基类，方便写代码
    abstract class XXmlSerializeGenericTypeBase : XXmlSerializeBase
    {
        public override bool Handles(Type type)
        {
            if (!type.IsGenericType)
                return false;

            return type.GetGenericTypeDefinition() == GetWorkGenericType();
        }

        public abstract Type GetWorkGenericType();

        public override object Read(XXmlSerializerInternal serializer, XmlReader reader, Type type)
        {
            var method_write = this.GetType()
                .GetMethod("ReadGeneric", BindingFlags.Static | BindingFlags.NonPublic)
                .MakeGenericMethod(type.GetGenericArguments());
            return method_write.Invoke(null, new object[] { serializer, reader });
        }

        public override void Write(XXmlSerializerInternal serializer, XmlWriter writer, object obj)
        {
            var method_write = this.GetType()
                .GetMethod("WriteGeneric", BindingFlags.Static | BindingFlags.NonPublic)
                .MakeGenericMethod(obj.GetType().GetGenericArguments());
            method_write.Invoke(null, new object[] { serializer, writer, obj });
        }
    }

    class XXmlSerializeList : XXmlSerializeGenericTypeBase
    {
        public override Type GetWorkGenericType()
        {
            return typeof(List<>);
        }

        static object ReadGeneric<T>(XXmlSerializerInternal serializer, XmlReader reader)
        {
            List<T> obj = new List<T>();
            bool is_null;
            while (serializer.ReadTypeStart(reader, out is_null))
            {
                T item = default(T);
                if (is_null == false)
                {
                    item = (T)serializer.InternalRead(reader, typeof(T));
                }
                obj.Add(item);
                serializer.ReadTypeEnd(reader);
            }
            return obj;
        }

        static void WriteGeneric<T>(XXmlSerializerInternal serializer, XmlWriter writer, List<T> obj)
        {
            for (int i = 0; i < obj.Count; ++i)
            {
                writer.WriteStartElement("Item");
                serializer.InternalWrite(writer, obj[i], typeof(T));
                writer.WriteEndElement();
            }
        }
    }

    class XXmlSerializeDictionary : XXmlSerializeGenericTypeBase
    {
        public override Type GetWorkGenericType()
        {
            return typeof(Dictionary<,>);
        }

        static object ReadGeneric<TKey, TValue>(XXmlSerializerInternal serializer, XmlReader reader)
        {
            Dictionary<TKey, TValue> obj = new Dictionary<TKey, TValue>();
            bool is_null;
            while (serializer.ReadTypeStart(reader, out is_null))
            {
                Debug.Assert(is_null == false);
                // Key
                TKey key;
                serializer.ReadTypeStart(reader, out is_null);
                Debug.Assert(is_null == false);// Key 不能为null
                key = (TKey)serializer.InternalRead(reader, typeof(TKey));
                serializer.ReadTypeEnd(reader);
                // Value
                TValue value = default(TValue);
                serializer.ReadTypeStart(reader, out is_null);
                if (is_null == false)
                {
                    value = (TValue)serializer.InternalRead(reader, typeof(TValue));
                }
                obj.Add(key, value);
                serializer.ReadTypeEnd(reader);
            }
            return obj;
        }

        static void WriteGeneric<TKey, TValue>(XXmlSerializerInternal serializer, XmlWriter writer, Dictionary<TKey, TValue> obj)
        {
            foreach (var item in obj)
            {
                writer.WriteStartElement("Item");
                writer.WriteStartElement("Key");
                serializer.InternalWrite(writer, item.Key, typeof(TKey));
                writer.WriteEndElement();
                writer.WriteStartElement("Value");
                serializer.InternalWrite(writer, item.Value, typeof(TValue));
                writer.WriteEndElement();
                writer.WriteEndElement();
            }
        }
    }
}
