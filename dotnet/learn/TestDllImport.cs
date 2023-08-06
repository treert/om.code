using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace MyTest;

/*
1. 最好不要传递 string, 内部包含的转换逻辑。

现在似乎推荐用 LibraryImport 替代 DllImport,两者还是有差别的。
> https://learn.microsoft.com/en-us/dotnet/standard/native-interop/pinvoke-source-generation
*/

partial class TestDllImport
{
    public static void Run()
    {
        TestHello();
        TestPassStruct();
    }

    // https://gist.github.com/esskar/3779066?permalink_comment_id=2005490
    // not work now. 这个结构会挂掉. 应该是不支持字符串的缘故
    // [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    //  struct Name
    // {
    //     [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)]
    //     public string FirstName;
    //     [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)]
    //     public string LastName;
    //     [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
    //     public string[] Array;
    // };

    struct Name{
        public int i;
         [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public byte[] arr;    
    }

    // 好像不能使用 LibraryImport
    [DllImport("HelloLib", CallingConvention = CallingConvention.Cdecl)]
    static extern void GetName(ref Name name);
    
    private static void TestPassStruct()
    {
        using var _ = new LogCall();
        var name = new Name();
        GetName(ref name);
        Console.WriteLine($"{name.i} Array={name.arr.Length}");
        for (int i = 0; i < name.arr.Length; i++)
        {
            Console.WriteLine($"Array[{i}]={name.arr[i]}");
        }
    }

    static void TestHello()
    {
        using var _ = new LogCall();
        int a = Add(1, 2);
        Debug.Assert(a == 3);
        var len = GetStrLen("abc", 1);
        Console.WriteLine(len);
        var ptr = ConvertIntToStr(11);
        var str = Marshal.PtrToStringUTF8(ptr);
        Console.WriteLine(str);
    }

    /*
        [DllImport("HelloLib", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int GetStrLen1(string str, int print);
    //*/
    // 这个是上面的代码自动转换的结果
    [LibraryImport("HelloLib", StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(System.Runtime.InteropServices.Marshalling.AnsiStringMarshaller))]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial int GetStrLen1(string str, int print);

    [LibraryImport("HelloLib")]
    public static partial int Add(int a, int b);

    [LibraryImport("HelloLib", StringMarshalling = StringMarshalling.Utf8)]
    public static partial int GetStrLen(string str, int print);

    /*
        need return IntPtr for const char*, then convert ptr to string in c#.
        也许还有别的方法返回字符串，但是没找到。
    */
    [LibraryImport("HelloLib")]
    public static partial IntPtr ConvertIntToStr(int n);

    static TestDllImport()
    {
        Console.WriteLine("static TestDllImport()");
        // https://stackoverflow.com/questions/8836093/how-can-i-specify-a-dllimport-path-at-runtime
        NativeLibrary.SetDllImportResolver(typeof(TestDllImport).Assembly, ImportResolver);
    }

    private static IntPtr ImportResolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        IntPtr libHandle = IntPtr.Zero;
        if (libraryName == "HelloLib")
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var dir = Environment.CurrentDirectory;
                var dll_path = $"{dir}/../cpp/output/bin/hello.dll";
                libHandle = NativeLibrary.Load(dll_path);
            }
        }
        return libHandle;
    }
}