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