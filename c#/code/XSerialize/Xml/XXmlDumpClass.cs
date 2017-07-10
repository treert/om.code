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
    class XXmlDumpClass : XXmlSerializeBase
    {

        public override bool Handles(Type type)
        {
            if (type.IsClass)
            {
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
            Debug.Assert(false);
            return null;
        }

        public override void Write(XXmlSerializerInternal serializer, XmlWriter writer, object obj)
        {
            _Write(serializer, writer, obj, obj.GetType());
        }

        void _Write(XXmlSerializerInternal serializer, XmlWriter writer, object obj, Type type)
        {
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .OrderBy(f => f.Name, StringComparer.Ordinal);
            foreach (var field in fields)
            {
                writer.WriteStartElement(field.Name);
                serializer.InternalWrite(writer, field.GetValue(obj), field.FieldType);
                writer.WriteEndElement();
            }

            if (type.BaseType != null)
            {
                writer.WriteStartElement("__Base__");
                writer.WriteAttributeString("type",type.BaseType.ToString());
                _Write(serializer, writer, obj, type.BaseType);
                writer.WriteEndElement();
            }
        }

        static IEnumerable<FieldInfo> GetFieldInfos(Type type)
        {
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .OrderBy(f => f.Name, StringComparer.Ordinal);

            if (type.BaseType == null)
            {
                return fields;
            }
            else
            {
                var baseFields = GetFieldInfos(type.BaseType);
                return baseFields.Concat(fields);
            }
        }
    }
}
