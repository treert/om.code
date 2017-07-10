using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Diagnostics;

using XSerialize.Binary;
using XSerialize.Xml;

namespace XSerialize
{
    public class XBinarySerializer
    {
        XBinarySerializerInternal _serializer = null;
        public XBinarySerializer(params Type[] types)
        {
            _serializer = new XBinarySerializerInternal(types);
        }

        public void ResetTypes(params Type[] types)
        {
            _serializer.ResetTypes(types);
        }

        public void AddTypes(params Type[] types)
        {
            _serializer.AddTypes(types);
        }

        public void Serialize(Stream stream, object obj)
        {
            _serializer.Serialize(stream, obj);
        }

        public T Deserialize<T>(Stream stream)
        {
            return (T)Deserialize(stream);
        }

        public object Deserialize(Stream stream)
        {
            return _serializer.Deserialize(stream);
        }
    }

    public class XBinarySerializer2 : XSerializeSingleton<XBinarySerializer2>
    {
        XBinarySerializerInternal2 _serializer = null;
        public XBinarySerializer2()
        {
            _serializer = new XBinarySerializerInternal2();
        }

        public void Serialize(Stream stream, object obj)
        {
            _serializer.Serialize(stream, obj);
        }

        public T Deserialize<T>(Stream stream)
        {
            return (T)Deserialize(stream);
        }

        public object Deserialize(Stream stream)
        {
            return _serializer.Deserialize(stream);
        }
    }

    public class XXmlSerializer : XSerializeSingleton<XXmlSerializer>
    {
        XXmlSerializerInternal _serializer = null;
        public XXmlSerializer()
        {
            _serializer = new XXmlSerializerInternal();
        }

        public string SerializeToString(object obj)
        {
            if (obj == null)
            {
                //throw new XSerializeException("param obj can not be null");
                return SerializeToString(null, typeof(object));
            }
            else
            {
                return SerializeToString(obj, obj.GetType());
            }
        }

        public string SerializeToString<T>(object obj)
        {
            return SerializeToString(obj, typeof(T));
        }

        public string SerializeToString(object obj, Type type)
        {
            return _serializer.SerializeToString(obj, type);
        }

        public T DeserializeFromString<T>(string str)
        {
            return (T)DeserializeFromString(str, typeof(T));
        }

        public object DeserializeFromString(string str, Type type)
        {
            return _serializer.DeserializeFromString(str, type);
        }
    }

    public class XXmlDump : XSerializeSingleton<XXmlDump>
    {
        XXmlDumpInternal _serializer = null;
        public XXmlDump()
        {
            _serializer = new XXmlDumpInternal();
        }

        public string Dump(object obj)
        {
            return _serializer.Dump(obj);
        }
    }


    public class XSerializeSingleton<T> where T:new()
    {
        static T _instance = default(T);
        public static T singleton
        {
            get
            {
                if(_instance == null)
                {
                    _instance = new T();
                }
                return _instance;
            }
        }
    }
}
