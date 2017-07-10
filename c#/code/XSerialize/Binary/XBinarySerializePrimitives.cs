using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Diagnostics;
using System.Runtime.Serialization;


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
namespace XSerialize.Binary
{


    /*************** primitives *****************************/
    abstract class XBinarySerializePrimitive<T> : XBinarySerializerBase
    {
        public override bool Handles(Type type)
        {
            return typeof(T) == type;
        }
    }
    class XBinarySerializeBoolean : XBinarySerializePrimitive<bool>
    {

        public override object Read(IBinarySerializerForHandle serializer, BinaryReader reader, Type type)
        {
            return reader.ReadBoolean();
        }

        public override void Write(IBinarySerializerForHandle serializer, BinaryWriter writer, object obj)
        {
            writer.Write((bool)obj);
        }
    }

    class XBinarySerializeChar : XBinarySerializePrimitive<char>
    {

        public override object Read(IBinarySerializerForHandle serializer, BinaryReader reader, Type type)
        {
            return reader.ReadChar();
        }

        public override void Write(IBinarySerializerForHandle serializer, BinaryWriter writer, object obj)
        {
            writer.Write((char)obj);
        }
    }

    class XBinarySerializeByte : XBinarySerializePrimitive<byte>
    {

        public override object Read(IBinarySerializerForHandle serializer, BinaryReader reader, Type type)
        {
            return reader.ReadByte();
        }

        public override void Write(IBinarySerializerForHandle serializer, BinaryWriter writer, object obj)
        {
            writer.Write((byte)obj);
        }
    }

    class XBinarySerializeSByte : XBinarySerializePrimitive<sbyte>
    {

        public override object Read(IBinarySerializerForHandle serializer, BinaryReader reader, Type type)
        {
            return reader.ReadSByte();
        }

        public override void Write(IBinarySerializerForHandle serializer, BinaryWriter writer, object obj)
        {
            writer.Write((sbyte)obj);
        }
    }

    class XBinarySerializeInt16 : XBinarySerializePrimitive<short>
    {

        public override object Read(IBinarySerializerForHandle serializer, BinaryReader reader, Type type)
        {
            return reader.ReadInt16();
        }

        public override void Write(IBinarySerializerForHandle serializer, BinaryWriter writer, object obj)
        {
            writer.Write((short)obj);
        }
    }

    class XBinarySerializeUInt16 : XBinarySerializePrimitive<ushort>
    {

        public override object Read(IBinarySerializerForHandle serializer, BinaryReader reader, Type type)
        {
            return reader.ReadUInt16();
        }

        public override void Write(IBinarySerializerForHandle serializer, BinaryWriter writer, object obj)
        {
            writer.Write((ushort)obj);
        }
    }

    class XBinarySerializeInt32 : XBinarySerializePrimitive<int>
    {

        public override object Read(IBinarySerializerForHandle serializer, BinaryReader reader, Type type)
        {
            return reader.ReadInt32();
        }

        public override void Write(IBinarySerializerForHandle serializer, BinaryWriter writer, object obj)
        {
            writer.Write((int)obj);
        }
    }

    class XBinarySerializeUInt32 : XBinarySerializePrimitive<uint>
    {

        public override object Read(IBinarySerializerForHandle serializer, BinaryReader reader, Type type)
        {
            return reader.ReadUInt32();
        }

        public override void Write(IBinarySerializerForHandle serializer, BinaryWriter writer, object obj)
        {
            writer.Write((uint)obj);
        }
    }

    class XBinarySerializeInt64 : XBinarySerializePrimitive<long>
    {

        public override object Read(IBinarySerializerForHandle serializer, BinaryReader reader, Type type)
        {
            return reader.ReadInt64();
        }

        public override void Write(IBinarySerializerForHandle serializer, BinaryWriter writer, object obj)
        {
            writer.Write((long)obj);
        }
    }

    class XBinarySerializeUInt64 : XBinarySerializePrimitive<ulong>
    {

        public override object Read(IBinarySerializerForHandle serializer, BinaryReader reader, Type type)
        {
            return reader.ReadUInt64();
        }

        public override void Write(IBinarySerializerForHandle serializer, BinaryWriter writer, object obj)
        {
            writer.Write((ulong)obj);
        }
    }

    class XBinarySerializeSingle : XBinarySerializePrimitive<float>
    {

        public override object Read(IBinarySerializerForHandle serializer, BinaryReader reader, Type type)
        {
            return reader.ReadSingle();
        }

        public override void Write(IBinarySerializerForHandle serializer, BinaryWriter writer, object obj)
        {
            writer.Write((float)obj);
        }
    }

    class XBinarySerializeDouble : XBinarySerializePrimitive<double>
    {

        public override object Read(IBinarySerializerForHandle serializer, BinaryReader reader, Type type)
        {
            return reader.ReadDouble();
        }

        public override void Write(IBinarySerializerForHandle serializer, BinaryWriter writer, object obj)
        {
            writer.Write((double)obj);
        }
    }
}
