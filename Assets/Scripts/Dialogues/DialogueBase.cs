using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = nameof(DialogueBase), menuName = nameof(DialogueBase))]
public class DialogueBase : ScriptableObject, IEnumerable<DialogueBase.Command> {

    [Serializable]
    public struct Command {
        public enum Type {
            SetSpeaker,
            SayLine
        }

        [SerializeField] private Type type;
        [SerializeField] private DialogueSpeaker speaker;
        [SerializeField] [TextArea(3, 10)] private string line;

        public Type CommandType => type;
        public DialogueSpeaker Speaker => speaker;
        public string Line => line;

        public static Command SetSpeaker(DialogueSpeaker speaker) => new() { type = Type.SetSpeaker, speaker = speaker };
        public static Command SayLine(string line) => new() { type = Type.SayLine, line = line };
    }

    [Serializable]
    private struct LinesBatch {
        [SerializeField] private DialogueSpeaker speaker;
        [SerializeField] [TextArea(3, 10)] private List<string> lines;
        
        public DialogueSpeaker Speaker => speaker;
        public IReadOnlyList<string> Lines => lines;
    }

    [SerializeField] private List<LinesBatch> linesBatches = new();
    [SerializeField] [HideInInspector] private List<Command> commands = new();
    public IReadOnlyList<Command> Commands => commands;

    private void OnValidate() {
        commands.Clear();
        foreach (var batch in linesBatches) {
            commands.Add(Command.SetSpeaker(batch.Speaker));
            foreach (var line in batch.Lines)
                commands.Add(Command.SayLine(line));
        }
    }

    public void Add(Command command) {
        commands.Add(command);
    }

    public IEnumerator<Command> GetEnumerator() {
        return commands.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return ((IEnumerable)commands).GetEnumerator();
    }
}