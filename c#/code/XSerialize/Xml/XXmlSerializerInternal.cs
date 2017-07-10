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
 * 
 * 限制很大：
 * 1. 不支持多态，传入对象类型和定义类型需要相同。【不能使用接口，虚类】
 * 2. 不能有类对象引用出现两次。【当然也不支持引用循环】
 * 3. 类需要有默认构造函数。
 * 4. 容器当前只支持：`List<>`,`Dictionary<,>`
 * 
 * 坑：
 * 1. XmlReader忒不好用
 * 2. Type.GetType(type_name) 容易出错，不用了
 * 
 * > 参考资料：玉开技术博客, msdn
 * > XmlWriter http://www.cnblogs.com/yukaizhao/archive/2011/07/20/xmlwriter-write-xml.html
 * > XmlReader http://www.cnblogs.com/yukaizhao/archive/2011/07/20/xmlreader_read_xml.html
 */
//<ClassName>
//  <FieldName1>1</FieldName1>
//  <FieldName2 "flag"="null"></FieldName2>
//  <FieldName3>
//    <Item>1</Item>
//  </FieldName3>
//  <FieldName4>
//    <Item>
//        <Key>abc</Key>
//        <Value>abc</Value>
//    </Item>
//  </FieldName4>
//  <FieldName5>
//    ...
//  </FieldName5>
//</ClassName>

namespace XSerialize.Xml
{
    class XXmlSerializerInternal
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

            new XXmlSerializeClass(),
		};

        internal Dictionary<Type, XXmlSerializeBase> _type_handle_map = new Dictionary<Type, XXmlSerializeBase>();

        public string SerializeToString(object obj, Type type)
        {
            // 字符串来说，用StringBuilder最方便，只是也把格式限制成了utf-16了
            {
                StringBuilder str = new StringBuilder();
                XmlWriterSettings settings = new XmlWriterSettings();
                settings.Indent = true;// 缩进
                //settings.Encoding = Encoding.UTF8;// encoding并不生效，为了避免混乱，把开头去掉把
                settings.OmitXmlDeclaration = true;
                settings.NewLineChars = "\n";
                using (XmlWriter writer = XmlWriter.Create(str, settings))
                {
                    _writed_obj_ids.Clear();
                    if(type.IsGenericType)
                    {
                        writer.WriteStartElement("Root");
                        writer.WriteAttributeString("generic", type.ToString());
                    }
                    else
                    {
                        writer.WriteStartElement(type.Name);
                    }
                    InternalWrite(writer, obj, type);
                    writer.WriteEndElement();
                    _writed_obj_ids.Clear();
                }
                var xml = str.ToString();
                return xml;
            }

            // utf-8-bom
            //using(MemoryStream stream = new MemoryStream())
            //{
            //    XmlWriterSettings settings = new XmlWriterSettings();
            //    settings.Indent = true;// 缩进
            //    settings.Encoding = Encoding.UTF8;// utf-8-bom
            //    //settings.Encoding = new UTF8Encoding(false,true);// utf-8
            //    settings.NewLineChars = "\n";
            //    using (XmlWriter writer = XmlWriter.Create(stream, settings))
            //    {
            //        _writed_obj_ids.Clear();
            //        writer.WriteStartElement(type.ToString());
            //        InternalWrite(writer, obj, type);
            //        writer.WriteEndElement();
            //        _writed_obj_ids.Clear();
            //    }
            //    // writer已经释放，数据已经在memory里了。
            //    var xml = Encoding.UTF8.GetString(stream.ToArray(),3,(int)stream.Length - 3);
            //    return xml;
            //}
        }

        public object DeserializeFromString(string str, Type type)
        {
            using (StringReader str_reader = new StringReader(str))
            using (XmlReader reader = XmlReader.Create(str_reader))
            {
                // reader.ReadStartElement();
                bool is_null;
                ReadTypeStart(reader, out is_null);
                if (is_null)
                {
                    return null;
                }
                var obj = InternalRead(reader, type);
                ReadTypeEnd(reader);
                return obj;
            }
        }

        internal bool ReadTypeStart(XmlReader reader, out bool is_null)
        {
            var next = reader.MoveToContent();
            if(next == XmlNodeType.Element)
            {
                is_null = reader["flag"] == "null";
                reader.Read();// skip start element
                return true;
            }
            else
            {
                is_null = false;
                return false;
            }
        }

        internal void ReadTypeEnd(XmlReader reader)
        {
            var next = reader.MoveToContent();
            if(next == XmlNodeType.EndElement)
            {
                reader.Read();// skip
            }
        }

        // For check, Xml does not support use one class twice
        Dictionary<object, int> _writed_obj_ids = new Dictionary<object, int>();
        // List<object> _readed_objs = new List<object>();

        internal virtual void InternalWrite(XmlWriter writer, object obj, Type type)
        {
            if(obj == null)
            {
                writer.WriteAttributeString("flag", "null");
                return;
            }
            else
            {
                if(type.IsValueType == false && type != typeof(string))
                {
                    if(_writed_obj_ids.ContainsKey(obj))
                    {
                        throw new XSerializeException("Xml does not support use one class twice");
                    }
                }
                if(obj.GetType() != type)
                {
                    throw new XSerializeException("Type does not match, Xml does not support polymorphic");
                }
                var handle = GetTypeSerializerWithException(type);
                if (type.IsValueType == false && type != typeof(string))
                {
                    _writed_obj_ids.Add(obj, 0);
                }
                handle.Write(this, writer, obj);
            }
        }

        internal object InternalRead(XmlReader reader, Type type)
        {
            if(type.IsValueType == false)
            {
                if (reader["flag"] == "null")
                    return null;
            }
            var handle = GetTypeSerializerWithException(type);
            return handle.Read(this, reader, type);
        }

        XXmlSerializeBase GetTypeSerializerWithException(Type type)
        {
            if(_type_handle_map.ContainsKey(type))
            {
                return _type_handle_map[type];
            }

            var serializer = _inner_serializers.FirstOrDefault(h => h.Handles(type));

            if (serializer == null)
                throw new NotSupportedException(String.Format("No serializer for {0}", type));

            _type_handle_map.Add(type, serializer);
            return serializer;
        }
    }

    /***************************************************************/

    abstract class XXmlSerializeBase
    {
        /// <summary>
        /// Returns if this TypeSerializer handles the given type
        /// </summary>
        public abstract bool Handles(Type type);

        public abstract object Read(XXmlSerializerInternal serializer, XmlReader reader, Type type);
        public abstract void Write(XXmlSerializerInternal serializer, XmlWriter writer, object obj);
    }
}
