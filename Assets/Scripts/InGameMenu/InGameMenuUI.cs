using System.IO;
using UnityEngine;

public class InGameMenuUI : WorldBehaviour {

    [SerializeField] private GameObject root;
    [SerializeField] private GameObject settingsMenuRoot;
    [SerializeField] private string settingsPlayerPrefsKey = "GameSettings";

    private StringWriter stringWriter;
    private SimpleReflectionBasedTextSerializer serializer;
    private GameSettings currentSettings;

    private void Awake() {
        stringWriter = new StringWriter();
        serializer = new SimpleReflectionBasedTextSerializer(stringWriter);
        currentSettings = LoadSettings() ?? new GameSettings();

        Hide();
    }

    public void Show() {
        root.SetActive(true);
    }
    public void Hide() {
        root.SetActive(false);
    }

    public void OpenSettingsMenu() {
        settingsMenuRoot.SetActive(true);
    }
    public void CloseSettingsMenu() {
        settingsMenuRoot.SetActive(false);
    }

    public void SaveSettings(ref GameSettings settings) {
        stringWriter.GetStringBuilder().Clear();
        serializer.Serialize(settings);
        PlayerPrefs.SetString(settingsPlayerPrefsKey, stringWriter.ToString());
    }
    public GameSettings LoadSettings() {
        if (!PlayerPrefs.HasKey(settingsPlayerPrefsKey))
            return null;
        var stringReader = new StringReader(PlayerPrefs.GetString(settingsPlayerPrefsKey));
        var deserializer = new SimpleReflectionBasedTextSerializer(stringReader);
        var settings = new GameSettings();
        deserializer.Deserialize(settings);
        return settings;
    }
}