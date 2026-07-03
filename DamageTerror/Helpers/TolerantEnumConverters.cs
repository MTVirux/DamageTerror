namespace DamageTerror.Helpers;

/// <summary>
/// Tolerant JSON converter for the enum-backed collection shapes used across the
/// configuration. Silently skips values (or dictionary keys) that are not
/// recognized members of the enum type. Handles three shapes:
/// <list type="bullet">
/// <item><c>List&lt;TEnum&gt;</c> / <c>HashSet&lt;TEnum&gt;</c></item>
/// <item><c>Dictionary&lt;TEnum, TValue&gt;</c> (enum key)</item>
/// <item><c>Dictionary&lt;string, List&lt;TEnum&gt;&gt;</c></item>
/// </list>
/// </summary>
public sealed class TolerantEnumConverter : JsonConverter
{
    private enum Shape { None, EnumCollection, EnumKeyDictionary, StringEnumListMap }

    private static Shape DetectShape(Type type)
    {
        if (!type.IsGenericType) return Shape.None;

        var def = type.GetGenericTypeDefinition();
        var args = type.GetGenericArguments();

        if (def == typeof(List<>) || def == typeof(HashSet<>))
            return args[0].IsEnum ? Shape.EnumCollection : Shape.None;

        if (def == typeof(Dictionary<,>))
        {
            if (args[0].IsEnum) return Shape.EnumKeyDictionary;
            if (args[0] == typeof(string) && args[1].IsGenericType
                && args[1].GetGenericTypeDefinition() == typeof(List<>)
                && args[1].GetGenericArguments()[0].IsEnum)
                return Shape.StringEnumListMap;
        }

        return Shape.None;
    }

    public override bool CanConvert(Type objectType) => DetectShape(objectType) != Shape.None;

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
            return null;

        return DetectShape(objectType) switch
        {
            Shape.EnumCollection => ReadCollection(reader, objectType),
            Shape.EnumKeyDictionary => ReadKeyDictionary(reader, objectType, serializer),
            Shape.StringEnumListMap => ReadListMap(reader, objectType),
            _ => null,
        };
    }

    private static object ReadCollection(JsonReader reader, Type objectType)
    {
        var elemType = objectType.GetGenericArguments()[0];
        var collection = Activator.CreateInstance(objectType)!;
        var addMethod = objectType.GetMethod("Add")!;

        TolerantEnumParsing.ParseEnumArray(JArray.Load(reader), elemType,
            val => addMethod.Invoke(collection, new[] { val }));

        return collection;
    }

    private static object ReadKeyDictionary(JsonReader reader, Type objectType, JsonSerializer serializer)
    {
        var args = objectType.GetGenericArguments();
        var keyType = args[0];
        var valueType = args[1];
        var dict = (System.Collections.IDictionary)Activator.CreateInstance(objectType)!;
        var obj = JObject.Load(reader);

        foreach (var prop in obj.Properties())
        {
            if (Enum.TryParse(keyType, prop.Name, out var key) && key != null)
            {
                try
                {
                    var val = prop.Value.ToObject(valueType, serializer);
                    dict[key] = val;
                }
                catch
                {
                    // Skip values that fail to deserialize too
                }
            }
        }

        return dict;
    }

    private static object ReadListMap(JsonReader reader, Type objectType)
    {
        var valueListType = objectType.GetGenericArguments()[1];
        var elemType = valueListType.GetGenericArguments()[0];
        var dict = (System.Collections.IDictionary)Activator.CreateInstance(objectType)!;
        var obj = JObject.Load(reader);

        foreach (var prop in obj.Properties())
        {
            var list = (System.Collections.IList)Activator.CreateInstance(valueListType)!;

            if (prop.Value is JArray arr)
                TolerantEnumParsing.ParseEnumArray(arr, elemType, val => list.Add(val));

            dict[prop.Name] = list;
        }

        return dict;
    }

    public override bool CanWrite => true;

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value == null) { writer.WriteNull(); return; }

        switch (DetectShape(value.GetType()))
        {
            case Shape.EnumCollection:
                WriteEnumArray(writer, (System.Collections.IEnumerable)value);
                break;

            case Shape.EnumKeyDictionary:
                writer.WriteStartObject();
                foreach (System.Collections.DictionaryEntry entry in (System.Collections.IDictionary)value)
                {
                    writer.WritePropertyName(entry.Key.ToString()!);
                    serializer.Serialize(writer, entry.Value);
                }
                writer.WriteEndObject();
                break;

            case Shape.StringEnumListMap:
                writer.WriteStartObject();
                foreach (System.Collections.DictionaryEntry entry in (System.Collections.IDictionary)value)
                {
                    writer.WritePropertyName(entry.Key.ToString()!);
                    WriteEnumArray(writer, (System.Collections.IEnumerable)entry.Value!);
                }
                writer.WriteEndObject();
                break;
        }
    }

    private static void WriteEnumArray(JsonWriter writer, System.Collections.IEnumerable items)
    {
        writer.WriteStartArray();
        foreach (var item in items)
            writer.WriteValue(item.ToString());
        writer.WriteEndArray();
    }
}

file static class TolerantEnumParsing
{
    public static void ParseEnumArray(JArray arr, Type elemType, Action<object> add)
    {
        foreach (var token in arr)
        {
            if (token.Type == JTokenType.String)
            {
                var str = token.Value<string>();
                if (str != null && Enum.TryParse(elemType, str, out var val) && val != null)
                    add(val);
            }
            else if (token.Type == JTokenType.Integer)
            {
                var intVal = token.Value<int>();
                if (Enum.IsDefined(elemType, intVal))
                    add(Enum.ToObject(elemType, intVal));
            }
        }
    }
}
