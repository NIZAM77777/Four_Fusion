using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip backgroundMusic;
    [SerializeField] private AudioClip buttonClick;
    [SerializeField] private AudioClip piecePlace;
    [SerializeField] private AudioClip win;
    [SerializeField] private AudioClip lose;
    [Range(0, 1)] [SerializeField] private float masterVolume = 1;
    private bool dirty;
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        masterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat("MasterVolume", 1));
        if (musicSource == null) musicSource = gameObject.AddComponent<AudioSource>();
        if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
        musicSource.playOnAwake = sfxSource.playOnAwake = false;
        musicSource.clip = backgroundMusic;
        musicSource.loop = true;
        RefreshSettings();
        PlayMusic();
    }
    public void PlayButtonClick() => Play(buttonClick);
    public void PlayPiecePlace() => Play(piecePlace);
    public void PlayWin() => Play(win);
    public void PlayLose() => Play(lose);
    public void PlayDraw() => Play(buttonClick);
    private void Play(AudioClip clip) { if (clip != null && sfxSource != null && PlayerSettingsStore.SoundEnabled) sfxSource.PlayOneShot(clip); }
    public void PlayMusic() { if (musicSource != null && musicSource.clip != null && !musicSource.isPlaying) musicSource.Play(); }
    public void StopMusic() { if (musicSource != null) musicSource.Pause(); }
    public void RefreshSettings()
    {
        if (musicSource != null) { musicSource.volume = masterVolume; musicSource.mute = !PlayerSettingsStore.MusicEnabled; }
        if (sfxSource != null) { sfxSource.volume = masterVolume; sfxSource.mute = !PlayerSettingsStore.SoundEnabled; }
    }
    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        RefreshSettings();
        PlayerPrefs.SetFloat("MasterVolume", masterVolume);
        dirty = true;
    }
    public float GetMasterVolume() => masterVolume;
    public void SaveSettings() { if (dirty) { PlayerPrefs.Save(); dirty = false; } }
    private void OnApplicationPause(bool paused) { if (paused) SaveSettings(); }
    private void OnApplicationQuit() => SaveSettings();
    private void OnDestroy() { if (Instance == this) { SaveSettings(); Instance = null; } }
}
