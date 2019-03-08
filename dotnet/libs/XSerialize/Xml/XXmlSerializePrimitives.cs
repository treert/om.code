using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Xml;


/**
 * 基元类型(IsPrimitive)
    typeof(bool),
    typeof(char),
    typeof(byte), typeof(sbyte),
    typeof(ushort), typeof(short),
    typeof(uint), typeof(int),
    typeof(ulong), typeof(long),
    typeof(float), typeof(double),
 */
namespace XSerialize.Xml
{


    /*************** primitives *****************************/
    abstract class XXmlSerializePrimitive<T> : XXmlSerializeBase
    {
        public override bool Handles(Type type)
        {
            return typeof(T) == type;
        }
    }
    class XXmlSerializeBoolean : XXmlSerializePrimitive<bool>
    {

        public override object Read(XXmlSerializerInternal serializer, XmlReader reader, Type type)
        {
            return reader.ReadContentAsBoolean();
        }

        public override void Write(XXmlSerializerInternal serializer, XmlWriter writer, object obj)
        {
            writer.WriteValue((bool)obj);
        }
    }

    class XXmlSerializeChar : XXmlSerializePrimitive<char>
    {

        public override object Read(XXmlSerializerInternal serializer, XmlReader reader, Type type)
        {
            string s =  reader.ReadString();
            if (string.IsNullOrEmpty(s))
                return ' ';
            else
                return s[0];
        }

        public override void Write(XXmlSerializerInternal serializer, XmlWriter writer, object obj)
        {
            writer.WriteValue((char)obj);
        }
    }

    class XXmlSerializeByte : XXmlSerializePrimitive<byte>
    {

        public override object Read(XXmlSerializerInternal serializer, XmlReader reader, Type type)
        {
            return (byte)reader.ReadContentAsInt();
        }

        public override void Write(XXmlSerializerInternal serializer, XmlWriter writer, object obj)
        {
            writer.WriteValue(obj);
        }
    }

    class XXmlSerializeSByte : XXmlSerializePrimitive<sbyte>
    {

        public override object Read(XXmlSerializerInternal serializer, XmlReader reader, Type type)
        {
            return (sbyte)reader.ReadContentAsInt();
        }

        public override void Write(XXmlSerializerInternal serializer, XmlWriter writer, object obj)
        {
            writer.WriteValue(obj);
        }
    }

    class XXmlSerializeInt16 : XXmlSerializePrimitive<short>
    {

        public override object Read(XXmlSerializerInternal serializer, XmlReader reader, Type type)
        {
            return (short)reader.ReadContentAsInt();
        }

        public override void Write(XXmlSerializerInternal serializer, XmlWriter writer, object obj)
        {
            writer.WriteValue(obj);
        }
    }

    class XXmlSerializeUInt16 : XXmlSerializePrimitive<ushort>
    {

        public override object Read(XXmlSerializerInternal serializer, XmlReader reader, Type type)
        {
            return (ushort)reader.ReadContentAsInt();
        }

        public override void Write(XXmlSerializerInternal serializer, XmlWriter writer, object obj)
        {
            writer.WriteValue(obj);
        }
    }

    class XXmlSerializeInt32 : XXmlSerializePrimitive<int>
    {

        public override object Read(XXmlSerializerInternal serializer, XmlReader reader, Type type)
        {
            return (int)reader.ReadContentAsInt();
        }

        public override void Write(XXmlSerializerInternal serializer, XmlWriter writer, object obj)
        {
            writer.WriteValue((int)obj);
        }
    }

    class XXmlSerializeUInt32 : XXmlSerializePrimitive<uint>
    {

        public override object Read(XXmlSerializerInternal serializer, XmlReader reader, Type type)
        {
            return (uint)reader.ReadContentAsLong();
        }

        public override void Write(XXmlSerializerInternal serializer, XmlWriter writer, object obj)
        {
            writer.WriteValue(obj);
        }
    }

    class XXmlSerializeInt64 : XXmlSerializePrimitive<long>
    {

        public override object Read(XXmlSerializerInternal serializer, XmlReader reader, Type type)
        {
            return (long)reader.ReadContentAsLong();
        }

        public override void Write(XXmlSerializerInternal serializer, XmlWriter writer, object obj)
        {
            writer.WriteValue(obj);
        }
    }

    class XXmlSerializeUInt64 : XXmlSerializePrimitive<ulong>
    {

        public override object Read(XXmlSerializerInternal serializer, XmlReader reader, Type type)
        {
            return (ulong)reader.ReadContentAsDecimal();
        }

        public override void Write(XXmlSerializerInternal serializer, XmlWriter writer, object obj)
        {
            writer.WriteValue((decimal)obj);
        }
    }

    class XXmlSerializeSingle : XXmlSerializePrimitive<float>
    {

        public override object Read(XXmlSerializerInternal serializer, XmlReader reader, Type type)
        {
            return reader.ReadContentAsFloat();
        }

        public override void Write(XXmlSerializerInternal serializer, XmlWriter writer, object obj)
        {
            writer.WriteValue((float)obj);
        }
    }

    class XXmlSerializeDouble : XXmlSerializePrimitive<double>
    {

        public override object Read(XXmlSerializerInternal serializer, XmlReader reader, Type type)
        {
            return reader.ReadContentAsDouble();
        }

        public override void Write(XXmlSerializerInternal serializer, XmlWriter writer, object obj)
        {
            writer.WriteValue((double)obj);
        }
    }
}
