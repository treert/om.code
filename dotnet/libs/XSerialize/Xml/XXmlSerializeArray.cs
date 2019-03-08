using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Diagnostics;
using System.Xml;

/*
 * 1. XXX[]
 * 2. string
 * 3. byte[]
 */
namespace XSerialize.Xml
{
    /*************************** XXX[] *********************************************/
    class XXmlSerializeArray : XXmlSerializeBase
    {
        public override bool Handles(Type type)
        {
            return type.IsArray && type.GetArrayRank() == 1;
        }

        public override object Read(XXmlSerializerInternal serializer, XmlReader reader, Type type)
        {
            var method_write = this.GetType()
                .GetMethod("ReadGeneric", BindingFlags.Static | BindingFlags.NonPublic)
                .MakeGenericMethod(type.GetElementType());
            return method_write.Invoke(null, new object[] { serializer, reader });
        }

        static object ReadGeneric<T>(XXmlSerializerInternal serializer, XmlReader reader)
        {
            List<T> obj = new List<T>();
            bool is_null;
            while(serializer.ReadTypeStart(reader, out is_null))
            {
                T item = default(T);
                if(is_null == false)
                {
                    item = (T)serializer.InternalRead(reader, typeof(T));
                }
                obj.Add(item);
                serializer.ReadTypeEnd(reader);
            }
            return obj.ToArray();
        }

        public override void Write(XXmlSerializerInternal serializer, XmlWriter writer, object obj)
        {
            Array arr = (Array)obj;
            if (arr.GetLowerBound(0) != 0)
                throw new XSerializeException("Array low bound must be 0");
            var type = obj.GetType();
            var element_type = type.GetElementType();
            foreach(var info in arr)
            {
                writer.WriteStartElement("Item");
                serializer.InternalWrite(writer, info, element_type);
                writer.WriteEndElement();
            }
        }
    }

    /*************************** string *********************************************/
    class XXmlSerializeString : XXmlSerializeBase
    {
        public override bool Handles(Type type)
        {
            return typeof(string) == type;
        }

        public override object Read(XXmlSerializerInternal serializer, XmlReader reader, Type type)
        {
            var obj = reader.ReadString();
            return obj;
        }

        public override void Write(XXmlSerializerInternal serializer, XmlWriter writer, object obj)
        {
            writer.WriteValue((string)obj);
        }
    }

    /*************************** bype[] *********************************************/
    class XXmlSerializeByteArray : XXmlSerializeBase
    {

        public override bool Handles(Type type)
        {
            return typeof(byte[]) == type;
        }

        public override object Read(XXmlSerializerInternal serializer, XmlReader reader, Type type)
        {
            string s = reader.ReadContentAsString();
            return Convert.FromBase64String(s);
        }

        public override void Write(XXmlSerializerInternal serializer, XmlWriter writer, object obj)
        {
            byte[] data = (byte[])obj;
            writer.WriteString(Convert.ToBase64String(data));
        }
    }
}
