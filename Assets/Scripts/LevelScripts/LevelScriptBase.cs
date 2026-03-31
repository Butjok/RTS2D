using System.Collections;
using UnityEngine;

public class LevelScriptBase : WorldBehaviour {
    private bool playedMatchStartAnimation;

    public IEnumerator MatchEndAnimation(bool isVictory) {
        World.PlayerController.enabled = false;

        if (isVictory)
            World.PlayerController.PlayerHUD.ShowVictoryScreen();
        else
            World.PlayerController.PlayerHUD.ShowDefeatScreen();
        
        while (!Input.GetKeyDown(KeyCode.Space))
            yield return null;
        yield return null; // consume key press
    }

    protected IEnumerator DialogueAnimation(DialogueBase dialogue) {
        
        World.DialogueUI.Show();
        
        foreach (var command in dialogue.Commands)
            switch (command.CommandType) {

                case DialogueBase.Command.Type.SetSpeaker:
                    World.DialogueUI.SetSpeaker(command.Speaker);
                    break;

                case DialogueBase.Command.Type.SayLine:

                    World.DialogueUI.SetLine(command.Line);

                    while (!Input.GetKeyDown(KeyCode.Space))
                        yield return null;
                    yield return null; // consume key press

                    if (!World.DialogueUI.IsTypingCompleted) {
                        World.DialogueUI.InterruptTyping();
                        
                        while (!Input.GetKeyDown(KeyCode.Space))
                            yield return null;
                        yield return null; // consume key press
                    }
                    break;
            }
        
        World.DialogueUI.Hide();
    }

    protected IEnumerator MatchStartAnimation() {
        World.PlayerController.NotifyBattleControlActivated();
        World.PlayerController.enabled = true;
        yield break;
    }

    public virtual IEnumerator RequestAnimation() {
        if (!playedMatchStartAnimation) {
            playedMatchStartAnimation = true;
            return MatchStartAnimation();
        }
        return null;
    }
}