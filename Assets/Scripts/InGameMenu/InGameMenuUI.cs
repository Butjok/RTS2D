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

using System.IO;
using UnityEngine;

public class InGameMenuUI : WorldBehaviour {

    [SerializeField] private GameObject root;
    [SerializeField] private GameObject settingsMenuRoot;
    [SerializeField] private string settingsPlayerPrefsKey = "GameSettings";

    private GameSettings currentSettings;
    private GameSettings originalSettings;

    private void Awake() {
        Hide();
        originalSettings = LoadSettingsFromPlayerPrefs() ?? GameSettings.Default;
    }

    public void Show() {
        root.SetActive(true);
    }

    public void Hide() {
        root.SetActive(false);
    }

    public void OpenSettingsMenu() {
        settingsMenuRoot.SetActive(true);

        currentSettings = originalSettings.Clone();
    }

    public void CloseSettingsMenu(bool applyChanges) {
        
        if (applyChanges) {
            originalSettings = currentSettings;
            SaveSettingsToPlayerPrefs(currentSettings);
        }
        else
            ApplyVolumeSettings(originalSettings);
        
        settingsMenuRoot.SetActive(false);
    }

    private void ApplyVolumeSettings(GameSettings settings) {
        World.AudioSystem.VolumeMaster = settings.volumeMaster;
        World.AudioSystem.VolumeMusic = settings.volumeMusic;
        World.AudioSystem.VolumeEffects = settings.volumeEffects;
        World.AudioSystem.VolumeVoiceLines = settings.volumeVoiceLines;
    }

    private void SaveSettingsToPlayerPrefs(GameSettings settings) {
        var stringWriter = new StringWriter();
        var serializer = new SimpleReflectionBasedTextSerializer(stringWriter);
        serializer.Serialize(settings);
        PlayerPrefs.SetString(settingsPlayerPrefsKey, stringWriter.ToString());
    }

    private GameSettings LoadSettingsFromPlayerPrefs() {
        if (!PlayerPrefs.HasKey(settingsPlayerPrefsKey))
            return null;
        var stringReader = new StringReader(PlayerPrefs.GetString(settingsPlayerPrefsKey));
        var deserializer = new SimpleReflectionBasedTextSerializer(stringReader);
        var settings = new GameSettings();
        deserializer.Deserialize(settings);
        return settings;
    }
}