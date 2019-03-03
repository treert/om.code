/*
 * 1.0 单文件版
 * 一些限制：
 * - 不支持默认值设置
 * - 不支持自定义类型，只支持几个基础类型
 * 
 * 
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
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

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    public class Option:Attribute
    {
        public string name;
        public string alias;// 别名，可以用于提供一个缩写名
        public string tip;// 自定义帮助说明
        public bool required = false;

        public Option(string name){
            if (name == null) throw new ArgumentNullException("name");

            this.name = name.Trim().ToLower();
        }
        
    }

    class MyItor<T> : IDisposable
    {
        IEnumerator<T> m_itor;

        public bool HasValue { get; private set; }
        public MyItor(IEnumerable<T> args)
        {
            Debug.Assert(args != null, "args can not be null");
            m_itor = args.GetEnumerator();
            HasValue = m_itor.MoveNext();
        }

        public T Current
        {
            get
            {
                return m_itor.Current;
            }
        }

        public bool MoveNext()
        {
            return HasValue = m_itor.MoveNext();
        }

        public void Dispose()
        {
            if (m_itor != null)
            {
                m_itor.Dispose();
                m_itor = null;
            }
        }
    }

    public class CmdLine
    {
        public static bool Run<T>(string[] args) where T : ICmd
        {
            
            return true;
        }

        public static ICmd Parse<T>(string[] args) where T : ICmd
        {
            var type = typeof(T);

            var objs = type.GetCustomAttributes(typeof(Option), false);

            return null;
        }

        public class OptionHandle
        {
            Type v_type;
            Type k_type;// for Map<K,V>
            public void GetValue(IEnumerator<string> args)
            {

            }
        }

        public class CmdOptions
        {
            OptionHandle cmd_option;
            OptionHandle[] options;

        }
    }


    public class CmdGroup
    {
        // 这些字段设置为public，是为了方便，不要乱用哦，设置他们，用接口函数
        public Type m_cmd_type;
        public List<CmdGroup> m_sub_cmd_types;

        public void SetCmd<T>() where T : ICmd
        {
            m_cmd_type = typeof(T);
        }

        public void AddSubCmd<T>() where T : ICmd
        {
            if (m_sub_cmd_types == null) m_sub_cmd_types = new List<CmdGroup>();

            var group = new CmdGroup();
            group.m_cmd_type = typeof(T);
            m_sub_cmd_types.Add(group);
        }

        public void AddSubGroup(CmdGroup group)
        {
            if (m_sub_cmd_types == null) m_sub_cmd_types = new List<CmdGroup>();
            m_sub_cmd_types.Add(group);
        }

        public virtual void PrintHelp(){

        }
    }

    #region Parse
    class OptionInfo
    {
        public string name;
        public string alias;
        public string tip;
        public bool required;

        public bool m_has_input = false;

        Type m_type;
        SerializeTool.SerializeBase m_handler;

        FieldInfo m_field;
        PropertyInfo m_property;

        public OptionInfo(FieldInfo field)
        {
            m_field = field;
            InitType(field.FieldType);
            InitOptionInfo(field);
        }

        public OptionInfo(PropertyInfo property)
        {
            m_property = property;
            InitType(property.PropertyType);
            InitOptionInfo(property);
        }

        void InitOptionInfo(MemberInfo member)
        {
            var option = member.GetCustomAttribute<Option>();
            if (option == null)
            {
                // 应该只有field会走到这儿来
                name = member.Name.ToLower();
                if (name.StartsWith("m_")) name = name.Substring(2);
                name = name.Trim('_').Replace('_', '-');
            }
            else
            {
                name = option.name;
                alias = option.alias;
                tip = option.tip;
                required = option.required;
            }
            if (string.IsNullOrWhiteSpace(alias) || alias == name) alias = null;
        }

        void InitType(Type type)
        {
            m_type = type;
            m_handler = SerializeTool.FindHandler(type);
        }

        public void Parse(object obj, MyItor<string> args)
        {
            if(m_has_input){
                if(name == ""){
                    throw new Exception("args duplication");
                }
                else{
                    throw new Exception($"option -{name} duplication");
                }
            }
            m_has_input = true;
            var val = m_handler.Parse(args, m_type);// 格式错误让handle抛异常好了
            
            if(m_field != null)
            {
                m_field.SetValue(obj, val);
            }
            else
            {
                m_property.SetValue(obj, val);
            }
        }
    }

    class CmdParser
    {
        public Dictionary<string, OptionInfo> m_options;
        public string name;
        public string alias;
        public string tip;

        public Type m_type;

        public CmdParser(Type type)
        {
            m_type = type;
            var option = type.GetCustomAttribute<Option>();
            if(option == null)
            {
                name = type.Name.ToLower();
                if (name.EndsWith("cmd")) name = name.Substring(0, name.Length - 3);
            }
            else
            {
                name = option.name;
                alias = option.alias;
                tip = option.tip;
            }

            if (string.IsNullOrWhiteSpace(name)) throw new NotSupportedException($"cmd name can not be empty, cmd type is {type.Name}");

            InitOptions(type);
        }

        void InitOptions(Type type)
        {
            foreach (var field in type.GetFields())
            {
                var option = new OptionInfo(field);
                m_options.Add(option.name, option);
                if (option.alias != null)
                {
                    m_options.Add(option.alias, option);
                }
            }
            foreach (var property in type.GetProperties())
            {
                if(property.GetCustomAttribute<Option>() != null)
                {
                    if (property.GetIndexParameters().Length == 0)
                    {
                        throw new NotSupportedException($"do not support indexed property, {property.DeclaringType.Name}.{property.Name}");
                    }
                    var option = new OptionInfo(property);
                    m_options.Add(option.name, option);
                    if (option.alias != null)
                    {
                        m_options.Add(option.alias, option);
                    }
                }
            }
        }

        public ICmd Parse(ICmd obj, MyItor<string> itor){
            while(itor.HasValue){
                string name = itor.Current;
                if(name.StartsWith("-")){
                    name = name.Substring(1);
                    itor.MoveNext();
                }
                else{
                    name = "";
                }
                OptionInfo option;
                if(m_options.TryGetValue(name, out option)){
                    option.Parse(obj, itor);
                }
                else{
                    throw new Exception($"unkown option -{name}");
                }
            }
            foreach(var item in m_options.Values){
                if(item.required && item.m_has_input == false){
                    throw new Exception($"miss requied option -{item.name} {item.tip}");
                }
            }
            return obj;
        }

        public ICmd Parse(ICmd obj, IEnumerable<string> args){
            var itor = new MyItor<string>(args);
            return Parse(obj, itor);
        }
    }

    class CmdGroupParser
    {
        CmdGroup m_group;
        CmdParser m_parser;
        Dictionary<string, CmdGroupParser> m_sub_parsers;
        public CmdGroupParser(CmdGroup group)
        {
            m_group = group;
            if(group.m_cmd_type != null){
                m_parser = new CmdParser(group.m_cmd_type);
            }
            m_sub_parsers = new Dictionary<string, CmdGroupParser>();
            if(group.m_sub_cmd_types != null){
                foreach(var g in group.m_sub_cmd_types){
                    var p = new CmdGroupParser(g);
                    m_sub_parsers.Add(p.m_parser.name, p);
                    if(string.IsNullOrWhiteSpace(p.m_parser.alias) != false){
                        m_sub_parsers.Add(p.m_parser.alias, p);
                    }
                }
            }
        }

        public ICmd Parse(IEnumerable<string> args){
            var itor = new MyItor<string>(args);
            // get type
            CmdGroupParser g = this;
            List<string> cmds = new List<string>();
            while(itor.HasValue){
                if(g.m_sub_parsers.ContainsKey(itor.Current)){
                    g = g.m_sub_parsers[itor.Current];
                    cmds.Add(itor.Current);
                    itor.MoveNext();
                }
            }
            if(g.m_parser == null){
                g.m_group.PrintHelp();
                return null;
            }
            
            var obj = (ICmd)Activator.CreateInstance(g.m_parser.m_type, true);
            g.m_parser.Parse(obj, itor);
            return obj;
        }
    }

    #endregion

    #region serialize tool
    /// <summary>
    /// 1.0 
    /// 不支持扩展，只接受指定的类型
    /// 容器类型只支持少数几个。Array，List
    /// </summary>
    static class SerializeTool
    {
        static Dictionary<Type, SerializeBase> m_sp_handlers;// 主要是给 Primitives类型，可以扩充给具体类型
        static List<SerializeBase> m_other_handlers;// 主要是给泛型或者数组类的容器

        static SerializeTool()
        {
            m_sp_handlers = new Dictionary<Type, SerializeBase>()
            {
                {typeof(int),null},
            };

            m_other_handlers = new List<SerializeBase>()
            {
            };
        }

        public static SerializeBase FindHandler(Type type)
        {
            if (m_sp_handlers.ContainsKey(type))
            {
                return m_sp_handlers[type];
            }

            var handler = m_other_handlers.Find(h => h.CanHandle(type));
            if (handler == null)
                throw new NotSupportedException($"Can not Parse {type}");

            return handler;
        }


        public abstract class SerializeBase
        {
            public virtual bool CanHandle(Type type) { return false; }
            public virtual object GetDefaultForNullInput() { return null; }
            public abstract object Parse(MyItor<string> args, Type type);
        }
    }
    #endregion
}
