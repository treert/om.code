using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Diagnostics;
using System.Runtime.Serialization;

/*
 * typeof(DateTime)
 * typeof(Decimal)
 */
namespace XSerialize.Binary
{
    /****************** decimal ***************************/
    class XBinarySerializeDecimal : XBinarySerializePrimitive<decimal> // 128位浮点数,不是基本类型
    {
        public override object Read(XBinarySerializer serializer, BinaryReader reader, Type type)
        {
            return reader.ReadDecimal();
        }

        public override void Write(XBinarySerializer serializer, BinaryWriter writer, object obj)
        {
            writer.Write((decimal)obj);
        }
    }

    /****************** DateTime ***************************/
    class XBinarySerializeDateTime : XBinarySerializePrimitive<DateTime>
    {
        public override object Read(XBinarySerializer serializer, BinaryReader reader, Type type)
        {
            long v = reader.ReadInt64();
            return DateTime.FromBinary(v);
        }

        public override void Write(XBinarySerializer serializer, BinaryWriter writer, object obj)
        {
            long v = ((DateTime)obj).ToBinary();
            writer.Write(v);
        }
    }
}
