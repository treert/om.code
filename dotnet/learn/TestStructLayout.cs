using System.Runtime.InteropServices;

namespace MyTest;

/*
c# 不支持 Bit-Fields
*/

class TestStructLayout{
    public static void Run(){
        TestUnion();
        TestPack();
    }

    // Pack=0 时是默认的吧。
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct TPack{
        public byte b1;
        public byte b2;
        public int i3;
    }
    private unsafe static void TestPack()
    {
        using var _ = new LogCall();
        var t = new TPack();
        byte* addr = (byte*) &t;
        Console.WriteLine("Size:      {0}", sizeof(TPack));
        Console.WriteLine("b1 Offset: {0}", &t.b1 - addr);
        Console.WriteLine("b2 Offset: {0}", &t.b2 - addr);
        Console.WriteLine("i3 Offset: {0}", (byte*) &t.i3 - addr);
    }

    [StructLayout(LayoutKind.Explicit)]
    struct TUnion
    {
        [FieldOffset(0)]
        public int i;
        [FieldOffset(0)]
        public double d;
        [FieldOffset(0)]
        public long i64;
        [FieldOffset(0)]
        public bool b;
    };
    private static void TestUnion()
    {
        using var _ = new LogCall();
        TUnion t1 = new TUnion{i=1};
        Console.WriteLine($"i={t1.i} d={t1.d} i64={t1.i64} b={t1.b}");
        t1.i = 2;
        Console.WriteLine($"i={t1.i} d={t1.d} i64={t1.i64} b={t1.b}");
        t1.i = 1<<7;
        Console.WriteLine($"i={t1.i} d={t1.d} i64={t1.i64} b={t1.b}");
        t1.i = 1<<8;
        Console.WriteLine($"i={t1.i} d={t1.d} i64={t1.i64} b={t1.b}");
        t1.i = (1<<8)|1;
        Console.WriteLine($"i={t1.i} d={t1.d} i64={t1.i64} b={t1.b}");
    }
}