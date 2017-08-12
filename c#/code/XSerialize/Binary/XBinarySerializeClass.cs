using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Diagnostics;
using System.Runtime.Serialization;

/**
 * Class and Struct
 */
namespace XSerialize.Binary
{
    /**
     * 通用类序列化，这个放到最后面
     * 需要设置 XBinarySerializer.m_class_fields
     */
    class XBinarySerializeClass : XBinarySerializerBase
    {
        public override bool Handles(Type type)
        {
            // todo@om 可以考虑加上
            //if(type.IsSerializable)
            //{
            //    throw new Exception(String.Format("Class {0} has not add [Serializable]"));
            //}

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

        public override IEnumerable<Type> AddSubtypes(IBinarySerializerForHandle serializer, Type type)
        {
            if (type.IsAbstract || type.IsInterface)
            {
                yield break;// can not use new Type[0]
            }
            else
            {
                var fields = GetFieldInfos(type);
                var fields_array = fields.ToArray();
                serializer.AddClassFieldInfos(type, fields_array);

                foreach (var field in fields_array)
                {
                    yield return field.FieldType;
                }
            }
        }

        public override object Read(IBinarySerializerForHandle serializer, BinaryReader reader, Type type)
        {
            var fields = serializer.GetClassFieldInfos(type);
            object obj = FormatterServices.GetUninitializedObject(type);
            if(type.IsValueType == false)
            {
                serializer.InternalAddReadObjToCacheList(obj);
            }
            foreach (var field in fields)
            {
                var val = serializer.InternalRead(reader, field.FieldType);
                field.SetValue(obj, val);
            }
            return obj;
        }

        public override void Write(IBinarySerializerForHandle serializer, BinaryWriter writer, object obj)
        {
            var fields = serializer.GetClassFieldInfos(obj.GetType());
            foreach (var field in fields)
            {
                var val = field.GetValue(obj);
                serializer.InternalWrite(writer, val, field.FieldType);
            }
        }

        internal static IEnumerable<FieldInfo> GetFieldInfos(Type type)
        {
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(fi => (fi.Attributes & FieldAttributes.NotSerialized) == 0)// [NonSerialized]
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
