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
 * Dump object
 */
//<Root type="**" flag="new" id="1">
//  <FieldName1 declare="**" type="System.Int32">1</FieldName1>
//  <FieldName2 declare="**" "flag"="null"></FieldName2>
//  <FieldName3 declare="**" type="**list**">
//    <Item ***>1</Item>
//  </FieldName3>
//  <FieldName4 ***>
//    <Item ***>
//        <Key ***>abc</Key>
//        <Value ***>abc</Value>
//    </Item>
//  </FieldName4>
//  <__Base__ type="**" ***>
//    ...
//  </__Base__>
//</ClassName>

namespace XSerialize.Xml
{
    class XXmlDumpInternal : XXmlSerializerInternal
    {
        XXmlSerializeBase[] _inner_serializers = new XXmlSerializeBase[] {
            new XXmlSerializeObject(),
            new XXmlSerializeEnum(),

            new XXmlSerializeBoolean(),
            new XXmlSerializeChar(),

            new XXmlSerializeByte(),
            new XXmlSerializeSByte(),
            new XXmlSerializeInt16(),
            new XXmlSerializeUInt16(),
            new XXmlSerializeInt32(),
            new XXmlSerializeUInt32(),
            new XXmlSerializeInt64(),
            new XXmlSerializeUInt64(),

            new XXmlSerializeSingle(),
            new XXmlSerializeDouble(),

            new XXmlSerializeDecimal(),
            new XXmlSerializeDateTime(),
            new XXmlSerializeDateTimeOffset(),
            
            new XXmlSerializeString(),
            new XXmlSerializeByteArray(),

            new XXmlSerializeArray(),

            new XXmlSerializeList(),
            new XXmlSerializeDictionary(),

            new XXmlDumpClass(),// 这个不一样
		};

        public string Dump(object obj)
        {
            if (obj == null)
            {
                return @"<Root flag=""null""/>";
            }
            string xml;
            StringBuilder str = new StringBuilder();
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;// 缩进
            //settings.Encoding = Encoding.UTF8;// encoding并不生效，为了避免混乱，把开头去掉把
            settings.OmitXmlDeclaration = true;
            settings.NewLineChars = "\n";
            using (XmlWriter writer = XmlWriter.Create(str, settings))
            {
                _writed_obj_ids.Clear();
                writer.WriteStartElement("Root");
                InternalWrite(writer, obj, obj.GetType());
                writer.WriteEndElement();
                _writed_obj_ids.Clear();
            }
            xml = str.ToString();

            return xml;
        }

        Dictionary<object, int> _writed_obj_ids = new Dictionary<object, int>();

        void _Write(XmlWriter writer, object obj)
        {
            if (obj == null)
            {
                writer.WriteAttributeString("flag", "null");
                return;
            }
            else
            {
                var type = obj.GetType();
                if (type.IsValueType == false && type != typeof(string))
                {
                    if (_writed_obj_ids.ContainsKey(obj))
                    {
                        writer.WriteAttributeString("flag", "reuse");
                        writer.WriteAttributeString("id", _writed_obj_ids[obj].ToString());
                        return;
                    }
                }
                XXmlSerializeBase handle = GetTypeSerializer(type);
                if(handle == null)
                {
                    writer.WriteAttributeString("flag", "not serializable");
                    return;
                }
                if (type.IsValueType == false && type != typeof(string))
                {
                    writer.WriteAttributeString("flag", "new");
                    writer.WriteAttributeString("id", _writed_obj_ids.Count.ToString());
                    _writed_obj_ids.Add(obj, _writed_obj_ids.Count);
                }
                handle.Write(this, writer, obj);
            }
        }

        internal override void InternalWrite(XmlWriter writer, object obj, Type type)
        {
            writer.WriteAttributeString("type", type.ToString());
            _Write(writer, obj);
        }
        XXmlSerializeBase GetTypeSerializer(Type type)
        {
            if (_type_handle_map.ContainsKey(type))
            {
                return _type_handle_map[type];
            }

            var serializer = _inner_serializers.FirstOrDefault(h => h.Handles(type));

            _type_handle_map.Add(type, serializer);
            return serializer;
        }
    }
}
