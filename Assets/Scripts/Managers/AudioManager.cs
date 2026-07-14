using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [SerializeField] private AudioSource sfxSource;

    [SerializeField] private AudioClip pieceDropClip;
    [SerializeField] private AudioClip victoryClip;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void PlayPieceDrop()
    {
        sfxSource.PlayOneShot(pieceDropClip);
    }

    public void PlayVictory()
    {
        sfxSource.PlayOneShot(victoryClip);
    }
}