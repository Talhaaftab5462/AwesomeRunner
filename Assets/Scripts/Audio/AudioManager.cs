using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Clips")]
    [SerializeField] private AudioClip music;
    [SerializeField] private AudioClip coinClip;
    [SerializeField] private AudioClip jumpClip;
    [SerializeField] private AudioClip slideClip;
    [SerializeField] private AudioClip hitClip;
    [SerializeField] private AudioClip gameOverClip;

    [Header("Volume")]
    [Range(0f, 1f)] [SerializeField] private float musicVolume = 0.4f;
    [Range(0f, 1f)] [SerializeField] private float sfxVolume = 1f;

    private AudioSource musicSource;
    private AudioSource sfxSource;

    private void Awake()
    {
        Instance = this;

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.playOnAwake = false;
        musicSource.volume = musicVolume;
        musicSource.clip = music;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;

        if (music != null)
            musicSource.Play();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void PlaySfx(AudioClip clip)
    {
        if (clip != null)
            sfxSource.PlayOneShot(clip, sfxVolume);
    }

    public static void PlayCoin()     { if (Instance) Instance.PlaySfx(Instance.coinClip); }
    public static void PlayJump()     { if (Instance) Instance.PlaySfx(Instance.jumpClip); }
    public static void PlaySlide()    { if (Instance) Instance.PlaySfx(Instance.slideClip); }
    public static void PlayHit()      { if (Instance) Instance.PlaySfx(Instance.hitClip); }
    public static void PlayGameOver()
    {
        if (!Instance) return;
        Instance.musicSource.Stop();
        Instance.PlaySfx(Instance.gameOverClip);
    }
}