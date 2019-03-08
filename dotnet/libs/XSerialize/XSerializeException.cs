using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XSerialize
{
    public class XSerializeException : Exception
    {
        public override string Message
        {
            get
            {
                return _info;
            }
        }

        public XSerializeException(string format, params object[] args)
        {
            _info = string.Format(format, args);
        }

        string _info = string.Empty;
    }
}
