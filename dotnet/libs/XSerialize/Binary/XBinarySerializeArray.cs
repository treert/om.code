using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Diagnostics;

/*
 * 1. XXX[]
 * 2. string
 * 3. byte[]
 */
namespace XSerialize.Binary
{
    /*************************** XXX[,,,] *********************************************/
    class XBinarySerializeArray : XBinarySerializerBase
    {
        public override bool Handles(Type type)
        {
            return type.IsArray;
        }

        public override IEnumerable<Type> AddSubtypes(IBinarySerializerForHandle serializer, Type type)
        {
            yield return type.GetElementType();
        }

        public override object Read(IBinarySerializerForHandle serializer, BinaryReader reader, Type type)
        {
            int rank = type.GetArrayRank();
            var element_type = type.GetElementType();
            int[] lengths = new int[rank];
            int[] lowerBounds = new int[rank];
            int[] uperBounds = new int[rank];
            int[] idxs = new int[rank];// for read will use
            bool is_empty = false;
            for (int i = 0; i < rank; ++i)
            {
                lengths[i] = reader.ReadInt32();
                lowerBounds[i] = reader.ReadInt32();
                uperBounds[i] = lengths[i] - 1 + lowerBounds[i];
                idxs[i] = lowerBounds[i];
                is_empty = is_empty || (lengths[i] == 0);
            }
            var arr = Array.CreateInstance(element_type, lengths, lowerBounds);
            serializer.InternalAddReadObjToCacheList(arr);
            if (is_empty)
                return arr;// why c# allow `new int[0,0]`

            for (; ; )
            {
                var val = serializer.InternalRead(reader, element_type);
                arr.SetValue(val, idxs);
                // next
                int dim = rank - 1;
                for (; dim >= 0; --dim)
                {
                    if (idxs[dim] < uperBounds[dim])
                        break;
                }
                if (dim == -1) break;// loop end
                idxs[dim]++;
                for (dim++; dim < rank; ++dim)
                {
                    idxs[dim] = lowerBounds[dim];
                }
            }

            return arr;
        }

        public override void Write(IBinarySerializerForHandle serializer, BinaryWriter writer, object obj)
        {
            Array arr = (Array)obj;
            var type = obj.GetType();
            var element_type = type.GetElementType();
            int rank = type.GetArrayRank();
            int[] lengths = new int[rank];
            int[] lowerBounds = new int[rank];
            int[] uperBounds = new int[rank];
            int[] idxs = new int[rank];// for read will use
            bool is_empty = false;
            for (int i = 0; i < rank; ++i)
            {
                lengths[i] = arr.GetLength(i);
                writer.Write(lengths[i]);
                lowerBounds[i] = arr.GetLowerBound(i);
                writer.Write(lowerBounds[i]);
                uperBounds[i] = arr.GetUpperBound(i);
                idxs[i] = lowerBounds[i];
                is_empty = is_empty || (lengths[i] == 0);
            }
            if (is_empty)
            {
                return ;// why c# allow `new int[0,0]`
            }
            
            for (; ; )
            {
                var val = arr.GetValue(idxs);
                serializer.InternalWrite(writer, val, element_type);
                // next
                int dim = rank - 1;
                for (; dim >= 0; --dim)
                {
                    if (idxs[dim] < uperBounds[dim])
                        break;
                }
                if (dim == -1) break;// loop end
                idxs[dim]++;
                for (dim++; dim < rank; ++dim)
                {
                    idxs[dim] = lowerBounds[dim];
                }
            }
        }
    }

    /*************************** string *********************************************/
    class XBinarySerializeString : XBinarySerializerBase
    {
        public override bool Handles(Type type)
        {
            return typeof(string) == type;
        }

        public override object Read(IBinarySerializerForHandle serializer, BinaryReader reader, Type type)
        {
            var obj = reader.ReadString();
            serializer.InternalAddReadObjToCacheList(obj);
            return obj;
        }

        public override void Write(IBinarySerializerForHandle serializer, BinaryWriter writer, object obj)
        {
            writer.Write((string)obj);
        }
    }

    /*************************** bype[] *********************************************/
    class XBinarySerializeByteArray : XBinarySerializerBase
    {

        public override bool Handles(Type type)
        {
            return typeof(byte[]) == type;
        }

        public override object Read(IBinarySerializerForHandle serializer, BinaryReader reader, Type type)
        {
            int count = reader.ReadInt32();
            var obj = reader.ReadBytes(count);
            serializer.InternalAddReadObjToCacheList(obj);
            return obj;
        }

        public override void Write(IBinarySerializerForHandle serializer, BinaryWriter writer, object obj)
        {
            byte[] data = (byte[])obj;
            writer.Write(data.Length);
            writer.Write(data);
        }
    }
}
