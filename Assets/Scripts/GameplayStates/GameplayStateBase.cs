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

public abstract class GameplayStateBase : ScriptableObject {

    public struct StateChange {
        public readonly GameplayStateBase newState;
        public readonly int popCount;

        public StateChange(GameplayStateBase newState = null, int popCount = 0) {
            this.newState = newState;
            this.popCount = popCount;
        }
        public static StateChange Pop(int count = 1) => new(null, count);
        public static StateChange PopAll() => new(null, int.MaxValue);
        public static StateChange Push(GameplayStateBase newState) => new(newState, 0);
    }

    private GameplayStateMachine stateMachine;
    private IEnumerator<StateChange> enumerator;

    public GameplayStateMachine StateMachine => stateMachine;
    public IEnumerator<StateChange> Enumerator => enumerator ??= Run();

    private bool wasInitialized;
    public void Initialize(GameplayStateMachine stateMachine) {
        Debug.Assert(!wasInitialized);
        wasInitialized = true;

        this.stateMachine = stateMachine;
    }

    protected virtual IEnumerator<StateChange> Run() {
        yield break;
    }

    public virtual void Exit() { }
}