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
using UnityEngine;

public abstract class InGameConsoleCommand {
    public readonly string name;
    public readonly string description;
    public virtual bool IsVariable => false;
    public abstract object Execute(World world, IReadOnlyList<object> arguments);

    protected InGameConsoleCommand(string name, string description = null) {
        if (name.EndsWith("InGameConsoleCommand"))
            name = name.Substring(0, name.Length - "InGameConsoleCommand".Length);
        this.name = name;
        this.description = description;
    }
}

public abstract class BooleanInGameConsoleCommand : InGameConsoleCommand {
    public override bool IsVariable => true;

    protected BooleanInGameConsoleCommand(string name, string description = null) : base(name, description) { }

    protected abstract bool GetValue(World world);
    protected abstract void SetValue(World world, bool value);
    
    public override object Execute(World world, IReadOnlyList<object> arguments) {
        if (arguments.Count == 0)
            return GetValue(world);

        Debug.Assert(arguments.Count == 1, $"Expected exactly one argument for command '{name}', but got {arguments.Count}.");
        Debug.Assert(arguments[0] is bool or int, $"Expected argument for command '{name}' to be of type bool or int, but got {arguments[0].GetType()}.");

        bool value = false;
        if (arguments[0] is bool boolValue)
            value = boolValue;
        else if (arguments[0] is int intValue)
            value = intValue != 0;
        SetValue(world, value);
        return null;
    }
}