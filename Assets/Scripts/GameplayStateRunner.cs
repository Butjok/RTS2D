using System.Collections.Generic;
using UnityEngine;

public class GameplayStateRunner : WorldBehaviour {

    [SerializeField] private List<GameplayState> states = new();

    public const int maxDepth = 100;

    private void Awake() {
        //states.Add(Create<LevelSessionGameplayState>());
    }

    private void Update() {
        var depth = 0;

        while (true) {
            Debug.Assert(depth < maxDepth);

            if (states.Count == 0)
                return;

            var state = states[states.Count - 1];
            if (state.Enumerator.MoveNext()) {
                var stateChange = state.Enumerator.Current;
                for (var i = 0; i < stateChange.popCount; i++)
                    Pop();
                if (stateChange.newState)
                    Push(stateChange.newState);

                if (stateChange.popCount != 0 || stateChange.newState != null) {
                    depth++;
                    continue;
                }
            }
            else {
                Pop();
                depth++;
                continue;
            }

            break;
        }
    }

    public void Pop(int count = 1, bool popAll = false) {
        if (popAll)
            count = states.Count;
        for (var i = 0; i < count; i++) {
            var state = states[states.Count - 1];
            state.Exit();
            states.RemoveAt(states.Count - 1);
        }
    }

    public void Push(GameplayState state) {
        states.Add(state);
    }

    public T Create<T>() where T : GameplayState {
        var instance = ScriptableObject.CreateInstance<T>();
        instance.gameplayStateRunner = this;
        return instance;
    }
}

public abstract class GameplayState : ScriptableObject {

    public struct Change {
        public GameplayState newState;
        public int popCount;
    }

    public GameplayStateRunner gameplayStateRunner;

    private IEnumerator<Change> enumerator;
    public IEnumerator<Change> Enumerator => enumerator ??= Run();

    public virtual IEnumerator<Change> Run() {
        yield break;
    }

    public virtual void Exit() { }
}