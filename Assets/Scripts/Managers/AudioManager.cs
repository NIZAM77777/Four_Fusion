using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;


    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;


    [Header("Music")]
    [SerializeField] private AudioClip backgroundMusic;


    [Header("Sound Effects")]
    [SerializeField] private AudioClip buttonClick;
    [SerializeField] private AudioClip piecePlace;
    [SerializeField] private AudioClip win;
    [SerializeField] private AudioClip lose;


    [Header("Volume")]
    [Range(0f, 1f)]
    [SerializeField] private float masterVolume = 1f;


    private const string MASTER_VOLUME_KEY =
        "MasterVolume";


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            DontDestroyOnLoad(gameObject);

            LoadVolume();

            musicSource.clip = backgroundMusic;
            musicSource.loop = true;
            musicSource.Play();

            ApplyVolume();
        }
        else
        {
            Destroy(gameObject);
        }
    }


    //==================================================
    // SOUND EFFECTS
    //==================================================

    public void PlayButtonClick()
    {
        sfxSource.PlayOneShot(buttonClick);
    }


    public void PlayPiecePlace()
    {
        sfxSource.PlayOneShot(piecePlace);
    }


    public void PlayWin()
    {
        sfxSource.PlayOneShot(win);
    }


    public void PlayLose()
    {
        sfxSource.PlayOneShot(lose);
    }


    //==================================================
    // MUSIC
    //==================================================

    public void PlayMusic()
    {
        if (!musicSource.isPlaying)
        {
            musicSource.Play();
        }
    }


    public void StopMusic()
    {
        musicSource.Stop();
    }


    //==================================================
    // MASTER VOLUME
    //==================================================

    public void SetMasterVolume(float volume)
    {
        masterVolume =
            Mathf.Clamp01(volume);

        ApplyVolume();

        PlayerPrefs.SetFloat(
            MASTER_VOLUME_KEY,
            masterVolume);

        PlayerPrefs.Save();
    }


    public float GetMasterVolume()
    {
        return masterVolume;
    }


    private void ApplyVolume()
    {
        musicSource.volume =
            masterVolume;

        sfxSource.volume =
            masterVolume;
    }


    private void LoadVolume()
    {
        masterVolume =
            PlayerPrefs.GetFloat(
                MASTER_VOLUME_KEY,
                1f);
    }
}