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