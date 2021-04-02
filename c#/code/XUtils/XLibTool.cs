using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace XUtils
{
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
            var name = Path.GetFileNameWithoutExtension(dll_path);
            var dir = Path.GetDirectoryName(dll_path);
            try
            {
                var dll = File.ReadAllBytes(dll_path);
                var pdb_path = Path.Combine(dir, name + ".pdb");
                if (File.Exists(pdb_path))
                {
                    var symbol = File.ReadAllBytes(pdb_path);
                    return Assembly.Load(dll, symbol);
                }
                return Assembly.Load(dll);
            }
            catch (Exception)
            {
                return null;
            }
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
