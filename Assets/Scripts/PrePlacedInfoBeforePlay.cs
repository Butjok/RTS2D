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