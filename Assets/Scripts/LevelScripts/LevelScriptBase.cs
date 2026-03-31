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

using System.Collections;
using UnityEngine;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class LevelScriptBase : WorldBehaviour {

    private bool playedMatchStartAnimation;
    private IEnumerator animationCoroutine;

    public IEnumerator MatchEndAnimation(bool isVictory) {
        World.PlayerController.enabled = false;

        if (isVictory)
            World.PlayerController.PlayerHUD.ShowVictoryScreen();
        else
            World.PlayerController.PlayerHUD.ShowDefeatScreen();

        while (!Input.GetKeyDown(KeyCode.Space))
            yield return null;

#if UNITY_EDITOR
        EditorApplication.ExitPlaymode();
#else
        Application.Quit();
#endif
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

    protected virtual IEnumerator RequestAnimation() {
        if (!playedMatchStartAnimation) {
            playedMatchStartAnimation = true;
            return MatchStartAnimation();
        }
        return null;
    }

    private void Awake() {
        World.onObjectDestroyed += OnBuildingDestroyed;
    }

    private void OnDestroy() {
        World.onObjectDestroyed -= OnBuildingDestroyed;
    }

    private void OnBuildingDestroyed(Object obj) {
        if (obj is Building building) {
            var buildingOwner = building.OwningPlayer;

            // check if player has any building left
            var hasAnyBuildingsLeft = false;
            foreach (var otherBuilding in World.Buildings)
                if (building != otherBuilding && otherBuilding.OwningPlayer == buildingOwner) {
                    hasAnyBuildingsLeft = true;
                    break;
                }

            if (!hasAnyBuildingsLeft) {
                World.PlayerController.enabled = false;

                var isVictory = buildingOwner.IsAi;
                animationCoroutine = MatchEndAnimation(isVictory);
            }
        }
    }

    private void Update() {
        if (animationCoroutine != null) {
            if (!animationCoroutine.MoveNext())
                animationCoroutine = null;
        }
        if (animationCoroutine == null)
            animationCoroutine = RequestAnimation();
    }
}