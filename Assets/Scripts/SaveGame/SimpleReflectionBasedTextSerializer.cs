using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using UnityEngine;

public class SimpleReflectionBasedTextSerializer {

    public const BindingFlags DefaultBindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

    private readonly TextWriter writer;
    private readonly TextReader reader;

    public SimpleReflectionBasedTextSerializer(TextWriter writer) {
        this.writer = writer;
    }
    public SimpleReflectionBasedTextSerializer(TextReader reader) {
        this.reader = reader;
    }

    private IEnumerable<FieldInfo> EnumerateFields(Type type) {
        return type.GetFields(DefaultBindingFlags).Where(field => field.GetCustomAttribute<SaveGameAttribute>() != null);
    }

    public void Serialize(object obj) {
        Debug.Assert(writer != null);

        foreach (var field in EnumerateFields(obj.GetType()))
            switch (field.GetValue(obj)) {
                case null:
                    writer.WriteLine("null");
                    break;
                case bool boolValue:
                    writer.WriteLine("bool");
                    writer.WriteLine(boolValue);
                    break;
                case int intValue:
                    writer.WriteLine("int");
                    writer.WriteLine(intValue);
                    break;
                case float floatValue:
                    writer.WriteLine("float");
                    writer.WriteLine(floatValue);
                    break;
                case string stringValue:
                    writer.WriteLine("string");
                    writer.WriteLine(EscapeString(stringValue));
                    break;
            }
    }
    
    public void Deserialize(object obj) {
        Debug.Assert(reader != null);

        foreach (var field in EnumerateFields(obj.GetType())) {
            var typeString = ReadLine();
            object value = null;
            switch (typeString) {
                case "null":
                    break;
                case "bool":
                    value = bool.Parse(ReadLine());
                    break;
                case "int":
                    value = int.Parse(ReadLine());
                    break;
                case "float":
                    value = float.Parse(ReadLine());
                    break;
                case "string":
                    value = UnescapeString(ReadLine());
                    break;
            }
            field.SetValue(obj, value);
        }
    }
    
    private string ReadLine() {
        var line = reader.ReadLine();
        Debug.Assert(line != null);
        return line;
    }
    private static string EscapeString(string str) {
        return WebUtility.UrlEncode(str);
    }
    private static string UnescapeString(string str) {
        return WebUtility.UrlDecode(str);
    }
}