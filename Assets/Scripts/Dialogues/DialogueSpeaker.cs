using UnityEngine;

[CreateAssetMenu(fileName = nameof(DialogueSpeaker), menuName = nameof(DialogueSpeaker))]
public class DialogueSpeaker : ScriptableObject {
    [SerializeField] private string speakerSpeakerName;
    [SerializeField] private Sprite speakerPortrait;
    
    public string SpeakerName => speakerSpeakerName;
    public Sprite SpeakerPortrait => speakerPortrait;
}