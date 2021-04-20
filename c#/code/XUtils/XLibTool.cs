using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace XUtils
{
    /// <summary>
    /// ResolveAssembly 总结：使用 Assembly.LoadFrom 去加载dll。
    /// 1. Assembly.Load(string-name) 根据名字加载dll，如果默认路径里没有dll，需要通过ResolveAssembly去加载真实内容。
    ///     - 不会重复加载，同样的字符串参数，只会加载一次
    /// 2. Assembly.LoadFrom(path) 加载文件，路径可以是相对路径，甚至url。
    ///     - 不会重复加载，它会读取dll获取FullName，用于调用Assembly.Load(string-name)。
    /// 3. Assembly.LoadFile(path) 不会重复加载，**但是它不加载依赖。一般不用，做dll分析时可能有用**
    /// 4. Assembly.Load(byte[]) **会重复加载**，不要使用。
    /// 
    /// 一些参考文章：
    /// 1. https://www.cnblogs.com/zagelover/articles/2726034.html C#反射-Assembly.Load、LoadFrom与LoadFile进阶 可以读一读
    /// 2. https://stackoverflow.com/questions/733868/difference-between-appdomain-assembly-process-and-a-thread 补充阅读
    /// 3. https://docs.microsoft.com/en-us/dotnet/api/system.appdomain?view=net-5.0 AppDomain 官方文件，值得一读
    /// </summary>
    public class XLibTool
    {
        // 不知道是这样好，还是吧XLibTool标记成static好。
        public static readonly XLibTool singleton = new XLibTool();

        public string m_dll_dir = "";
        public void Init(string dll_dir)
        {
            m_dll_dir = dll_dir;
            AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;
        }

        public void UnInit()
        {
            AppDomain.CurrentDomain.AssemblyResolve -= ResolveAssembly;
        }

        public Assembly LoadDll(string dll_path)
        {
            try
            {
                return Assembly.LoadFrom(dll_path);
            }
            catch
            {
                return null;
            }
            // 这段代码会导致重复加载
            //var name = Path.GetFileNameWithoutExtension(dll_path);
            //var dir = Path.GetDirectoryName(dll_path);
            //try
            //{
            //    var dll = File.ReadAllBytes(dll_path);
            //    var pdb_path = Path.Combine(dir, name + ".pdb");
            //    if (File.Exists(pdb_path))
            //    {
            //        var symbol = File.ReadAllBytes(pdb_path);
            //        return Assembly.Load(dll, symbol);
            //    }
            //    return Assembly.Load(dll);
            //}
            //catch (Exception)
            //{
            //    return null;
            //}
        }

        Assembly ResolveAssembly(object sender, ResolveEventArgs args)
        {
            int idx = args.Name.IndexOf(',');
            string name = idx > 0 ? args.Name.Substring(0, idx) : args.Name;

            if (Directory.Exists(m_dll_dir) == false) return null;

            foreach(var file in Directory.GetFiles(m_dll_dir, "*.dll", SearchOption.AllDirectories))
            {
                if(Path.GetFileNameWithoutExtension(file) == name)
                {
                    return LoadDll(file);
                }
            }
            return null;
        }
    }
}
