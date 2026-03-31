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

#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
public static class PrePlacedInfoBeforePlay {

    static PrePlacedInfoBeforePlay() {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange change) {

        var prePlacedPlayerProperties = Object.FindObjectsByType<PrePlacedInfo>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var aboutToPlay = change == PlayModeStateChange.ExitingEditMode;
        var shouldBeDisabled = aboutToPlay;

        foreach (var prePlacedPlayerProperty in prePlacedPlayerProperties)
            prePlacedPlayerProperty.gameObject.SetActive(!shouldBeDisabled);

        var aboutToOpenEditor = change == PlayModeStateChange.EnteredEditMode;
        if (aboutToOpenEditor) {
            var prePlacedInfos = Object.FindObjectsByType<PrePlacedInfo>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var prePlacedInfo in prePlacedInfos)
                prePlacedInfo.UpdateInEditorPlayerColor();
        }
    }
}

#endif