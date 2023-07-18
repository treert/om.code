namespace MyTest;

/*
零散的知识点

*/



public class TestSyntax{
    
    public static void Run(){
        Console.WriteLine();
        Console.WriteLine("Start TestSyntax");
        TestRecord();
        
    }
    record Item(string name, int i){
        // extra property, and will change change equal
        public string full_name => $"{name}-{i:D4}-{Random.Shared.Next()}";
    }
    /*
        https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/tutorials/records
        record 相当于一个语法糖，
        1. 创建不可变结构。
        2. 自动实现 System.IEquatable<T> 和 GetHashCode == 等接口。
        几个选项
        1. record class. init-only. class 可以省略.
            - record class 也可以实现 read-write. 定义 public int i=1; 这种字段就行， 见 TestChannel.Item
        2. readonly record struct. init-only.
        3. record struct. read-write.
        
    */
    static void TestRecord(){
        Console.WriteLine("Start TestRecord");
        Item t1 = new("t123",123);
        Item t2 = new("t123",123);
        Console.WriteLine($"{t1 == t2} {t1} {t2}");
        
        // create new record base on proto
        var t3 = t2 with{ i = 345};
        Console.WriteLine($"t3={t3}");

    }



    /*
        https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/tutorials/static-virtual-interface-members
    */
    static void TestStaticInterfaceMembers(){

    }
}