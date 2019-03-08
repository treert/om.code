using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Xml;

namespace XSerialize.Xml
{
    /*
     * should use as last serialize handle
     */
    class XXmlSerializeClass : XXmlSerializeBase
    {

        public override bool Handles(Type type)
        {
            if (type.IsClass)
            {
                CheckIsClassSupportAndThrowException(type);
                return true;
            }
            else if (type.IsValueType)
            {
                return !type.IsPrimitive && !type.IsEnum;
            }
            return false;
        }

        public override object Read(XXmlSerializerInternal serializer, XmlReader reader, Type type)
        {
            var obj = Activator.CreateInstance(type, true);
            bool is_null;
            string field_name;
            while (ReadFieldStart(reader, out field_name, out is_null))
            {
                var field = type.GetField(field_name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if(field == null)
                {
                    // throw ...
                }
                else
                {
                    if(is_null)
                    {
                        field.SetValue(obj, null);
                    }
                    else
                    {
                        field.SetValue(obj, serializer.InternalRead(reader, field.FieldType));
                    }
                }
                serializer.ReadTypeEnd(reader);
            }
            return obj;
        }

        bool ReadFieldStart(XmlReader reader, out string field_name, out bool is_null)
        {
            var next = reader.MoveToContent();
            if(next == XmlNodeType.Element)
            {
                field_name = reader.Name;
                is_null = reader["flag"] == "null";
                reader.Read();// skip
                return true;
            }
            else
            {
                field_name = string.Empty;
                is_null = false;
                return false;
            }
        }

        public override void Write(XXmlSerializerInternal serializer, XmlWriter writer, object obj)
        {
            foreach(var field in GetFieldInfos(obj.GetType()))
            {
                writer.WriteStartElement(field.Name);
                serializer.InternalWrite(writer, field.GetValue(obj), field.FieldType);
                writer.WriteEndElement();
            }
        }

        static IEnumerable<FieldInfo> GetFieldInfos(Type type)
        {
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(fi => (fi.Attributes & FieldAttributes.NotSerialized) == 0)// [NonSerialized]
                .OrderBy(f => f.Name, StringComparer.Ordinal);

            return fields;
        }

        static void CheckIsClassSupportAndThrowException(Type type)
        {
            // should has default constructor
            var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic);
            if (constructors.Length > 0)
            {
                bool has = false;
                foreach(var constructor in constructors)
                {
                    if(constructor.GetParameters().Length == 0)
                    {
                        has = true;
                        break;
                    }
                }
                if(has == false)
                    throw new XSerializeException("Type {0} does not have default constructor which has no params", type);
            }

            // todo@om 这儿可能有bug
            // field name should all be diffrent
            var fields = GetFieldInfos(type).GetEnumerator();
            
            fields.MoveNext();
            var last_field = fields.Current;
            if(last_field != null)
            {
                while(fields.MoveNext())
                {
                    var field = fields.Current;
                    if(field.Name == last_field.Name)
                    {
                        throw new XSerializeException("field name error: Type {0} and {1} has same field name {2}",
                            last_field.DeclaringType, field.DeclaringType, field.Name);
                    }
                    last_field = field;
                }
            }
        }
    }
}
