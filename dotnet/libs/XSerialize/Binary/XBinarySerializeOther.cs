using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Diagnostics;
using System.Runtime.Serialization;

/*
 * object
 * Enum
 * DateTime
 * Decimal
 */
namespace XSerialize.Binary
{
    /******************* object ***********************************/
    class XBinarySerializeObject : XBinarySerializerBase
    {
        public override bool Handles(Type type)
        {
            return typeof(object) == type;
        }

        public override object Read(IBinarySerializerForHandle serializer, BinaryReader reader, Type type)
        {
            var obj = new object();
            serializer.InternalAddReadObjToCacheList(obj);
            return obj;
        }

        public override void Write(IBinarySerializerForHandle serializer, BinaryWriter writer, object obj)
        {
            Debug.Assert(typeof(object) == obj.GetType());
            return;
        }
    }

    /******************* enum ***************************************/
    class XBinarySerializeEnum : XBinarySerializerBase
    {
        public override bool Handles(Type type)
        {
            return type.IsEnum;
        }

        public override object Read(IBinarySerializerForHandle serializer, BinaryReader reader, Type type)
        {
            object obj = serializer.InternalRead(reader, Enum.GetUnderlyingType(type));
            return obj;// Enum里没有好的方法转换类型，发现可以直接=
        }

        public override void Write(IBinarySerializerForHandle serializer, BinaryWriter writer, object obj)
        {
            serializer.InternalWrite(writer, obj, Enum.GetUnderlyingType(obj.GetType()));
        }

        public override IEnumerable<Type> AddSubtypes(IBinarySerializerForHandle serializer, Type type)
        {
            return new[] { Enum.GetUnderlyingType(type) };
        }
    }

    /****************** decimal ***************************/
    class XBinarySerializeDecimal : XBinarySerializePrimitive<decimal> // 128位浮点数,不是基本类型
    {
        public override object Read(IBinarySerializerForHandle serializer, BinaryReader reader, Type type)
        {
            return reader.ReadDecimal();
        }

        public override void Write(IBinarySerializerForHandle serializer, BinaryWriter writer, object obj)
        {
            writer.Write((decimal)obj);
        }
    }

    /****************** DateTime ***************************/
    class XBinarySerializeDateTime : XBinarySerializePrimitive<DateTime>
    {
        public override object Read(IBinarySerializerForHandle serializer, BinaryReader reader, Type type)
        {
            long v = reader.ReadInt64();
            return DateTime.FromBinary(v);
        }

        public override void Write(IBinarySerializerForHandle serializer, BinaryWriter writer, object obj)
        {
            long v = ((DateTime)obj).ToBinary();
            writer.Write(v);
        }
    }
}
