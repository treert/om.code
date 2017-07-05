using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Diagnostics;

/**
 * 二进制序列化工具
 * 
 * Example1:
    using XBinarySerializer = XSerialize.Binary.XBinarySerializer
 
    List<int[]> xx = new List<int[]>() { new int[]{ 0, 1 }, new int[]{ 2, 3 } };
    var serializer = new XBinarySerializer(xx.GetType());
    using(var stream = new MemoryStream())
    {
        serializer.Serialize(stream, xx);
        stream.Seek(0, SeekOrigin.Begin);
        var yy = serializer.Deserialize(stream) as List<int[]>;
        Console.WriteLine(yy[0][1]);// output 1
    }
 * 
 * > 参考 https://github.com/tomba/netserializer
 */
namespace XSerialize.Binary
{
    public class XBinarySerializer : XSerializer
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
            
            new XBinarySerializeString(),
            new XBinarySerializeByteArray(),

            new XBinarySerializeArray(),

            new XBinarySerializeList(),
            new XBinarySerializeDictionary(),

            new XBinarySerializeClass(),
		};

        Type[] _base_types = new Type[] {
            typeof(bool),
            typeof(char),
			typeof(byte), typeof(sbyte),
			typeof(ushort), typeof(short),
			typeof(uint), typeof(int),
			typeof(ulong), typeof(long),
			typeof(float), typeof(double),
			typeof(string),
            typeof(byte[]),
			typeof(DateTime),
			typeof(Decimal),
        };

        // For type save flag
        class TypeFlag
        {
            public static byte NUll = 255;
            public static byte REUSE = 254;
            public static byte ID32 = 253;// 后面一个int32是typeid
            //[0,252] short typeid
        }

        List<Type> _type_list = new List<Type>();
        Dictionary<Type, int> _type_id_map = new Dictionary<Type, int>();
        Dictionary<Type, XBinarySerializerBase> _type_handle_map = new Dictionary<Type, XBinarySerializerBase>();
        int _type_list_hash = 0;

        public XBinarySerializer(params Type[] types)
        {
            ResetTypes(types);
        }

        public override void ResetTypes(params Type[] types)
        {
            _type_handle_map.Clear();
            _type_id_map.Clear();
            _type_list.Clear();

            _AddTypesWithoutHash(_base_types);
            _AddTypesWithoutHash(types);

            OrderTypeAndCalculateHash();
        }

        public override void AddTypes(params Type[] types)
        {
            _AddTypesWithoutHash(types);

            OrderTypeAndCalculateHash();
        }

        void _AddTypesWithoutHash(Type[] types)
        {
            var stack = new Stack<Type>(types);
            while(stack.Count > 0)
            {
                var type = stack.Pop();

                if (_type_handle_map.ContainsKey(type))
                    continue;

                //if (type.IsAbstract || type.IsInterface)
                //    throw new Exception(String.Format("Type {0} can not be serialized", type.FullName));

                if (type.ContainsGenericParameters)
                    throw new Exception(String.Format("Type {0} contains generic parameters", type.FullName));

                XBinarySerializerBase serializer = GetTypeSerializer(type);

                _type_handle_map[type] = serializer;
                _type_list.Add(type);

                foreach(var t in serializer.AddSubtypes(this, type))
                {
                    if (_type_handle_map.ContainsKey(t) == false)
                        stack.Push(t);
                }
            }
        }

        void OrderTypeAndCalculateHash()
        {
            var tmp = _type_list.OrderBy(t => t.ToString());
            _type_list = new List<Type>();
            _type_id_map.Clear();
            int type_id = 0;
            foreach (var type in tmp)
            {
                _type_list.Add(type);
                _type_id_map[type] = type_id++;
            }

            CalculateHash();
        }

        void CalculateHash()
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                foreach(Type type in _type_list)
                {
                    writer.Write(type.ToString());// use FullName ??
                }
                foreach(var serializer in _custom_serializers)
                {
                    writer.Write(serializer.GetType().ToString());
                }

                var sha256 = System.Security.Cryptography.SHA256.Create();
                var bytes = sha256.ComputeHash(stream);

                _type_list_hash = (bytes[0] << 24) | (bytes[1] << 16) | (bytes[2] << 8) | (bytes[3]);
            }
        }

        public override void Serialize(Stream stream, object obj)
        {
            _writed_obj_ids.Clear();
            BinaryWriter writer = new BinaryWriter(stream);
            writer.Write(_type_list_hash);
            _Write(writer, obj);
            _writed_obj_ids.Clear();
        }

        public override object Deserialize(Stream stream)
        {
            _readed_objs.Clear();
            BinaryReader reader = new BinaryReader(stream);
            if(_type_list_hash != reader.ReadInt32())
            {
                throw new Exception("type list hash is error, check ResetTypes And AddTypes");
            }
            var obj = _Read(reader);
            _readed_objs.Clear();
            return obj;
        }

        // For class reuse, object born num
        Dictionary<object, int> _writed_obj_ids = new Dictionary<object, int>();// 【不需要传入比较器，这儿会使用Object.Equals来比较，相当于 == 】
        List<object> _readed_objs = new List<object>();

        internal void InternalWrite(BinaryWriter writer, object obj, Type type)
        {
            if(type.IsValueType)
            {
                _type_handle_map[type].Write(this, writer, obj);// value type optmize, don't need type flag
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
                int type_id = 0;
                if (!_type_id_map.TryGetValue(type, out type_id))
                {
                    throw new Exception(String.Format("Unkown type {0}", type));
                }
                if (type_id >= TypeFlag.ID32)
                {
                    writer.Write(TypeFlag.ID32);
                    writer.Write(type_id);
                }
                else
                {
                    writer.Write((byte)type_id);
                }
             
                _type_handle_map[type].Write(this, writer, obj);
                _writed_obj_ids.Add(obj, _writed_obj_ids.Count);// 放在后面，和read对应
            }
        }

        internal object InternalRead(BinaryReader reader, Type type)
        {
            if (type.IsValueType)
            {
                return _type_handle_map[type].Read(this, reader, type);// value type optmize, don't need type flag
            }
            else
            {
                return _Read(reader);
            }
        }

        object _Read(BinaryReader reader)
        {
            byte flag = reader.ReadByte();
            if(flag == TypeFlag.NUll)
            {
                return null;
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
                var obj = _type_handle_map[type].Read(this, reader, type);// 输入数据不对，会发生读dic错误
                _readed_objs.Add(obj);
                return obj;
            }
        }

        XBinarySerializerBase GetTypeSerializer(Type type)
        {
            var serializer = _custom_serializers.FirstOrDefault(h => h.Handles(type));

            if(serializer == null)
            {
                serializer = _inner_serializers.FirstOrDefault(h => h.Handles(type));
            }

            if (serializer == null)
                throw new NotSupportedException(String.Format("No serializer for {0}", type.FullName));

            return serializer;
        }
    }

    /***************************************************************/

    abstract class XBinarySerializerBase
    {
        /// <summary>
        /// Returns if this TypeSerializer handles the given type
        /// </summary>
        public abstract bool Handles(Type type);

        /// <summary>
        /// Return types that are needed to serialize the given type
        /// </summary>
        public virtual IEnumerable<Type> AddSubtypes(XBinarySerializer serializer, Type type)
        {
            yield break;
            //return new Type[0];
        }

        public abstract object Read(XBinarySerializer serializer, BinaryReader reader, Type type);
        public abstract void Write(XBinarySerializer serializer, BinaryWriter writer, object obj);
    }
}
