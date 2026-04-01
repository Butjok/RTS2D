// Copyright 2026 Viktor Fedotov
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using UnityEngine;

/*
 * This is a very simple implementation of automatic serialization of object fields based on the reflection information.
 * Right now this is only used to save and load game settings.
 * In the future this might be used to actually save and load game state, to support references to objects, support spawning of objects, versioning.
 * Also this should be implemented as a code generation rather than being based on reflection.
 * - Viktor Fedotov 01.04.2026
 */

public class SimpleReflectionBasedTextSerializer {

    public const BindingFlags defaultBindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

    private readonly TextWriter writer;
    private readonly TextReader reader;

    public SimpleReflectionBasedTextSerializer(TextWriter writer) {
        this.writer = writer;
    }
    public SimpleReflectionBasedTextSerializer(TextReader reader) {
        this.reader = reader;
    }

    private static IEnumerable<FieldInfo> EnumerateFields(IReflect type) {
        return type.GetFields(defaultBindingFlags).Where(field => field.GetCustomAttribute<SaveGameAttribute>() != null);
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
                    writer.WriteLine(floatValue.ToString(CultureInfo.InvariantCulture));
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
                    value = float.Parse(ReadLine(), CultureInfo.InvariantCulture);
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