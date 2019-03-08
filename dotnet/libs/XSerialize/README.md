# 序列化工具

## 二进制
### XBinarySerializer2
保存了类型信息，使用了`Type.GetType(AssemblyQualifiedName);`使用方便。例子： 
```c#
using XBinarySerializer = XSerialize.XBinarySerializer2;
{
    List<object[]> xx = new List<object[]>() { new object[] { 0, 1 }, new object[] { 2, 3 } };
    xx[0][1] = xx;
    var serializer = new XBinarySerializer();// 初始化
    using (var stream = new MemoryStream())
    {
        serializer.Serialize(stream, xx);// 序列化
        stream.Seek(0, SeekOrigin.Begin);
        var yy = serializer.Deserialize(stream) as List<object[]>;// 反序列化
        Console.WriteLine(yy[0][1]);
    }
}
```
### XBinarySerializer
需要提供type[]，用于初始化。例子： 
```c#
using XBinarySerializer = XSerialize.XBinarySerializer;
{
    List<object[]> xx = new List<object[]>() { new object[] { 0, 1 }, new object[] { 2, 3 } };
    xx[0][1] = xx;
    var serializer = new XBinarySerializer(xx.GetType());// 初始化
    using (var stream = new MemoryStream())
    {
        serializer.Serialize(stream, xx);// 序列化
        stream.Seek(0, SeekOrigin.Begin);
        var yy = serializer.Deserialize(stream) as List<object[]>;// 反序列化
        Console.WriteLine(yy[0][1]);
    }
}
```


## Xml
限制：
1. 不支持多态，传入对象类型和定义类型需要相同。【不能使用接口，虚类】
2. 不能有类对象引用出现两次。【当然也不支持引用循环】
2. 类需要有默认构造函数。

例子：
```c#
using XXmlSerializer = XSerialize.XXmlSerializer;
{
    XXmlSerializer serializer = new XXmlSerializer();
    Action<object> test_func = (object obj) =>
    {
        Type type;
        if (obj == null)
            type = typeof(object);
        else
            type = obj.GetType();
        string xx = serializer.SerializeToString(obj);
        Console.WriteLine(xx);
        object yy = serializer.DeserializeFromString(xx, type);
        Console.WriteLine(yy);
    };
    DateTime time = DateTime.Now;
    DateTime utc = DateTime.UtcNow;
    object x = (byte)2;
    byte y = (byte)x;
    test_func(null);
    test_func(1);
    test_func((sbyte)-2);
    test_func((long)1123456789123456789);
    test_func(123456789123456789123456789m);
}
```

## XmlDump
唯一限制：
1. 支持序列化，不支持反序列化

例子：
```c#
using XXmlDump = XSerialize.XXmlDump;
{
    XXmlDump serializer = new XXmlDump();
    Action<object> test_dump = (object obj) =>
    {
        string xx = serializer.Dump(obj);
        Console.WriteLine(xx);
    };
    test_dump(typeof(Type));
    test_dump(Encoding.UTF8);
}
```


