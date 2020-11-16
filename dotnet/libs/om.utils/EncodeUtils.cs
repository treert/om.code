using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace om.utils
{
    public static class EncodeUtils
    {

        static List<(Encoding code, byte[] bom, string name)> s_bom_list = new List<(Encoding, byte[], string)>()
        {
            (new UTF8Encoding(true, true), new byte[] { 0xEF, 0xBB, 0xBF }, "utf-8-bom"),
            (new UTF32Encoding(false, true, true), new byte[]{0xFF, 0xFE,0,0}, "utf-32"),
            (new UTF32Encoding(true, true, true), new byte[]{0,0,0xFE, 0xFF}, "utf-32BE"),
            (new UnicodeEncoding(false,true,true), new byte[]{0xFF, 0xFE}, "utf-16"),
            (new UnicodeEncoding(true,true,true), new byte[]{0xFE,0xFF}, "utf-16BE"),
        };

        public static Encoding GetEncodingByName(string name)
        {
            name = name.ToLower().Replace("-","").Trim();
            switch (name)
            {
                case "utf8":
                    return new UTF8Encoding(false, true);
                case "utf8bom":
                    return new UTF8Encoding(true, true);
                case "utf16":
                case "utf16le":
                case "utf16small":
                    return new UnicodeEncoding(false, true, true);
                case "utf16be":
                case "utf16big":
                    return new UnicodeEncoding(true, true, true);
                case "utf32":
                case "utf32le":
                case "utf32small":
                    return new UTF32Encoding(false, true, true);
                case "utf32be":
                case "utf32big":
                    return new UTF32Encoding(true, true, true);
                default:
                    return null;
            }
        }

        public static (Encoding, string) DetectEncodeByFileName(string filepath)
        {
            using(var fs = new FileStream(filepath, FileMode.Open, FileAccess.Read))
            {
                return DetectEncode(fs);
            }
        }

        public static (Encoding,string) DetectEncode(Stream fs)
        {
            // check bom if has
            {
                fs.Seek(0, SeekOrigin.Begin);
                byte[] bom = new byte[4];
                int len = fs.Read(bom, 0, bom.Length);
                foreach (var it in s_bom_list)
                {
                    if (len >= it.bom.Length)
                    {
                        bool eq = bom.Take(it.bom.Length).SequenceEqual(it.bom);
                        if (eq)
                        {
                            return (it.code, it.name);
                        }
                    }
                }
            }

            byte[] data = fs.ReadAllBytes();
            // check utf-8
            {
                if (IsUTF8Bytes(data))
                {
                    return (new UTF8Encoding(false, true), "utf-8");
                }
            }
            // try GB18030，统一处理下 gb2312 gbk之类的。error happend:No data is available for encoding 54936.
            // try Defautl
            {
                var code = Encoding.Default;
                if (code.WebName.StartsWith("utf-8") != true)
                {
                    try
                    {
                        code.GetString(data);
                        return (code, code.EncodingName);
                    }
                    catch
                    {

                    }
                }
            }
            return (null,"unkown");
        }

        // https://www.cnblogs.com/cyberarmy/p/5652835.html
        public static bool IsUTF8Bytes(byte[] data, bool is_whole = true)
        {
            int charByteCounter = 1; //计算当前正分析的字符应还有的字节数
            byte curByte; //当前分析的字节.
            for (int i = 0; i < data.Length; i++)
            {
                curByte = data[i];
                if (charByteCounter == 1)
                {
                    if (curByte >= 0x80)
                    {
                        //判断当前
                        while (((curByte <<= 1) & 0x80) != 0)
                        {
                            charByteCounter++;
                        }
                        //标记位首位若为非0 则至少以2个1开始 如:110XXXXX...........1111110X
                        if (charByteCounter == 1 || charByteCounter > 6)
                        {
                            return false;
                        }
                    }
                }
                else
                {
                    //若是UTF-8 此时前两位必须为10
                    if ((curByte & 0xC0) != 0x80)
                    {
                        return false;
                    }
                    charByteCounter--;
                }
            }
            if (charByteCounter > 1)
            {
                // 最后一个字符不完整，可能是被截断的，当成OK。
                return !is_whole;
            }
            return true;
        }
    }
}
