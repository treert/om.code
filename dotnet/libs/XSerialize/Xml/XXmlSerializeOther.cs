using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Xml;

/*
 * object
 * Enum
 * Decimal
 * DateTime
 * DateTimeOffset
 */
namespace XSerialize.Xml
{
    /******************* object ***********************************/
    class XXmlSerializeObject : XXmlSerializeBase
    {
        public override bool Handles(Type type)
        {
            return typeof(object) == type;
        }

        public override object Read(XXmlSerializerInternal serializer, XmlReader reader, Type type)
        {
            var obj = new object();
            return obj;
        }

        public override void Write(XXmlSerializerInternal serializer, XmlWriter writer, object obj)
        {
            Debug.Assert(typeof(object) == obj.GetType());
            return;
        }
    }

    /******************* enum ***************************************/
    class XXmlSerializeEnum : XXmlSerializeBase
    {
        public override bool Handles(Type type)
        {
            return type.IsEnum;
        }

        public override object Read(XXmlSerializerInternal serializer, XmlReader reader, Type type)
        {
            string s = reader.ReadContentAsString();
            var obj = Enum.Parse(type, s);
            return obj;
        }

        public override void Write(XXmlSerializerInternal serializer, XmlWriter writer, object obj)
        {
            //writer.WriteAttributeString("type", obj.GetType().ToString());
            writer.WriteValue(obj.ToString());
        }
    }

    /****************** decimal ***************************/
    class XXmlSerializeDecimal : XXmlSerializeBase // 128位浮点数,不是基本类型
    {
        public override bool Handles(Type type)
        {
            return typeof(decimal) == type;
        }

        public override object Read(XXmlSerializerInternal serializer, XmlReader reader, Type type)
        {
            return reader.ReadContentAsDecimal();
        }

        public override void Write(XXmlSerializerInternal serializer, XmlWriter writer, object obj)
        {
            writer.WriteValue((decimal)obj);
        }
    }

    /****************** DateTime ***************************/
    class XXmlSerializeDateTime : XXmlSerializeBase
    {
        public override bool Handles(Type type)
        {
            return typeof(DateTime) == type;
        }

        public override object Read(XXmlSerializerInternal serializer, XmlReader reader, Type type)
        {
            return reader.ReadContentAsDateTime();
        }

        public override void Write(XXmlSerializerInternal serializer, XmlWriter writer, object obj)
        {
            writer.WriteValue((DateTime)obj);
        }
    }

    /****************** DateTimeOffset ***************************/
    class XXmlSerializeDateTimeOffset : XXmlSerializeBase
    {
        public override bool Handles(Type type)
        {
            return typeof(DateTimeOffset) == type;
        }

        public override object Read(XXmlSerializerInternal serializer, XmlReader reader, Type type)
        {
            return reader.ReadContentAsDateTimeOffset();
        }

        public override void Write(XXmlSerializerInternal serializer, XmlWriter writer, object obj)
        {
            writer.WriteValue((DateTimeOffset)obj);
        }
    }
}
