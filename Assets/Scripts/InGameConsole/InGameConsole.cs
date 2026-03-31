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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

public class InGameConsole : WorldBehaviour {

    [SerializeField] private int viewRadius = 5;

    private readonly Dictionary<string, InGameConsoleCommand> commands = new();
    private readonly List<InGameConsoleCommand> matches = new();
    private string input = "";
    private int index = -1;
    private Action action;

    private void Awake() {
        foreach (var command in InGameConsoleCommands.Commands)
            commands[command.name] = command;
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private void OnGUI() {
        void FindCompletions() {
            var pattern = string.Join("[^.]*", input.Select(c => Regex.Escape(c.ToString()))) + "[^.]*$";
            var regex = new Regex(pattern, RegexOptions.IgnoreCase);
            matches.Clear();
            matches.AddRange(commands.Values
                .Where(command => regex.IsMatch(command.name))
                .OrderBy(command => Levenshtein.Distance(input, command.name))
                .ThenBy(command => command.name));
        }

        if (Event.current.type == EventType.KeyDown) {
            switch (Event.current.keyCode) {
                case KeyCode.Escape:
                    Event.current.Use();
                    input = "";
                    index = -1;
                    matches.Clear();
                    break;

                case KeyCode.Return:
                    Event.current.Use();
                    var tokens = input.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    var command = commands.GetValueOrDefault(tokens[0]);
                    if (command != null) {
                        var argumentTokens = tokens.Skip(1);
                        var arguments = new List<object>();
                        foreach (var token in argumentTokens) {
                            if (token.ToLowerInvariant() is "true" or "false")
                                arguments.Add(token.ToLowerInvariant() == "true");
                            else if (token.StartsWith("'") && token.EndsWith("'"))
                                arguments.Add(token.Substring(1, token.Length - 2));
                            else if (int.TryParse(token, out var intValue))
                                arguments.Add(intValue);
                            else if (float.TryParse(token, out var floatValue))
                                arguments.Add(floatValue);
                            else
                                Debug.LogWarning($"Could not parse argument token '{token}' as string, int, or float. Treating as string.");
                        }
                        var value = command.Execute(World, arguments);
                        if (value != null)
                            Debug.Log(value);
                        input = "";
                        index = -1;
                        matches.Clear();

                        var textEditor = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
                        textEditor.cursorIndex = 0;
                        textEditor.selectIndex = 0;
                    }
                    else
                        Debug.LogWarning($"No command found with name '{tokens[0]}'.");
                    break;

                case KeyCode.Tab: {
                    Event.current.Use();
                    if (matches.Count == 0) {
                        index = -1;
                        FindCompletions();
                        break;
                    }
                    var offset = Event.current.modifiers.HasFlag(EventModifiers.Shift) ? -1 : 1;
                    index = (index + matches.Count + offset) % matches.Count;
                    var completion = matches[index];
                    input = completion.name + ' ';
                    //if (completion.IsVariable)
                    //    input += completion.Execute(World, Array.Empty<object>()).ToString();
                    action = () => {
                        var textEditor = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
                        textEditor.cursorIndex = input.Length;
                        textEditor.selectIndex = input.Length;
                    };
                    break;
                }
            }
        }
        if (Event.current.type == EventType.Repaint && action != null) {
            action();
            action = null;
        }

        var oldInput = input;
        var rect = GUILayoutUtility.GetRect(new GUIContent(input), GUI.skin.textField, GUILayout.Width(Screen.width));
        input = GUI.TextField(rect, input);
        //GUI.Label(rect, input);
        if (input != oldInput) {
            index = -1;
            FindCompletions();
        }

        var (low, high) = SlidingWindow(matches.Count, index - viewRadius, index + viewRadius);
        for (var i = low; i <= high; i++) {
            var command = matches[i];
            var info = "";
            if (command.IsVariable)
                info = command.Execute(World, Array.Empty<object>()).ToString();
            GUILayout.Label($"[{index + 1}/{matches.Count}] {command.name} {info}");
        }
    }
#endif

    public static (int low, int high) SlidingWindow(int count, int low, int high) {
        if (low < 0 && high >= count) {
            low = 0;
            high = count - 1;
        }
        else if (low < 0) {
            high = Mathf.Min(count - 1, high - low);
            low = 0;
        }
        else if (high >= count) {
            low = Mathf.Max(0, low - high + count - 1);
            high = count - 1;
        }
        return (low, high);
    }
}