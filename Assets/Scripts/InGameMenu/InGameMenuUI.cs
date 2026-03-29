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