using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace MyTest
{
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
                Console.WriteLine($"{j} x={j["x"]} z={j["z"]}");
            }

        }
    }
}