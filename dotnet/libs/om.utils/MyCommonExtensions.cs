using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace om.utils
{
    public static class MyCommonExtensions
    {
        public static byte[] ReadAllBytes(this Stream instream)
        {
            if (instream.CanSeek)
            {
                instream.Seek(0, SeekOrigin.Begin);
            }
            if (instream is MemoryStream mem)
                return mem.ToArray();

            using (var memoryStream = new MemoryStream())
            {
                instream.CopyTo(memoryStream);
                return memoryStream.ToArray();
            }
        }

        public static bool IsNullOrEmpty(this string str)
        {
            return string.IsNullOrEmpty(str);
        }
    }
}
