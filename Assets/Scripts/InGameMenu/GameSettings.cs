public class GameSettings {

    public static readonly GameSettings Default = new();

    [SaveGame] public float volumeMaster = 1;
    [SaveGame] public float volumeMusic = 1;
    [SaveGame] public float volumeEffects = 1;
    [SaveGame] public float volumeVoiceLines = 1;

    [SaveGame] public bool skipDialogues = false;
}