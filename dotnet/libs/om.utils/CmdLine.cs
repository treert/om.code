/*
 * 1.0 
 * 
 * 
 */

using _Internal.CmdLine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

/// <summary>
/// > https://github.com/commandlineparser/commandline
/// 小轮子，自己简单用用
/// 
/// 基本格式：
///     cmdline => command {option} [-] [args]
///     cmdline => command $cmdline
///     option => -name args
///     args => {string}
///     name := [a-zA-Z][-_.0-9a-zA-Z]+
///     string := [^-]\S*
/// 
/// 说明：
/// 参数：命令之后就是参数了，参数不能是 - 开头，不然会被识别成选项。【转义什么的后续再看吧】
///     - 命令参数和选项参数连在一起的情况下，可以用单独一个 - 分隔下
///        - 逻辑上支持选项和参数混合，但是最好不要这么做
/// 参数类型：
///     - 基础类型：string bool number(int double ...)
///     - 可以是基础类型的数组、字典
///     - 参数范围检测【todo，这个先不管了，后续看看要不要加】
/// 选项格式：
///     - name支持多种别名，缩写之类，但是相互间不能冲突。
///     - 特殊的name可以出现多次，可以用来传入数组什么的。【并建议这么用，数组行参数可以用空格分开】
///     - 不分大小写，防止写乱了
///     - 如果name为空，可以用来任意的插入参数【建议相同选项连续，参数连续，不要乱在里面】
/// 命令组：
///     - 支持命令组，方便逻辑归类，类似git的 `git clone` `git checkout`
///     
/// 实现细节：
///     - 参数可以认为是一类特殊的选项，Option名字为空的当成参数。
///     - CmdBase 子类里的字段就是选项，除了m_args外，其他最好直接是选项名，不需要 m_ 开头。
///     - OptionAttribute 用来具体设置选项名，选项缩写名，选项帮助tip
/// </summary>
namespace om.utils
{
    public interface ICmd
    {
        void Exec();
    }

    public interface ICmdParser
    {
        ICmd Parse(IEnumerable<string> args);
        void PrintHelp(string cmd_prefix = "");
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    public class OptionAttribute:Attribute
    {
        public string name;
        public string alias;// 别名，可以用于提供一个缩写名
        public string tip;// 自定义帮助说明
        public bool required = false;

        public OptionAttribute(string name){
            if (name == null) throw new ArgumentNullException("name");

            this.name = name.Trim().ToLower();
        }
    }

    public abstract class CmdSerializeBase
    {
        public virtual bool CanHandle(Type type) { return false; }
        public virtual void Merge(ref object obj, object old) { }
        public virtual object Parse(MyItor<string> itor, Type type)
        {
            if(itor.HasValue == false || itor.Current.StartsWith("-"))
            {
                throw new MyException("need input");
            }
            var obj = _Parse(itor, type);
            itor.MoveNext();
            return obj;
        }
        // itor stop at last input for this option
        public abstract object _Parse(MyItor<string> itor, Type type);
        public abstract string GetTypeName(Type type);
    }

    public class CmdLine
    {
    }

    /// <summary>
    /// 1.0 
    /// 不支持扩展，只接受指定的类型
    /// 容器类型只支持少数几个。Array，List
    /// </summary>
    public static class CmdSerializeMgr
    {
        // 不要作死
        public static Dictionary<Type, CmdSerializeBase> m_sp_handlers;// 主要是给 Primitives类型，可以扩充给具体类型
        public static List<CmdSerializeBase> m_other_handlers;// 主要是给泛型或者数组类的容器

        static CmdSerializeMgr()
        {
            m_sp_handlers = new Dictionary<Type, CmdSerializeBase>()
            {
                {typeof(bool),new SerializeBoolean()},
                {typeof(char),new SerializeChar() },
                {typeof(byte),new SerializeByte() },
                {typeof(sbyte),new SerializeSByte() },
                {typeof(short),new SerializeInt16() },
                {typeof(ushort),new SerializeUInt16() },
                {typeof(int),new SerializeInt32() },
                {typeof(uint),new SerializeUInt32() },
                {typeof(long),new SerializeInt64() },
                {typeof(ulong),new SerializeUInt64() },
                {typeof(float),new SerializeSingle() },
                {typeof(double),new SerializeDouble() },
                {typeof(decimal), new SerializeDecimal() },
                {typeof(DateTime), new SerializeDateTime() },
            };

            m_other_handlers = new List<CmdSerializeBase>()
            {
                new SerializeEnum(),
            };
        }

        public static CmdSerializeBase FindHandler(Type type)
        {
            if (m_sp_handlers.ContainsKey(type))
            {
                return m_sp_handlers[type];
            }

            var handler = m_other_handlers.Find(h => h.CanHandle(type));
            return handler;
        }
    }
}


namespace _Internal.CmdLine
{
    using om.utils;
    #region Parse
    class OptionParser
    {
        public string name;
        public string alias;
        public string tip;
        public bool required;

        public bool m_has_input = false;

        public string TypeNameForPrint
        {
            get { return m_handler.GetTypeName(m_type); }
        }

        Type m_type;
        CmdSerializeBase m_handler;

        FieldInfo m_field;
        PropertyInfo m_property;

        public OptionParser(FieldInfo field)
        {
            m_field = field;
            InitType(field.FieldType);
            InitOptionInfo(field);
        }

        public OptionParser(PropertyInfo property)
        {
            m_property = property;
            InitType(property.PropertyType);
            InitOptionInfo(property);
        }

        void InitOptionInfo(MemberInfo member)
        {
            var option = member.GetCustomAttribute<OptionAttribute>();
            if (option == null)
            {
                // 应该只有field会走到这儿来
                name = member.Name.ToLower();
                if (name.StartsWith("m_")) name = name.Substring(2);
                name = name.Trim('_').Replace('_', '-');
            }
            else
            {
                name = option.name.ToLower();
                alias = option.alias;
                tip = option.tip;
                required = option.required;
            }
            if (alias != null) alias = alias.ToLower();
            if (string.IsNullOrWhiteSpace(alias) || alias == name) alias = null;
            tip = tip ?? "";
        }

        void InitType(Type type)
        {
            m_type = type;
            m_handler = CmdSerializeMgr.FindHandler(type);
            if (m_handler == null)
                throw new NotSupportedException($"Can not Parse {type}");
        }

        public void Parse(object obj, MyItor<string> args)
        {
            //if(m_has_input){
            //    if(name == ""){
            //        throw new Exception("args duplication");
            //    }
            //    else{
            //        throw new Exception($"option -{name} duplication");
            //    }
            //}

            var val = m_handler.Parse(args, m_type);// 格式错误让handle抛异常好了
            if (m_has_input)
            {
                var old = m_field != null ? m_field.GetValue(obj) : m_property.GetValue(obj);
                m_handler.Merge(ref val, old);
            }

            m_has_input = true;
            if (m_field != null)
            {
                m_field.SetValue(obj, val);
            }
            else
            {
                m_property.SetValue(obj, val);
            }
        }
    }

    class CmdParser : ICmdParser
    {
        public Dictionary<string, OptionParser> m_options;
        public string name;
        public string alias;
        public string tip;

        public Type m_type;

        public CmdParser(Type type)
        {
            m_type = type;
            var option = type.GetCustomAttribute<OptionAttribute>();
            if (option == null)
            {
                name = type.Name.ToLower();
                if (name.EndsWith("cmd")) name = name.Substring(0, name.Length - 3);
            }
            else
            {
                name = option.name.ToLower();
                alias = option.alias;
                tip = option.tip;
            }

            if (string.IsNullOrWhiteSpace(name)) throw new NotSupportedException($"cmd name can not be empty, cmd type is {type.Name}");
            if (alias != null) alias = alias.ToLower();
            if (string.IsNullOrWhiteSpace(alias) || alias == name) alias = null;
            tip = tip ?? "";

            InitOptions(type);
        }

        void InitOptions(Type type)
        {
            m_options = new Dictionary<string, OptionParser>();
            foreach (var field in type.GetFields())
            {
                var option = new OptionParser(field);
                m_options.Add(option.name, option);
                if (option.alias != null)
                {
                    m_options.Add(option.alias, option);
                }
            }
            foreach (var property in type.GetProperties())
            {
                if (property.GetCustomAttribute<OptionAttribute>() != null)
                {
                    if (property.GetIndexParameters().Length == 0)
                    {
                        throw new NotSupportedException($"do not support indexed property, {property.DeclaringType.Name}.{property.Name}");
                    }
                    var option = new OptionParser(property);
                    m_options.Add(option.name, option);
                    if (option.alias != null)
                    {
                        m_options.Add(option.alias, option);
                    }
                }
            }
        }

        internal ICmd Parse(ICmd obj, MyItor<string> itor)
        {
            while (itor.HasValue)
            {
                string name = itor.Current;
                if (name.StartsWith("-"))
                {
                    name = name.Substring(1).ToLower();
                    itor.MoveNext();
                }
                else
                {
                    name = "";
                }
                OptionParser option;
                if (m_options.TryGetValue(name, out option))
                {
                    try
                    {
                        option.Parse(obj, itor);
                    }
                    catch (Exception e)
                    {
                        throw new MyException($"parse error -{option.name} {option.tip}\n {e.Message}");
                    }
                }
                else
                {
                    throw new MyException($"unkown option -{name}");
                }
            }
            foreach (var item in m_options.Values)
            {
                if (item.required && item.m_has_input == false)
                {
                    throw new MyException($"miss requied option -{item.name} {item.tip}");
                }
            }
            return obj;
        }

        public ICmd Parse(IEnumerable<string> args)
        {
            var itor = new MyItor<string>(args);
            if (itor.HasValue && itor.Current == "-?")
            {
                PrintHelp(name);
                return null;
            }
            var obj = (ICmd)Activator.CreateInstance(m_type, true);
            try
            {
                Parse(obj, itor);
            }
            catch (MyException e)
            {
                Console.WriteLine(e.Message);
                return null;
            }
            return obj;
        }

        public void PrintHelp(string cmd_prefix = "")
        {
            if (string.IsNullOrWhiteSpace(cmd_prefix) == false)
            {
                Console.WriteLine($"Use: {cmd_prefix} [args] [options]");
                Console.WriteLine();
            }

            OptionParser args_option;
            if (m_options.TryGetValue("", out args_option))
            {
                Console.WriteLine("args:");
                Console.Write(args_option.required ? "* " : "  ");
                Console.WriteLine($"{args_option.TypeNameForPrint}  {args_option.tip}");
                Console.WriteLine();
            }

            Console.WriteLine("options:");
            var print = new CmdPrintLineTool();
            foreach (var option in m_options.Values.Distinct().OrderBy(p => p.name))
            {
                if (args_option == option) continue;

                string name = option.name;
                if (option.alias != null) name += "|-" + option.alias;
                string type = option.TypeNameForPrint;
                if (option.required)
                {
                    print.Add($"* -{name} {type}", option.tip);
                }
                else
                {
                    print.Add($"  -{name} {type}", option.tip);
                }
            }
            print.Print();
            Console.WriteLine();
        }
    }

    class CmdGroupParser : ICmdParser
    {
        Type m_type;
        CmdParser m_parser;
        Dictionary<string, CmdGroupParser> m_sub_parsers = new Dictionary<string, CmdGroupParser>();

        public void SetCmd<T>() where T : ICmd
        {
            m_type = typeof(T);
            m_parser = new CmdParser(m_type);
        }

        public void AddSubCmd<T>() where T : ICmd
        {
            var g = new CmdGroupParser();
            g.SetCmd<T>();
            m_sub_parsers.Add(g.m_parser.name, g);
            if (g.m_parser.alias != null) m_sub_parsers.Add(g.m_parser.alias, g);
        }

        public void AddSubGroup(CmdGroupParser group)
        {
            if (group.m_parser == null)
            {
                // 这种情况下，找到所有子命令，加入。sub group de m_cmd_type 不能为空。
                foreach (var item in group.m_sub_parsers)
                {
                    m_sub_parsers.Add(item.Key, item.Value);
                }
            }
        }

        public ICmd Parse(IEnumerable<string> args)
        {
            var itor = new MyItor<string>(args);
            // get type
            CmdGroupParser g = this;
            List<string> cmds = new List<string>();
            while (itor.HasValue)
            {
                var val = itor.Current.ToLower();
                if (g.m_sub_parsers.ContainsKey(val))
                {
                    g = g.m_sub_parsers[val];
                    cmds.Add(val);
                    itor.MoveNext();
                }
            }
            if (itor.HasValue && itor.Current == "-?")
            {
                g.PrintHelp(string.Join(" ", cmds));
                return null;
            }
            if (g.m_parser == null)
            {
                Console.WriteLine("need subcmd");
                g.PrintHelp(string.Join(" ", cmds));
                return null;
            }

            var obj = (ICmd)Activator.CreateInstance(g.m_parser.m_type, true);
            try
            {
                g.m_parser.Parse(obj, itor);
            }
            catch (MyException e)
            {
                Console.WriteLine(e.Message);
                return null;
            }

            return obj;
        }

        public void PrintHelp(string cmd_prefix = "")
        {
            if (m_sub_parsers.Count == 0)
            {
                m_parser?.PrintHelp(cmd_prefix);
                return;
            }
            cmd_prefix = cmd_prefix ?? "";
            if (m_parser != null)
            {
                m_parser.PrintHelp($"{cmd_prefix} [subcmd]");
            }
            else
            {
                Console.WriteLine($"Use: {cmd_prefix} <subcmd> [args] [options]");
                Console.WriteLine();
            }

            Console.WriteLine("subcmds:");
            var print = new CmdPrintLineTool();
            foreach (var cmd in m_sub_parsers.Values.Distinct().OrderBy(p => p.m_parser.name))
            {
                var name = cmd.m_parser.name;
                if (cmd.m_parser.alias != null) name += "|" + cmd.m_parser.alias;
                print.Add($"  {name}", cmd.m_parser.tip);
            }

            print.Print();
            Console.WriteLine();
        }
    }

    #endregion
    class CmdPrintLineTool
    {
        List<string> m_headers = new List<string>();
        List<string> m_tails = new List<string>();

        public void Add(string header, string tail)
        {
            m_headers.Add(header);
            m_tails.Add(tail);
        }

        public void Print()
        {
            int max_length = 20;
            m_headers.ForEach(h => max_length = Math.Max(max_length, h.Length + 2));

            for (int i = 0; i < m_headers.Count; i++)
            {
                Console.Write(m_headers[i]);
                int cnt = m_headers[i].Length;
                while (cnt++ < max_length)
                {
                    Console.Write(' ');
                }
                Console.WriteLine(m_tails[i]);
                //Console.WriteLine($"{m_headers[i],max_length}");
            }
        }

        public void Clear()
        {
            m_headers.Clear();
            m_tails.Clear();
        }
    }



    #region special type serializer
    class SerializeBoolean : CmdSerializeBase
    {
        public override string GetTypeName(Type type)
        {
            return "bool";
        }

        public override object Parse(MyItor<string> itor, Type type)
        {
            if (itor.HasValue && itor.Current.StartsWith("-") == false)
            {
                var obj = _Parse(itor, type);
                itor.MoveNext();
                return obj;
            }
            return true;// default
        }

        public override object _Parse(MyItor<string> itor, Type type)
        {
            return Convert.ToBoolean(itor.Current);
        }
    }

    class SerializeChar : CmdSerializeBase
    {
        public override string GetTypeName(Type type)
        {
            return "char";
        }

        public override object _Parse(MyItor<string> itor, Type type)
        {
            return Convert.ToChar(itor.Current);
        }
    }

    class SerializeByte : CmdSerializeBase
    {
        public override string GetTypeName(Type type)
        {
            return "byte";
        }

        public override object _Parse(MyItor<string> itor, Type type)
        {
            return Convert.ToByte(itor.Current);
        }
    }

    class SerializeSByte : CmdSerializeBase
    {
        public override string GetTypeName(Type type)
        {
            return "sbyte";
        }

        public override object _Parse(MyItor<string> itor, Type type)
        {
            return Convert.ToSByte(itor.Current);
        }
    }

    class SerializeInt16 : CmdSerializeBase
    {
        public override string GetTypeName(Type type)
        {
            return "int16";
        }

        public override object _Parse(MyItor<string> itor, Type type)
        {
            return Convert.ToInt16(itor.Current);
        }
    }

    class SerializeUInt16 : CmdSerializeBase
    {
        public override string GetTypeName(Type type)
        {
            return "uint16";
        }

        public override object _Parse(MyItor<string> itor, Type type)
        {
            return Convert.ToUInt16(itor.Current);
        }
    }

    class SerializeInt32 : CmdSerializeBase
    {
        public override string GetTypeName(Type type)
        {
            return "int32";
        }

        public override object _Parse(MyItor<string> itor, Type type)
        {
            return Convert.ToInt32(itor.Current);
        }
    }

    class SerializeUInt32 : CmdSerializeBase
    {
        public override string GetTypeName(Type type)
        {
            return "uint32";
        }

        public override object _Parse(MyItor<string> itor, Type type)
        {
            return Convert.ToUInt32(itor.Current);
        }
    }

    class SerializeInt64 : CmdSerializeBase
    {
        public override string GetTypeName(Type type)
        {
            return "int64";
        }

        public override object _Parse(MyItor<string> itor, Type type)
        {
            return Convert.ToInt64(itor.Current);
        }
    }

    class SerializeUInt64 : CmdSerializeBase
    {
        public override string GetTypeName(Type type)
        {
            return "uint64";
        }

        public override object _Parse(MyItor<string> itor, Type type)
        {
            return Convert.ToUInt64(itor.Current);
        }
    }

    class SerializeSingle : CmdSerializeBase
    {
        public override string GetTypeName(Type type)
        {
            return "float";
        }

        public override object _Parse(MyItor<string> itor, Type type)
        {
            return Convert.ToSingle(itor.Current);
        }
    }

    class SerializeDouble : CmdSerializeBase
    {
        public override string GetTypeName(Type type)
        {
            return "double";
        }

        public override object _Parse(MyItor<string> itor, Type type)
        {
            return Convert.ToDouble(itor.Current);
        }
    }

    class SerializeDecimal : CmdSerializeBase
    {
        public override string GetTypeName(Type type)
        {
            return "decimal";
        }

        public override object _Parse(MyItor<string> itor, Type type)
        {
            return Convert.ToDecimal(itor.Current);
        }
    }

    class SerializeDateTime : CmdSerializeBase
    {
        public override string GetTypeName(Type type)
        {
            return "DateTime";
        }

        public override object _Parse(MyItor<string> itor, Type type)
        {
            // todo, 格式差不多是：yyyy-MM-dd HH:mm:ss
            return Convert.ToDateTime(itor.Current);
        }
    }

    class SerializeString : CmdSerializeBase
    {
        public override string GetTypeName(Type type)
        {
            return "string";
        }

        public override object _Parse(MyItor<string> itor, Type type)
        {
            return itor.Current;
        }
    }

    class SerializeBytes : CmdSerializeBase
    {
        public override string GetTypeName(Type type)
        {
            return "Base64.byte[]";// base64 format
        }

        public override object _Parse(MyItor<string> itor, Type type)
        {
            return Convert.FromBase64String(itor.Current);
        }
    }

    #endregion

    #region serial types
    class SerializeEnum : CmdSerializeBase
    {
        public override bool CanHandle(Type type)
        {
            if (type.IsEnum)
            {
                var under = Enum.GetUnderlyingType(type);
                return CmdSerializeMgr.m_sp_handlers.ContainsKey(under);
            }
            return false;
        }
        public override string GetTypeName(Type type)
        {
            return "Enum." + type.Name;
        }

        public override object _Parse(MyItor<string> itor, Type type)
        {
            var under = Enum.GetUnderlyingType(type);
            var handler = CmdSerializeMgr.m_sp_handlers[under];
            return handler._Parse(itor, type);
        }
    }

    #endregion
}