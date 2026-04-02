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
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * The dialogues are basically a list of dialogue commands.
 * Currently there are two types of commands: SetSpeaker and SayLine. In the future we might support commands like OptionSelect, GameTrigger etc.
 *
 * To make editing of the dialogue nicer in inspector we have a list of LinesBatch, which is basically a speaker and a list of lines.
 * OnValidate we convert this list to the list of commands.
 */

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