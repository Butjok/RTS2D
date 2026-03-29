using System.IO;

public class GameSettings {

    public static readonly GameSettings Default = new();

    [SaveGame] public float volumeMaster = 1;
    [SaveGame] public float volumeMusic = 1;
    [SaveGame] public float volumeEffects = 1;
    [SaveGame] public float volumeVoiceLines = 1;

    [SaveGame] public bool skipDialogues = false;

    public GameSettings Clone() {
        var stringWriter = new StringWriter();
        var serializer = new SimpleReflectionBasedTextSerializer(stringWriter);
        serializer.Serialize(this);
        var stringReader = new StringReader(stringWriter.ToString());
        var deserializer = new SimpleReflectionBasedTextSerializer(stringReader);
        var clone = new GameSettings();
        deserializer.Deserialize(clone);
        return clone;
    }
}