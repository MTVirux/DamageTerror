using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DamageTerror.Helpers;

/// <summary>
/// Deserializes <c>Dictionary&lt;TEnum, TValue&gt;</c> while silently skipping
/// entries whose key is not a recognized member of the enum type.
/// </summary>
public class TolerantEnumKeyDictionaryConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        if (!objectType.IsGenericType) return false;
        var def = objectType.GetGenericTypeDefinition();
        return def == typeof(Dictionary<,>) && objectType.GetGenericArguments()[0].IsEnum;
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
            return null;

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

    public override bool CanWrite => true;

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value == null) { writer.WriteNull(); return; }

        var dict = (System.Collections.IDictionary)value;
        writer.WriteStartObject();
        foreach (System.Collections.DictionaryEntry entry in dict)
        {
            writer.WritePropertyName(entry.Key.ToString()!);
            serializer.Serialize(writer, entry.Value);
        }
        writer.WriteEndObject();
    }
}

/// <summary>
/// Deserializes <c>List&lt;TEnum&gt;</c> or <c>HashSet&lt;TEnum&gt;</c> while
/// silently skipping values that are not recognized members of the enum type.
/// </summary>
public class TolerantEnumCollectionConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        if (!objectType.IsGenericType) return false;
        var def = objectType.GetGenericTypeDefinition();
        return (def == typeof(List<>) || def == typeof(HashSet<>))
               && objectType.GetGenericArguments()[0].IsEnum;
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
            return null;

        var elemType = objectType.GetGenericArguments()[0];
        var collection = Activator.CreateInstance(objectType)!;
        var addMethod = objectType.GetMethod("Add")!;
        var arr = JArray.Load(reader);

        foreach (var token in arr)
        {
            if (token.Type == JTokenType.String)
            {
                var str = token.Value<string>();
                if (str != null && Enum.TryParse(elemType, str, out var val) && val != null)
                    addMethod.Invoke(collection, new[] { val });
            }
            else if (token.Type == JTokenType.Integer)
            {
                var intVal = token.Value<int>();
                if (Enum.IsDefined(elemType, intVal))
                    addMethod.Invoke(collection, new[] { Enum.ToObject(elemType, intVal) });
            }
        }

        return collection;
    }

    public override bool CanWrite => true;

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value == null) { writer.WriteNull(); return; }

        writer.WriteStartArray();
        foreach (var item in (System.Collections.IEnumerable)value)
            writer.WriteValue(item.ToString());
        writer.WriteEndArray();
    }
}

/// <summary>
/// Deserializes <c>Dictionary&lt;string, List&lt;TEnum&gt;&gt;</c> while silently
/// skipping unrecognized enum values inside the inner lists.
/// </summary>
public class TolerantEnumListMapConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        if (!objectType.IsGenericType) return false;
        var def = objectType.GetGenericTypeDefinition();
        if (def != typeof(Dictionary<,>)) return false;
        var args = objectType.GetGenericArguments();
        if (args[0] != typeof(string)) return false;
        if (!args[1].IsGenericType) return false;
        var innerDef = args[1].GetGenericTypeDefinition();
        return innerDef == typeof(List<>) && args[1].GetGenericArguments()[0].IsEnum;
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
            return null;

        var valueListType = objectType.GetGenericArguments()[1];
        var elemType = valueListType.GetGenericArguments()[0];
        var dict = (System.Collections.IDictionary)Activator.CreateInstance(objectType)!;
        var obj = JObject.Load(reader);

        foreach (var prop in obj.Properties())
        {
            var list = (System.Collections.IList)Activator.CreateInstance(valueListType)!;

            if (prop.Value is JArray arr)
            {
                foreach (var token in arr)
                {
                    if (token.Type == JTokenType.String)
                    {
                        var str = token.Value<string>();
                        if (str != null && Enum.TryParse(elemType, str, out var val) && val != null)
                            list.Add(val);
                    }
                    else if (token.Type == JTokenType.Integer)
                    {
                        var intVal = token.Value<int>();
                        if (Enum.IsDefined(elemType, intVal))
                            list.Add(Enum.ToObject(elemType, intVal));
                    }
                }
            }

            dict[prop.Name] = list;
        }

        return dict;
    }

    public override bool CanWrite => true;

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value == null) { writer.WriteNull(); return; }

        var dict = (System.Collections.IDictionary)value;
        writer.WriteStartObject();
        foreach (System.Collections.DictionaryEntry entry in dict)
        {
            writer.WritePropertyName(entry.Key.ToString()!);
            var list = (System.Collections.IEnumerable)entry.Value!;
            writer.WriteStartArray();
            foreach (var item in list)
                writer.WriteValue(item.ToString());
            writer.WriteEndArray();
        }
        writer.WriteEndObject();
    }
}
