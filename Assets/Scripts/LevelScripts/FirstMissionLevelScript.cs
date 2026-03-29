using System.Collections;
using UnityEngine;

public class FirstMissionLevelScript : LevelScriptBase {

    [SerializeField] private bool skipIntroDialogue;
    [SerializeField] private DialogueBase introDialogue;
    private bool playedIntroDialogue;

    public override IEnumerator RequestAnimation() {
        if (!playedIntroDialogue && !skipIntroDialogue) {
            playedIntroDialogue = true;
            return DialogueAnimation(introDialogue);
        }
        return base.RequestAnimation();
    }
}