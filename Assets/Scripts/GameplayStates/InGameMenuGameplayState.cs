using System.Collections.Generic;
using UnityEngine;

public class InGameMenuGameplayState : GameplayStateBase {

    protected override IEnumerator<StateChange> Run() {

        StateMachine.World.PlayerController.enabled = false;
        StateMachine.World.InGameMenuUI.Show();

        yield return default; // consume one frame to skip any key press
        while (!Input.GetKeyDown(KeyCode.Escape))
            yield return default;
        yield return StateChange.Pop();
    }

    public override void Exit() {
        StateMachine.World.InGameMenuUI.Hide();
        StateMachine.World.PlayerController.enabled = true;
    }
}