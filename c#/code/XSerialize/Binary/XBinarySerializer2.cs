using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Diagnostics;
/**
 * binary serialize tool 2
 * 
 * 不同的地方：
 * 1. 在文件开头处保存类型信息
 * 2. 不检查hash值，不要自己作死就好
 * 
 * 格式：
 * [assemblyClassName][typeid,value][assemblyClassName][typeid,[typeid,value]*]*
 */
namespace XSerialize.Binary
{
    class XBinarySerializerInternal2 : IBinarySerializerForHandle
    {
        // use at fisrt, don't recommend use, hash will take it info
        XBinarySerializerBase[] _custom_serializers = new XBinarySerializeByte[]{
            
        };
        XBinarySerializerBase[] _inner_serializers = new XBinarySerializerBase[] {
            new XBinarySerializeObject(),
            new XBinarySerializeEnum(),

            new XBinarySerializeBoolean(),
            new XBinarySerializeChar(),

            new XBinarySerializeByte(),
            new XBinarySerializeSByte(),
            new XBinarySerializeInt16(),
            new XBinarySerializeUInt16(),
            new XBinarySerializeInt32(),
            new XBinarySerializeUInt32(),
            new XBinarySerializeInt64(),
            new XBinarySerializeUInt64(),

            new XBinarySerializeSingle(),
            new XBinarySerializeDouble(),

            new XBinarySerializeDecimal(),
            new XBinarySerializeDateTime(),
            
            new XBinarySerializeString(),
            new XBinarySerializeByteArray(),

            new XBinarySerializeArray(),

            new XBinarySerializeList(),
            new XBinarySerializeDictionary(),

            new XBinarySerializeClass(),// special, can handle all class and struct, must be at last
        };

        // For type save flag
        class TypeFlag
        {
            internal static byte NUll = 255;
            internal static byte TYPE = 254;// 是类型信息
            internal static byte REUSE = 253;
            internal static byte ID32 = 252;// 后面一个int32是typeid
            //[0,251] short typeid

            // For WriteType and ReadType

        }

        List<Type> _type_list = new List<Type>();// For Read
        Dictionary<Type, int> _type_id_map = new Dictionary<Type, int>();// For Write
        Dictionary<Type, XBinarySerializerBase> _type_handle_map = new Dictionary<Type, XBinarySerializerBase>();
        // For class serialize
        protected Dictionary<Type, FieldInfo[]> _class_fields = new Dictionary<Type, FieldInfo[]>();

        public void AddClassFieldInfos(Type type, FieldInfo[] fields)
        {
            Debug.Assert(false);
        }

        public FieldInfo[] GetClassFieldInfos(Type type)
        {
            FieldInfo[] ret;
            if(_class_fields.TryGetValue(type, out ret) == false)
            {
                ret = XBinarySerializeClass.GetFieldInfos(type).ToArray();
                _class_fields.Add(type, ret);
            }
            return ret;
        }

        public void Serialize(Stream stream, object obj)
        {
            UTF8Encoding utf_8 = new UTF8Encoding(false, true);
            using (BinaryWriter writer = new BinaryWriter(stream, utf_8, true))
            {
                _writed_obj_ids.Clear();
                _type_id_map.Clear();
                _Write(writer, obj);
                _writed_obj_ids.Clear();
            }
        }

        public object Deserialize(Stream stream)
        {
            UTF8Encoding utf_8 = new UTF8Encoding(false, true);
            using (BinaryReader reader = new BinaryReader(stream, utf_8, true))
            {
                _readed_objs.Clear();
                _type_list.Clear();
                var obj = _Read(reader);
                _readed_objs.Clear();
                return obj;
            }
        }

        int GetOrAddTypeId(BinaryWriter writer, Type type)
        {
            if(_type_id_map.ContainsKey(type))
            {
                return _type_id_map[type];
            }
            else
            {
                int idx = _type_id_map.Count;
                WriteType(writer, type);
                _type_id_map.Add(type, idx);
                return idx;
            }
        }

        void WriteType(BinaryWriter writer, Type type)
        {
            GenerateTypeSerializer(type);


            writer.Write(TypeFlag.TYPE);
            if(type.AssemblyQualifiedName == null)
            {
                throw new XSerializeException("Can not write TypeName {0}", type.ToString());
            }

            writer.Write(type.AssemblyQualifiedName);
        }

        void ReadType(BinaryReader reader)
        {
            var AssemblyQualifiedName = reader.ReadString();
            Type type = Type.GetType(AssemblyQualifiedName);
            if(type == null)
            {
                throw new XSerializeException("Can not find Type {0}", AssemblyQualifiedName);
            }

            GenerateTypeSerializer(type);
            _type_list.Add(type);
        }



        // For class reuse, object born num
        Dictionary<object, int> _writed_obj_ids = new Dictionary<object, int>();// 【不需要传入比较器，这儿会使用Object.Equals来比较，相当于 == 】
        List<object> _readed_objs = new List<object>();

        public void InternalWrite(BinaryWriter writer, object obj, Type type)
        {
            if (type.IsValueType)
            {
                GenerateTypeSerializer(type).Write(this, writer, obj);// value type optmize, don't need type flag
            }
            else
            {
                _Write(writer, obj);
            }
        }

        void _Write(BinaryWriter writer, object obj)
        {
            if (obj == null)
            {
                writer.Write(TypeFlag.NUll);// null
                return;
            }
            else if (_writed_obj_ids.ContainsKey(obj))
            {
                writer.Write(TypeFlag.REUSE);// reuse
                writer.Write(_writed_obj_ids[obj]);
            }
            else
            {
                // new one
                var type = obj.GetType();
                int type_id = GetOrAddTypeId(writer, type);
                if (type_id >= TypeFlag.ID32)
                {
                    writer.Write(TypeFlag.ID32);
                    writer.Write(type_id);
                }
                else
                {
                    writer.Write((byte)type_id);
                }
                if (type.IsValueType == false)
                {
                    _writed_obj_ids.Add(obj, _writed_obj_ids.Count);// Must do before write to avoid loop
                }
                GenerateTypeSerializer(type).Write(this, writer, obj);
            }
        }

        public object InternalRead(BinaryReader reader, Type type)
        {
            if (type.IsValueType)
            {
                return GenerateTypeSerializer(type).Read(this, reader, type);// value type optmize, don't need type flag
            }
            else
            {
                return _Read(reader);
            }
        }

        /// <summary>
        /// add obj to read cache, which is not valuetype
        /// </summary>
        /// <param name="obj"></param>
        public void InternalAddReadObjToCacheList(object obj)
        {
            Debug.Assert(obj.GetType().IsValueType == false);
            _readed_objs.Add(obj);
        }

        object _Read(BinaryReader reader)
        {
            byte flag = reader.ReadByte();
            if (flag == TypeFlag.NUll)
            {
                return null;
            }
            else if(flag == TypeFlag.TYPE)
            {
                ReadType(reader);
                return _Read(reader);
            }
            else if (flag == TypeFlag.REUSE)
            {
                int obj_id = reader.ReadInt32();
                return _readed_objs[obj_id];// 输入数据不对，会发生越界错误
            }
            else
            {
                int type_id = 0;
                if (flag == TypeFlag.ID32)
                {
                    type_id = reader.ReadInt32();
                }
                else
                {
                    type_id = flag;
                }
                // 取得实际类型
                var type = _type_list[type_id];// 输入数据不对，会发生越界错误
                int idx = _readed_objs.Count;// use to check: is class obj add to _readed_objs
                var obj = GenerateTypeSerializer(type).Read(this, reader, type);// 输入数据不对，会发生读dic错误
                if (type.IsValueType == false)
                {
                    if (_readed_objs.Count == idx || _readed_objs[idx] != obj)
                    {
                        throw new XSerializeException("Serializer {0} forget use InternalAddReadObjToCacheList", GenerateTypeSerializer(type));
                    }
                }
                return obj;
            }
        }

        XBinarySerializerBase GenerateTypeSerializer(Type type)
        {
            XBinarySerializerBase serializer;
            if (_type_handle_map.TryGetValue(type, out serializer))
            {
                return serializer;
            }

            serializer = _custom_serializers.FirstOrDefault(h => h.Handles(type));

            if (serializer == null)
            {
                serializer = _inner_serializers.FirstOrDefault(h => h.Handles(type));
            }

            if (serializer == null)
                throw new NotSupportedException(String.Format("No serializer for {0}", type.FullName));

            _type_handle_map.Add(type, serializer);
            return serializer;
        }
    }
}