using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using Random = UnityEngine.Random;

[RequireComponent(typeof(AudioSource))]
public class AudioSystem : WorldBehaviour {

    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private Vector2 volumeRange = new(0, 1);

    [SerializeField] private List<AudioClip> musicClips = new();
    [SerializeField] private int nextMusicClipIndex = 0;

    [SerializeField] private AudioSource musicAudioSource;
    [SerializeField] private AudioSource unitVoiceAudioSourceForeground;
    [SerializeField] private AudioSource unitVoiceAudioSourceBackground;
    [SerializeField] private AudioSource announcerAudioSource;

    [SerializeField] private AudioClip announcer_battleControlActivated;
    [SerializeField] private AudioClip announcer_battleControlDeactivated;
    [SerializeField] private AudioClip announcer_building;
    [SerializeField] private AudioClip announcer_constructionComplete;
    [SerializeField] private AudioClip announcer_incomingTransmission;
    [SerializeField] private AudioClip announcer_insufficientFunds;
    [SerializeField] private AudioClip announcer_missionCompleted;
    [SerializeField] private AudioClip announcer_newConstructionOptions;
    [SerializeField] private AudioClip announcer_newObjective;
    [SerializeField] private AudioClip announcer_unitLost;
    [SerializeField] private AudioClip announcer_unitReady;

    public AudioClip AnnouncerBattleControlActivated => announcer_battleControlActivated;
    public AudioClip AnnouncerBattleControlDeactivated => announcer_battleControlDeactivated;
    public AudioClip AnnouncerBuilding => announcer_building;
    public AudioClip AnnouncerConstructionComplete => announcer_constructionComplete;
    public AudioClip AnnouncerIncomingTransmission => announcer_incomingTransmission;
    public AudioClip AnnouncerInsufficientFunds => announcer_insufficientFunds;
    public AudioClip AnnouncerMissionCompleted => announcer_missionCompleted;
    public AudioClip AnnouncerNewConstructionOptions => announcer_newConstructionOptions;
    public AudioClip AnnouncerNewObjective => announcer_newObjective;
    public AudioClip AnnouncerUnitLost => announcer_unitLost;
    public AudioClip AnnouncerUnitReady => announcer_unitReady;

    private readonly Dictionary<AudioClip, float> lastTimeAudioClipWasPlayed = new();
    private AudioClip lastSaidUnitVoiceLineClip;
    private IEnumerator musicPlayingLoopCoroutine;

    private void OnValidate() {
        if (musicAudioSource == null)
            musicAudioSource = GetComponent<AudioSource>();
    }

    private void Awake() {
        World.PlayerController.OwningPlayer.onBuildingConstructionComplete += OnBuildingConstructionComplete;
        World.PlayerController.OwningPlayer.onUnitConstructionComplete += OnUnitConstructionComplete;
    }

    private void OnDestroy() {
        World.PlayerController.OwningPlayer.onBuildingConstructionComplete -= OnBuildingConstructionComplete;
        World.PlayerController.OwningPlayer.onUnitConstructionComplete -= OnUnitConstructionComplete;
    }

    private void OnBuildingConstructionComplete(Building factory, Building buildingPrefab) {
        announcerAudioSource.PlayOneShot(announcer_constructionComplete);
    }
    private void OnUnitConstructionComplete(Building factory, Unit unitPrefab) {
        announcerAudioSource.PlayOneShot(announcer_unitReady);
    }

    private void Start() {
        musicPlayingLoopCoroutine = MusicPlayingLoop();
        StartCoroutine(musicPlayingLoopCoroutine);
    }

    private IEnumerator MusicPlayingLoop() {
        while (true) {
            musicAudioSource.clip = musicClips[nextMusicClipIndex];
            nextMusicClipIndex = (nextMusicClipIndex + 1) % musicClips.Count;
            musicAudioSource.Play();
            while (musicAudioSource.isPlaying)
                yield return null;
        }
    }

    private IEnumerator FadeAwayAudioSource(AudioSource audioSource, float duration = .125f) {
        var startVolume = audioSource.volume;
        var timeElapsed = 0f;
        while (timeElapsed < duration) {
            audioSource.volume = Mathf.Lerp(startVolume, 0, timeElapsed / duration);
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        audioSource.Stop();
        audioSource.volume = startVolume;
    }

    public void SayVoiceLine(Unit unit, AudioClip voiceLineClip) {
        StartCoroutine(FadeAwayAudioSource(unitVoiceAudioSourceForeground));
        (unitVoiceAudioSourceForeground, unitVoiceAudioSourceBackground) = (unitVoiceAudioSourceBackground, unitVoiceAudioSourceForeground);
        unitVoiceAudioSourceForeground.clip = voiceLineClip;
        unitVoiceAudioSourceForeground.Play();
        lastSaidUnitVoiceLineClip = voiceLineClip;
    }

    public void SayRandomVoiceLine(Unit unit, IReadOnlyList<AudioClip> voiceLineClips) {
        if (voiceLineClips.Count == 0)
            return;
        var randomIndex = Random.Range(0, voiceLineClips.Count);
        var randomClip = voiceLineClips[randomIndex];
        if (voiceLineClips.Count > 1 && randomClip == lastSaidUnitVoiceLineClip) {
            randomIndex = (randomIndex + 1) % voiceLineClips.Count;
            randomClip = voiceLineClips[randomIndex];
        }

        SayVoiceLine(unit, randomClip);
    }

    public void SayAnnouncerVoiceLine(AudioClip voiceLineClip) {
        announcerAudioSource.PlayOneShot(voiceLineClip);
    }

    public void PlayOneShotWithCooldown(AudioSource source, AudioClip clip) {
        if (!lastTimeAudioClipWasPlayed.TryGetValue(clip, out var lastPlayedTime))
            lastPlayedTime = -Mathf.Infinity;
        var elapsedTime = Time.time - lastPlayedTime;
        if (elapsedTime >= .1f) {
            lastTimeAudioClipWasPlayed[clip] = Time.time;
            source.PlayOneShot(clip);
        }
    }

    public void PlayRandomOneShotWithCooldown(AudioSource source, IReadOnlyList<AudioClip> clips) {
        Debug.Assert(clips.Count > 0, "Cannot play random one shot with cooldown if there are no clips");
        var randomIndex = Random.Range(0, clips.Count);
        PlayOneShotWithCooldown(source, clips[randomIndex]);
    }

    public float VolumeMaster {
        set => audioMixer.SetFloat("VolumeMaster", Mathf.Lerp(volumeRange[0], volumeRange[1], value));
    }

    public float VolumeMusic {
        set => audioMixer.SetFloat("VolumeMusic", Mathf.Lerp(volumeRange[0], volumeRange[1], value));
    }

    public float VolumeEffects {
        set => audioMixer.SetFloat("VolumeEffects", Mathf.Lerp(volumeRange[0], volumeRange[1], value));
    }

    public float VolumeVoiceLines {
        set => audioMixer.SetFloat("VolumeVoiceLines", Mathf.Lerp(volumeRange[0], volumeRange[1], value));
    }
}