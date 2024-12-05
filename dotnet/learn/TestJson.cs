using System.ComponentModel;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace MyTest
{

    /// <summary>
    /// 类似 JsonStringEnumConverter 不过序列化时全部转成小写。
    /// 
    /// https://stackoverflow.com/questions/59059989/system-text-json-how-do-i-specify-a-custom-name-for-an-enum-value
    /// </summary>
    public class MyJsonEnumConverter : JsonConverter<object>
    {
        /// <inheritdoc/>
        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert.IsEnum || Nullable.GetUnderlyingType(typeToConvert)?.IsEnum == true;
        }
        /// <inheritdoc/>
        public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var enumType = Nullable.GetUnderlyingType(typeToConvert) ?? typeToConvert;
            if (reader.TokenType == JsonTokenType.String){
                string str = reader.GetString()!;
                return Enum.Parse(enumType, str, true);
            }
            else if(reader.TokenType == JsonTokenType.Number){
                int num = reader.GetInt32();
                return Enum.ToObject(enumType, num);
            }
            else if(reader.TokenType == JsonTokenType.Null){
                return null;
            }
            throw new Exception();
        }
        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
        {
            if (value == null)
                writer.WriteNullValue();
            else{
                writer.WriteStringValue((value as Enum)!.ToString().ToLower());
                // writer.WriteStringValue(GetDescription((value as Enum)!));
            }
        }
        /// <summary>
        /// Get description
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        static string GetDescription(Enum source)
        {
            System.Reflection.FieldInfo? fi = source.GetType().GetField(source.ToString());
            if (fi == null) return source.ToString().ToLower();
            System.ComponentModel.DescriptionAttribute[] attributes = (System.ComponentModel.DescriptionAttribute[])fi.GetCustomAttributes(typeof(System.ComponentModel.DescriptionAttribute), false);

            if (attributes != null && attributes.Length > 0) return attributes[0].Description;
            else return source.ToString().ToLower();
        }

    }

    // 这么简单的函数。但是Json本身并没有。
    public static class MyJsonExt
    {
        private static JsonSerializerOptions s_json_options = new JsonSerializerOptions(){ 
            WriteIndented = false,
            // 如果用了。需要一直用这套设置。
            // Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)},
        };

        public static string ToJsonStr(this JsonNode node)
        {
            return node.ToJsonString(s_json_options);
        }

        public static string ToJsonStr(this object value)
        {
            return JsonSerializer.Serialize(value, value.GetType(), s_json_options);
        }

        public static T ConvertTo<T>(this JsonNode? node)
        {
            // 这个实现性能并不好。它会先序列化成字符串，然后再重新解析一遍。(⊙o⊙)…
            T t = JsonSerializer.Deserialize<T>(node, s_json_options)!;
            return t;
        }

        public static JsonNode ToJsonNode(this object value)
        {
            var node = JsonSerializer.SerializeToNode(value, value.GetType(), s_json_options);
            return node!;
        }

        public static void AddKeyValue(this JsonObject json, string key, object? value)
        {
            json.Add(key, value?.ToJsonNode());
        }
    }

    [JsonConverter(typeof(MyNodeJsonConverter))]
    public class MyNode
    {
        /// <summary>
        /// Json全局序列化选项。先放在这儿
        /// </summary>
        public static JsonSerializerOptions options = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        public T? AsT<T>()
        {
            if (node is null)
            {
                return default(T?);
            }
            else
            {
                try
                {
                    T? ret = node.Deserialize<T>(MyNode.options);
                    return ret;
                }
                catch (Exception)
                {
                    return default(T?);
                }
            }
        }

        public bool IsNull()
        {
            return node is null;
        }

        public JsonNode Node { get { return node!; } }

        public override string ToString()
        {
            if (node is null)
            {
                return "null";
            }
            else
            {
                return node.ToJsonString();
            }
        }
        public JsonNode? node;
    }



    public class MyNodeJsonConverter : JsonConverter<MyNode>
    {
        public override bool HandleNull => true;
        public override MyNode? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            MyNode mynode = new MyNode();
            mynode.node = JsonNode.Parse(ref reader, new JsonNodeOptions { PropertyNameCaseInsensitive = options.PropertyNameCaseInsensitive });
            return mynode;
        }

        public override void Write(Utf8JsonWriter writer, MyNode value, JsonSerializerOptions options)
        {
            if (value is null || value.node is null)
            {
                writer.WriteNullValue();
            }
            else
            {
                value.node.WriteTo(writer, options);
            }
        }
    }

    public interface ITest
    {
        /// <summary>
        /// ITest.xx
        /// </summary>
        public int xx { get; set; }
    }

    public class ABC : ITest
    {
        public int x { get; set; }

        public int xx { get; set; }
    }
    public class Point
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? X { get; set; }
        public int Y { get; set; }

        public int[] intarray { get; set; } = new int[2] { 1, 2 };
        public List<int> intlist { get; set; } = new List<int> { 1, 2, 3, 4 };

        public Tuple<int, int> tuple { get; set; } = new Tuple<int, int>(11, 22);

        public (int, int) valuetuple { get; set; } = (111, 222);

        public ABC? abc { get; set; }

        public JsonValue? node { get; set; }

        public MyNode? mynode1 { get; set; }

        public MyNode? mynode2 { get; set; }

        public string @kind { get; set; } = "asdfas";

        //public MyNode<int,string> node1 {get;set;}

        [JsonConverter(typeof(DescriptionConverter))]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Description { get; set; }
    }

    public class DescriptionConverter : JsonConverter<string>
    
    {
        public override bool HandleNull => true;

        public override string Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options) =>
            reader.GetString() ?? "No description provided.";

        public override void Write(
            Utf8JsonWriter writer,
            string value,
            JsonSerializerOptions options) =>
            writer.WriteStringValue(value);
    }

    public class PostionZ{
        public uint z {get; set;} = 444;
    }

    public class Position:PostionZ
    {
        /// <summary>
        /// Line position in a document (zero-based).
        /// </summary>
        public uint line {get;set; }

        /// <summary>
        /// Character offset on a line in a document(zero-based). The meaning of this
        /// offset is determined by the negotiated `PositionEncodingKind`.
        /// 
        /// If the character value is greater than the line length it defaults back
        /// to the line length.
        /// </summary>
        public uint character { get;set;}
    }

    public enum EA{
        A = 1,
        B = 2,
    }

    /// <summary>
    /// JsonStringEnumConverter 默认不区分大小写
    /// </summary>
    // [JsonConverter(typeof(JsonStringEnumConverter))]
    [JsonConverter(typeof(MyJsonEnumConverter))]
    public enum EB{
        AA = 11,
        [EnumMember(Value ="xbb")]
        BB = 22,
        [Description("xclass")]
        @class = 33,
    }

    public class Range
    {
        public string sss => "sss";
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Uri? uri{get;set;}
        public Position start { get; set; }
        public Position end { get; set; }

        // [JsonInclude]
        public EA kind {get; set;} = EA.B;

        public EB? bb {get; set;} = EB.BB;

        public EB[] bbs {get;set;} = new EB[]{EB.AA, EB.BB, EB.@class};

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Position? pos {get; set;}
    }

    [JsonConverter(typeof(NotebookDocumentFilterConverter))]
public class NotebookDocumentFilter
{
    public string? notebookType { get; set;}
    public string? scheme { get; set; }
    public string? pattern { get; set; }
    /// <summary>
    /// 兼容处理下。 lsp 里定义成了 string | NotebookDocumentFilter
    /// </summary>
    public string? type_pattern { get; set; }


    public bool IsValid()
    {
        return type_pattern != null || scheme != null || pattern != null || notebookType != null;
    }
}

public class NotebookDocumentFilterConverter : JsonConverter<NotebookDocumentFilter>
{
    //public override bool HandleNull => true;
    public override NotebookDocumentFilter? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        NotebookDocumentFilter ret = new NotebookDocumentFilter();
        switch (reader.TokenType)
        {
            case JsonTokenType.String:
                ret.type_pattern = reader.GetString();
                break;
            case JsonTokenType.StartObject:
                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndObject)
                    {
                        break;
                    }
                    if (reader.TokenType == JsonTokenType.PropertyName)
                    {
                        var propertyName = reader.GetString();
                        reader.Read();
                        switch (propertyName)
                        {
                            case "notebookType":
                                ret.notebookType = reader.GetString();
                                break;
                            case "scheme":
                                ret.scheme = reader.GetString();
                                break;
                            case "pattern":
                                ret.pattern = reader.GetString();
                                break;
                        }
                    }
                }
                break;
        }
        if (ret.IsValid())
        {
            return ret;
        }
        throw new Exception();
    }

    public override void Write(Utf8JsonWriter writer, NotebookDocumentFilter value, JsonSerializerOptions options)
    {
        if (value.type_pattern == null)
        {
            writer.WriteStartObject();
            if (value.notebookType == null)
            {
                writer.WriteString("notebookType", value.notebookType);
            }
            if (value.scheme == null)
            {
                writer.WriteString("scheme", value.scheme);
            }
            if (value.pattern == null)
            {
                writer.WriteString("pattern", value.pattern);
            }
            writer.WriteEndObject();
        }
        else
        {
            writer.WriteStringValue(value.type_pattern);
        }
    }
}

public class NotebookCellTextDocumentFilter
{
    public NotebookDocumentFilter notebook { get; set; }
    public string? language { get; set; }
}

    public class EmptyObject{

    }

    public class JsonTest
    {

        static T? Get<T>() where T : struct
        {
            return default(T?);
        }

        static T? Get<T>(object? _ = null) where T : class
        {
            return default(T?);
        }
        public static void Run()
        {
            // JsonSerializerOptions.Default.WriteIndented = false;
            using var _ = new LogCall();
            {
                int? str = default;
                string? nullEmpty = null;
                Console.WriteLine(value: $"{nullEmpty} str: {str == null} {str.HasValue} {str.ToString() + 1}");
            }

            {
                string json = """
            {"X":null,"Y":2,"Description11":null,"node":[1,2],"mynode1":{"x1":1234},"mynode3":null,"mynode2":{"my":123,"xxx":["12",213]},"abc":{"x":1,"xx":123},"kind":"abcd"}
            """;

                JsonSerializerOptions options = MyNode.options;

                Point point = JsonSerializer.Deserialize<Point>(json, options)!;
                ITest itest = point.abc!;
                int? xx = point.mynode1!.AsT<int>();
                int? yy = Get<int>();
                ABC? zz = Get<ABC>();
                Console.WriteLine($"Description: {point.Description} itest={null} xx={xx} yy={yy}");
                string str = JsonSerializer.Serialize(point, options);
                Console.WriteLine($"node-type:{point.node?.GetType()} node:{point.node}");
                Console.WriteLine($"json: {str} default={default(int?)}");

                {
                    var doc = JsonDocument.Parse(json);
                    var j = doc.RootElement;
                    JsonElement jnode = j.GetProperty("node");
                    Console.WriteLine($"node:{jnode!}");
                }

                {
                    var node = JsonNode.Parse(json);
                    JsonNode? x = node!["X"];
                    Console.WriteLine($"x={x}");
                    var j = node!["kind"]!.GetValue<JsonElement>();
                    Console.WriteLine($"node:{j}");
                }

            }

            {
                var j = new JsonObject();
                j["x"] = null;
                j["y"] = 2;
                JsonNode? jx = j["x"];
                JsonNode? jz = j["z"];
                JsonNode? jj = JsonNode.Parse("null");
                Console.WriteLine($"{j} x={jx} z={jz} null={jj}");
            
            }

            {
                var range = new Range{
                    start = new Position{line=1,character=2},
                    end = new Position{line=1,character=222},
                    uri = new Uri("http://xx.xx"),
                };
                var s1 = range.ToJsonNode()!.ToJsonStr();
                Console.WriteLine($"range={s1}");
                var n = JsonNode.Parse(s1);
                var range2 = JsonSerializer.Deserialize<Range>(n);
                Console.WriteLine($"range={range2!.uri}");
                var range3 = n!.ConvertTo<Range>();
                Console.WriteLine($"range={range3?.start.character}");
            }

            {
                JsonObject json = new JsonObject();
                json.AddKeyValue("x", new Position{});
                json.AddKeyValue("xs1", new Position[]{new Position{}, new Position{}});
                json.AddKeyValue("xs2", new List<Position>{new Position{}, new Position{}});
                json.AddKeyValue("nil", null);
                Console.WriteLine($"arr={json.ToJsonStr()}");

                Console.WriteLine($"xs1={json["xs1"]!.ConvertTo<Position[]>()}");
                Console.WriteLine($"xs1={json["xs1"]!.ConvertTo<Position[]>()}");
                Console.WriteLine($"xs2={json["xs2"]!.ConvertTo<List<Position>>()}");
            }

            {
                EA aa = EA.B;
                EB bb = EB.BB;
                ValueTuple<EA,EB> xx = (EA.A, EB.AA);
                Tuple<EA,EB> yy = new Tuple<EA, EB>(EA.A, EB.AA);
                Console.WriteLine($"EB = {EB.AA} EA = {EA.A} aa={aa} bb={bb} xx={xx} yy={yy}");
                var json = """{"sss":"abc","uri":"http://xx.xx","start":{"line":1,"character":2,"z":444},"end":{"line":1,"character":222,"z":444},"kind":3,"bb":"aa"}""";
                var range = JsonNode.Parse(json)!.ConvertTo<Range>();
                // c# enum 可以超出范围的呀
                // https://stackoverflow.com/questions/618305/casting-an-out-of-range-number-to-an-enum-in-c-sharp-does-not-produce-an-excepti
                Console.WriteLine($"kind={range?.kind.ToString()} bb={range?.bb.ToString()}");
                // todo 非常特殊。enum.ToString() 一般是按名字变成字符串。但是如果超过范围，按数字变成字符串
            }

            {
                var json = """
                {"notebook":{"scheme":"file","pattern":"**/books1/**","notebookType":"jupyter-notebook"},
                "language":"python",
                "xxx":12
                }
                """;
                var xx = JsonNode.Parse(json)!.ConvertTo<NotebookCellTextDocumentFilter>();
                Console.WriteLine($"xx={xx?.notebook.IsValid()}");
            }
            {
                var t1 = new EmptyObject();
                var t2 = new JsonArray();
                Console.WriteLine($"t1={t1.ToJsonStr()} t2={t2}");
                JsonNode? node = JsonNode.Parse("null");
                var xx = node.ConvertTo<Range>();
                Console.WriteLine($"xx={xx}");
            }
        }
    }
}