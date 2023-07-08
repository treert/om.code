namespace MyTest;



public class TestSyntax{
    
    public static void Run(){
        TestRecord();
        
    }

    record Item(string name, int i);

    static void TestRecord(){
        // https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/tutorials/records
        Console.WriteLine("Start TestRecord");
        Item t1 = new("123",123);
        Item t2 = new("123",123);
        Console.WriteLine($"{t1 == t2} {t1}");

    }
}