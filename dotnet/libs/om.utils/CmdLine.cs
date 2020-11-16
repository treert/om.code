/*
 * 1.0 
例子：
    class TestVal
    {
        string name;
        public TestVal(string str) { name = str; }
        public static TestVal ParseFromString(string str)
        {
            return new TestVal(str);
        }
        public override string ToString()
        {
            return name;
        }
    }
    [Option("test", tip = "Test command")]
    class TestCmd:CmdLine.ICmd
    {
        [Option("",tip = "Args is a int")]
        public int num;
        [Option("help")]
        public bool help;
        [Option("val")]
        public TestVal val;
        [Option("message",alias = "m", required = true, tip = "Test option message")]
        public string Message { get; set; }

        public void Exec()
        {
            Console.WriteLine($"input {help} {num} {Message} {val}");
        }
    }
    // ... in main
    var cmd_parser = CmdLine.CreateCmdParser<TestCmd>();
    cmd_parser.Parse(args)?.Exec();
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
///     name := [a-zA-Z]\S*
///     string := ([^-]\S* | -[^a-zA-Z]\S*)
/// 
/// 说明：
/// 参数：参数不能是 -[a-zA-Z] 开头，不然会被识别成选项。【todo 转义什么的后续再看吧】
///     - 命令参数和选项参数连在一起的情况下，可以用单独一个 - 分隔下
///        - 逻辑上支持选项和参数混合，但是不建议不要使用
/// 参数类型：
///     - 基础类型：所有C#支持的基础类型，DateTime，decimal
///     - 基础类型的Array、List、Dictionary(Map)
///     - 参数内容检测【todo 这个先不管了，后续看看要不要加】
/// 选项格式：
///     - name支持多种别名，缩写之类，但是相互间不能冲突。
///     - 特殊的name可以出现多次，可以用来传入数组什么的。【不建议这么用】
///         - 数组行参数可以用空格分开
///         - Map类的，每个key-value的格式是 key=value
///     - 选项name不分大小写，防止写乱了。
///     - 如果name为空，可以用来任意的插入参数【建议相同选项连续，参数连续，不要乱在里面】
/// 命令组：
///     - 支持命令组，方便逻辑归类，类似git的 `git clone` `git checkout`
///     
/// 实现细节：
///     - 命令需要继承CmdLine.ICmd，其中的public field 和被 OptionAttribute 修饰的property 会被提取出来当作选项
///         - 使用一个Exec接口，用来运行
///     - 两个核心类
///         - OptionAttribute 用来具体设置选项名，选项缩写名，选项帮助tip
///             - 参数可以认为是一类特殊的选项，Option名字为空的当成参数。
///             - 选项输入 -？ 默认用作输出帮助信息的，集成在代码逻辑里。
///         - CmdLine 静态类，所有的接口都在这儿
///             - ICmdParser 提供连个接口 Parse 和 PrintHelp
///             - 两个工厂创建方法 CreateCmdParser 和 CreateGroupParser，据说这样写比较好
///                 - CmdGroupParser提供SetCmd/SetSubCmd/SetSubGroup来组装命令组
///     - 扩展。不支持容器扩展，但是支持单字符串序列化扩展，只要自定义的类实现`static ParseFromString(string str)`就行，权限和返回值不敏感。
///               
/// </summary>
namespace om.utils
{
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

    public static class CmdLine
    {
        public static CmdParser CreateCmdParser<T>()where T:ICmd
        {
            var p = new CmdParser(typeof(T));
            return p;
        }

        public static CmdParser CreateCmdParser(Type type)
        {
            if (typeof(ICmd).IsAssignableFrom(type))
            {
                var p = new CmdParser(type);
                return p;
            }
            throw new ArgumentException($"{type} has not implement ICmd");
        }

        public static CmdGroupParser CreateGroupParser()
        {
            return new CmdGroupParser();
        }

        public interface ICmdParser
        {
            ICmd Parse(IEnumerable<string> args);
            // 暴露出来，可能有好处
            void PrintHelp(string cmd_prefix = "");
        }

        public class CmdParser:ICmdParser
        {
            InnerCmdParser m_parser;

            internal CmdParser(Type type)
            {
                m_parser = new InnerCmdParser(type);
            }
            
            public ICmd Parse(IEnumerable<string> args)
            {
                return (ICmd)m_parser.Parse(args);
            }
            public void PrintHelp(string cmd_prefix = "")
            {
                m_parser.PrintHelp(cmd_prefix);
            }
        }

        public class CmdGroupParser: ICmdParser
        {
            InnerCmdGroupParser m_parser = new InnerCmdGroupParser();

            internal CmdGroupParser() { }

            public ICmd Parse(IEnumerable<string> args)
            {
                return (ICmd)m_parser.Parse(args);
            }

            public void PrintHelp(string cmd_prefix = "")
            {
                m_parser.PrintHelp(cmd_prefix);
            }

            public void SetCmd<T>()where T : ICmd
            {
                m_parser.SetCmd(typeof(T));
            }

            public void SetCmd(Type type)
            {
                if (typeof(ICmd).IsAssignableFrom(type))
                {
                    m_parser.SetCmd(type);
                }
                throw new ArgumentException($"{type} has not implement ICmd");
            }

            public void AddSubCmd<T>()where T : ICmd
            {
                m_parser.AddSubCmd(typeof(T));
            }

            public void AddSubCmd(Type type)
            {
                if (typeof(ICmd).IsAssignableFrom(type))
                {
                    m_parser.AddSubCmd(type);
                }
                throw new ArgumentException($"{type} has not implement ICmd");
            }

            public void AddSubGroup(CmdGroupParser group)
            {
                m_parser.AddSubGroup(group.m_parser);
            }
        }

        public static bool MatchOptionPrefix(this string str)
        {
            if (str == "-") return true;// fix bug
            if(str?.Length > 1 && str[0] == '-')
            {
                char c = str[1];
                return (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z');
            }
            return false;
        }
        // 先不开放这个
        internal abstract class CmdSerializeBase
        {
            // for generic and container type
            public virtual bool CanHandle(Type type) { return false; }
            public virtual void Merge(ref object obj, object old) { }
            public abstract string GetTypeName(Type type);
            public virtual object Parse(MyItor<string> itor, Type type)
            {
                if (itor.HasValue == false || itor.Current.MatchOptionPrefix())
                {
                    throw new MyException("Need input");
                }
                var obj = Parse(itor.Current, type);
                itor.MoveNext();
                return obj;
            }
            public abstract object Parse(string str, Type type);
        }

        public interface ICmd
        {
            void Exec();
        }
    }
}


namespace _Internal.CmdLine
{
    using om.utils;
    using System.Text;
    using CmdSerializeBase = om.utils.CmdLine.CmdSerializeBase;

    /// <summary>
    /// 1.0 
    /// </summary> 
    static class CmdSerializeMgr
    {
        // 不要作死
        // 主要是给 Primitives类型，可以扩充给具体类型，这里的元素每次解析消耗一个字符串
        static Dictionary<Type, CmdSerializeBase> m_sp_handlers;
        // 主要是给泛型或者数组类的容器，发现Enum也是这类的
        static List<CmdSerializeBase> m_other_handlers;

        static SerializeFromString m_str_handler;

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

                {typeof(string), new SerializeString() },
                {typeof(byte[]), new SerializeBytes() },
            };

            m_other_handlers = new List<CmdSerializeBase>()
            {
                new SerializeEnum(),
                new SerializeArray(),
                new SerializeList(),
                new SerializeDictionary(),
            };

            m_str_handler = new SerializeFromString();
        }

        public static CmdSerializeBase FindHandler(Type type)
        {
            if (m_sp_handlers.ContainsKey(type))
            {
                return m_sp_handlers[type];
            }

            if (m_str_handler.CanHandle(type))
            {
                return m_str_handler;
            }

            var handler = m_other_handlers.Find(h => h.CanHandle(type));
            return handler;
        }

        public static bool CanInContainer(Type type)
        {
            return type.IsEnum || m_sp_handlers.ContainsKey(type) || m_str_handler.CanHandle(type);
        }
    }

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

        public string MessageForTip
        {
            get
            {
                string req = required ? "* " : "  ";

                CmdPrintLineTool tool = new CmdPrintLineTool(3, 10,10);
                if (name == "")
                {
                    tool.Add($"Args: {req}", TypeNameForPrint, tip);
                }
                else
                {
                    tool.Add($"{req}-{name}", TypeNameForPrint, tip);
                }
                return tool.GetPrintString().TrimEnd();
            }
        }

        void InitOptionInfo(MemberInfo member)
        {
            var option = member.GetCustomAttribute<OptionAttribute>();
            if (option == null)
            {
                // 应该只有field会走到这儿来
                throw new Exception("should not happend");
                //name = member.Name.ToLower();
                //if (name.StartsWith("m_")) name = name.Substring(2);
                //name = name.Trim('_').Replace('_', '-');
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

    class InnerCmdParser
    {
        Dictionary<string, OptionParser> m_options;
        public string name;
        public string alias;
        public string tip;

        public Type m_type;

        public InnerCmdParser(Type type)
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
            foreach (var field in type.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance))
            {
                if (field.GetCustomAttribute<OptionAttribute>() == null) continue;
                var option = new OptionParser(field);
                m_options.Add(option.name, option);
                if (option.alias != null)
                {
                    m_options.Add(option.alias, option);
                }
            }
            foreach (var property in type.GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance))
            {
                if (property.GetCustomAttribute<OptionAttribute>() != null)
                {
                    if (property.GetIndexParameters().Length > 0)
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

        internal object Parse(object obj, MyItor<string> itor)
        {
            while (itor.HasValue)
            {
                string name = itor.Current;
                if (name.MatchOptionPrefix())
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
                        throw new MyException($"Parse error \n{option.MessageForTip}\n{e.Message}");
                    }
                }
                else
                {
                    if (name == "") throw new MyException($"Do not need any args");
                    throw new MyException($"Unkown option -{name}");
                }
            }
            foreach (var item in m_options.Values)
            {
                if (item.required && item.m_has_input == false)
                {
                    throw new MyException($"Miss requied option\n{item.MessageForTip}");
                }
            }
            return obj;
        }

        public object Parse(IEnumerable<string> args)
        {
            var itor = new MyItor<string>(args);
            if (itor.HasValue && itor.Current == "-?" || itor.Current == "-？")
            {
                PrintHelp();
                return null;
            }
            var obj = Activator.CreateInstance(m_type, true);
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
            Console.WriteLine($"Use: {cmd_prefix} [args] [options]");
            Console.WriteLine();

            OptionParser args_option;
            if (m_options.TryGetValue("", out args_option))
            {
                Console.WriteLine(args_option.MessageForTip);
                Console.WriteLine();
            }

            Console.WriteLine("Options:");
            var print = new CmdPrintLineTool(3,10,10);
            foreach (var option in m_options.Values.Distinct().OrderBy(p => p.name))
            {
                if (args_option == option) continue;

                string name = option.name;
                if (option.alias != null) name += "|-" + option.alias;
                string type = option.TypeNameForPrint;
                if (option.required)
                {
                    print.Add($"* -{name}", $"{type}", option.tip);
                }
                else
                {
                    print.Add($"  -{name}", $"{type}", option.tip);
                }
            }
            print.Print();
            Console.WriteLine();
        }
    }

    class InnerCmdGroupParser
    {
        Type m_type;
        InnerCmdParser m_parser;
        Dictionary<string, InnerCmdGroupParser> m_sub_parsers = new Dictionary<string, InnerCmdGroupParser>();

        public void SetCmd(Type type)
        {
            m_type = type;
            m_parser = new InnerCmdParser(m_type);
        }

        public void AddSubCmd(Type type)
        {
            var g = new InnerCmdGroupParser();
            g.SetCmd(type);
            m_sub_parsers.Add(g.m_parser.name, g);
            if (g.m_parser.alias != null) m_sub_parsers.Add(g.m_parser.alias, g);
        }

        public void AddSubGroup(InnerCmdGroupParser group)
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

        public object Parse(IEnumerable<string> args)
        {
            var itor = new MyItor<string>(args);
            // get type
            InnerCmdGroupParser g = this;
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
            if (itor.HasValue && itor.Current == "-?" || itor.Current == "-？")
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

            var obj = Activator.CreateInstance(g.m_parser.m_type, true);
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

            Console.WriteLine("Subcmds:");
            var print = new CmdPrintLineTool(2,20);
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
        List<string>[] m_cols;
        int m_col_num;
        int[] m_lens;


        public CmdPrintLineTool(int col_num, params int[] lens)
        {
            Debug.Assert(col_num > 0);
            m_col_num = col_num;
            m_cols = new List<string>[col_num];
            m_lens = new int[col_num];
            for (int i = 0; i < col_num; i++)
            {
                m_cols[i] = new List<string>();
                m_lens[i] = lens?.Length > i ? lens[i] : 10;
            }
        }

        public void Add(params string[] strs)
        {
            Debug.Assert(strs?.Length == m_col_num);
            for (int i = 0; i < m_col_num; i++)
            {
                var str = strs[i] ?? "";
                m_cols[i].Add(str.TrimEnd().Replace("\t",""));
            }
        }

        public void Print()
        {
            Console.Write(GetPrintString());
        }

        public string GetPrintString()
        {
            // 约定下，除了最后一列，前面的列应该都是半角字符
            StringBuilder sb = new StringBuilder();

            int pre_width = 0;

            int[] lens = new int[m_col_num];
            m_lens.CopyTo(lens, 0);
            for (var i = 0; i < m_col_num - 1; i++)
            {
                m_cols[i].ForEach(h => lens[i] = Math.Max(lens[i], h.Length + 2));
                pre_width += lens[i];
            }
            string pad_pre = " ".PadRight(pre_width);

            int max_width = Math.Max(pre_width + 10, Console.WindowWidth);
            int last_len = max_width - pre_width;

            for (var i = 0; i < m_cols[0].Count; i++)
            {
                for (var k = 0; k < m_col_num - 1; k++)
                {
                    var str = m_cols[k][i];
                    sb.Append(str.PadRight(lens[k]));
                }
                // 最后一列可能被切断
                string last = m_cols[m_col_num - 1][i];
                int len = last.Length;
                int idx = 0;
                int cur = 0;
                while (idx < len)
                {
                    if(last[idx] == '\r')
                    {
                        idx++;
                        continue;
                    }
                    else if(last[idx] == '\n')
                    {
                        cur = 0;
                        // new line
                        idx++;
                        sb.AppendLine();
                        sb.Append(pad_pre);
                        continue;
                    }
                    int t = GetCharWidth(last[idx]);
                    if (cur + t <= last_len)
                    {
                        cur += t;
                        sb.Append(last[idx++]);
                    }
                    else
                    {
                        cur = 0;
                        // new line
                        sb.AppendLine();
                        sb.Append(pad_pre);
                    }
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }

        static int GetCharWidth(char ch)
        {
            return IsFullWidthChar(ch) ? 2 : 1;
        }

        static HashSet<char> s_sp_full_width_chars = new HashSet<char>
        {
            '｟',
            '｠',
            '￠',
            '￡',
            '￢',
            '￣',
            '￤',
            '￥',
            '￦',
            '│',
            '←',
            '↑',
            '→',
            '↓',
            '■',
            '○',
        };
        static bool IsFullWidthChar(char ch)
        {
            // > https://zh.wikipedia.org/wiki/全形和半形
            // > https://www.jianshu.com/p/e3e281193d14
            // > https://blog.csdn.net/youoran/article/details/8299731

            return ('\uff01' <= ch && ch <= '\uff5e') // ASCII 全角版本，特殊的 空格是'\u3000'
                | ('\u3000' <= ch && ch <= '\u9fff') // CJK 中日韩
                | s_sp_full_width_chars.Contains(ch)
                ;
        }

        public void Clear()
        {
            foreach(var col in m_cols)
            {
                col.Clear();
            }
        }
    }

    class SerializeFromString : CmdSerializeBase
    {
        public override bool CanHandle(Type type)
        {
            var func = type.GetMethod("ParseFromString",
                BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public,
                null,
                new Type[] { typeof(string) },
                null);
            return func != null;
        }
        public override string GetTypeName(Type type)
        {
            return type.Name;
        }

        public override object Parse(string str, Type type)
        {
            var func = type.GetMethod("ParseFromString",
                BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public,
                null,
                new Type[] { typeof(string) },
                null);
            return func.Invoke(null, new object[] { str });
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
            if (itor.HasValue && itor.Current.MatchOptionPrefix() == false)
            {
                var obj = Parse(itor.Current, type);
                itor.MoveNext();
                return obj;
            }
            return true;// default
        }

        public override object Parse(string str, Type type)
        {
            return Convert.ToBoolean(str);
        }
    }

    class SerializeChar : CmdSerializeBase
    {
        public override string GetTypeName(Type type)
        {
            return "char";
        }

        public override object Parse(string str, Type type)
        {
            return Convert.ToChar(str);
        }
    }

    class SerializeByte : CmdSerializeBase
    {
        public override string GetTypeName(Type type)
        {
            return "byte";
        }

        public override object Parse(string str, Type type)
        {
            return Convert.ToByte(str);
        }
    }

    class SerializeSByte : CmdSerializeBase
    {
        public override string GetTypeName(Type type)
        {
            return "sbyte";
        }

        public override object Parse(string str, Type type)
        {
            return Convert.ToSByte(str);
        }
    }

    class SerializeInt16 : CmdSerializeBase
    {
        public override string GetTypeName(Type type)
        {
            return "int16";
        }

        public override object Parse(string str, Type type)
        {
            return Convert.ToInt16(str);
        }
    }

    class SerializeUInt16 : CmdSerializeBase
    {
        public override string GetTypeName(Type type)
        {
            return "uint16";
        }

        public override object Parse(string str, Type type)
        {
            return Convert.ToUInt16(str);
        }
    }

    class SerializeInt32 : CmdSerializeBase
    {
        public override string GetTypeName(Type type)
        {
            return "int32";
        }

        public override object Parse(string str, Type type)
        {
            return Convert.ToInt32(str);
        }
    }

    class SerializeUInt32 : CmdSerializeBase
    {
        public override string GetTypeName(Type type)
        {
            return "uint32";
        }

        public override object Parse(string str, Type type)
        {
            return Convert.ToUInt32(str);
        }
    }

    class SerializeInt64 : CmdSerializeBase
    {
        public override string GetTypeName(Type type)
        {
            return "int64";
        }

        public override object Parse(string str, Type type)
        {
            return Convert.ToInt64(str);
        }
    }

    class SerializeUInt64 : CmdSerializeBase
    {
        public override string GetTypeName(Type type)
        {
            return "uint64";
        }

        public override object Parse(string str, Type type)
        {
            return Convert.ToUInt64(str);
        }
    }

    class SerializeSingle : CmdSerializeBase
    {
        public override string GetTypeName(Type type)
        {
            return "float";
        }

        public override object Parse(string str, Type type)
        {
            return Convert.ToSingle(str);
        }
    }

    class SerializeDouble : CmdSerializeBase
    {
        public override string GetTypeName(Type type)
        {
            return "double";
        }

        public override object Parse(string str, Type type)
        {
            return Convert.ToDouble(str);
        }
    }

    class SerializeDecimal : CmdSerializeBase
    {
        public override string GetTypeName(Type type)
        {
            return "decimal";
        }

        public override object Parse(string str, Type type)
        {
            return Convert.ToDecimal(str);
        }
    }

    class SerializeDateTime : CmdSerializeBase
    {
        public override string GetTypeName(Type type)
        {
            return "DateTime";
        }

        public override object Parse(string str, Type type)
        {
            // todo, 格式差不多是：yyyy-MM-dd HH:mm:ss
            return Convert.ToDateTime(str);
        }
    }

    class SerializeString : CmdSerializeBase
    {
        public override string GetTypeName(Type type)
        {
            return "string";
        }

        public override object Parse(string str, Type type)
        {
            return str;
        }
    }

    class SerializeBytes : CmdSerializeBase
    {
        public override string GetTypeName(Type type)
        {
            return "Base64.byte[]";// base64 format
        }

        public override object Parse(string str, Type type)
        {
            return Convert.FromBase64String(str);
        }
    }

    #endregion

    class SerializeEnum : CmdSerializeBase
    {
        public override bool CanHandle(Type type)
        {
            return type.IsEnum;
        }
        public override string GetTypeName(Type type)
        {
            return "Enum." + type.Name;
        }

        public override object Parse(string str, Type type)
        {
            return Enum.Parse(type, str);
        }
    }

    class SerializeArray : CmdSerializeBase
    {
        public override bool CanHandle(Type type)
        {
            if (type.IsArray && type.GetArrayRank() == 1)
            {
                var under = type.GetElementType();
                return CmdSerializeMgr.CanInContainer(under);
            }
            return base.CanHandle(type);
        }
        public override string GetTypeName(Type type)
        {
            var under = type.GetElementType();
            var handler = CmdSerializeMgr.FindHandler(under);
            return handler.GetTypeName(under) + "[]";
        }

        public override object Parse(MyItor<string> itor, Type type)
        {
            if (itor.HasValue == false || itor.Current.MatchOptionPrefix())
            {
                throw new MyException("Array need input");
            }
            var obj = InnerParse(itor, type);
            return obj;
        }

        public object InnerParse(MyItor<string> itor, Type type)
        {
            List<string> args = new List<string>();
            while (itor.HasValue && itor.Current.MatchOptionPrefix() == false)
            {
                args.Add(itor.Current);
                itor.MoveNext();
            }

            var under = type.GetElementType();
            var func = this.GetType()
                .GetMethod("_Parse", BindingFlags.Static | BindingFlags.NonPublic)
                .MakeGenericMethod(under);

            return func.Invoke(null, new object[] { args });
        }

        static object Parse<T>(List<string> args)
        {
            T[] arr = new T[args.Count];
            var handler = CmdSerializeMgr.FindHandler(typeof(T));
            for(int i = 0; i < args.Count; i++)
            {
                var obj = handler.Parse(args[i], typeof(T));
                arr[i] = (T)obj;
            }
            return arr;
        }

        public override object Parse(string str, Type type)
        {
            throw new NotImplementedException();
        }

        public override void Merge(ref object obj, object old)
        {
            if(old != null)
            {
                Array a = (Array)obj;
                Array b = (Array)old;
                int al = a.Length;
                int bl = b.Length;
                Array c = Array.CreateInstance(obj.GetType().GetElementType(),al + bl);
                Array.Copy(a, 0, c, 0, al);
                Array.Copy(b, 0, c, al, bl);

                obj = c;
            }
        }
    }

    #region Generic
    abstract class SerializeGeneric : CmdSerializeBase
    {
        public override bool CanHandle(Type type)
        {
            if (type.IsGenericType == false) return false;
            if (type.GetGenericTypeDefinition() != GetWorkGenericType()) return false;

            var unders = type.GetGenericArguments();
            foreach(var under in unders)
            {
                if (CmdSerializeMgr.CanInContainer(under) == false) return false;
            }
            return true;
        }

        public abstract Type GetWorkGenericType();

        public override object Parse(MyItor<string> itor, Type type)
        {
            if (itor.HasValue == false || itor.Current.MatchOptionPrefix())
            {
                throw new MyException("Generic container need input");
            }
            var obj = InnerParse(itor, type);
            return obj;
        }

        public object InnerParse(MyItor<string> itor, Type type)
        {
            List<string> args = new List<string>();
            while (itor.HasValue && itor.Current.MatchOptionPrefix() == false)
            {
                args.Add(itor.Current);
                itor.MoveNext();
            }

            var unders = type.GetGenericArguments();
            var func = this.GetType()
                .GetMethod("ParseGeneric", BindingFlags.Static | BindingFlags.NonPublic)
                .MakeGenericMethod(unders);

            return func.Invoke(null, new object[] { args });
        }

        public override object Parse(string str, Type type)
        {
            throw new NotImplementedException();
        }
    }

    class SerializeList : SerializeGeneric
    {
        public override string GetTypeName(Type type)
        {
            var unders = type.GetGenericArguments();
            var h = CmdSerializeMgr.FindHandler(unders[0]);
            return $"List<{h.GetTypeName(unders[0])}>";
        }

        public override Type GetWorkGenericType()
        {
            return typeof(List<>);
        }

        public override void Merge(ref object obj, object old)
        {
            if (old == null) return;

            var func = this.GetType()
                .GetMethod("Merge", BindingFlags.Static | BindingFlags.NonPublic)
                .MakeGenericMethod(obj.GetType().GetGenericArguments());

            func.Invoke(null, new object[] { obj, old});
        }

        static void Merge<T>(List<T> a, List<T> b)
        {
            a.AddRange(b);
        }

        static object ParseGeneric<T>(List<string> args)
        {
            T[] arr = new T[args.Count];
            var handler = CmdSerializeMgr.FindHandler(typeof(T));
            for (int i = 0; i < args.Count; i++)
            {
                var obj = handler.Parse(args[i], typeof(T));
                arr[i] = (T)obj;
            }
            return arr.ToList();
        }
    }

    class SerializeDictionary : SerializeGeneric
    {
        public override string GetTypeName(Type type)
        {
            var unders = type.GetGenericArguments();
            var h = CmdSerializeMgr.FindHandler(unders[0]);
            var h1 = CmdSerializeMgr.FindHandler(unders[1]);
            return $"Map<{h.GetTypeName(unders[0])},{h1.GetTypeName(unders[1])}>";
        }

        public override Type GetWorkGenericType()
        {
            return typeof(Dictionary<,>);
        }

        public override void Merge(ref object obj, object old)
        {
            if (old == null) return;

            var func = this.GetType()
                .GetMethod("Merge", BindingFlags.Static | BindingFlags.NonPublic)
                .MakeGenericMethod(obj.GetType().GetGenericArguments());

            func.Invoke(null, new object[] { obj, old });
        }

        static void Merge<K,V>(Dictionary<K, V> a, Dictionary<K, V> b)
        {
            foreach(var item in b)
            {
                if(a.ContainsKey(item.Key) == false)
                {
                    a.Add(item.Key, item.Value);
                }
            }
        }

        static object ParseGeneric<K,V>(List<string> args)
        {
            Dictionary<K, V> dic = new Dictionary<K, V>(args.Count);
            var hk = CmdSerializeMgr.FindHandler(typeof(K));
            var hv = CmdSerializeMgr.FindHandler(typeof(V));

            for (int i = 0; i < args.Count; i++)
            {
                var s = args[i];
                var idx = s.IndexOf('=');
                if (idx == -1)
                {
                    throw new MyException("Map type format is <key>=<value>");
                }
                var k = (K)hk.Parse(s.Substring(0, idx), typeof(K));
                var v = (V)hv.Parse(s.Substring(idx + 1), typeof(V));
                dic[k] = v;// 重复的不会出错
            }
            return dic;
        }
    }

    #endregion
}