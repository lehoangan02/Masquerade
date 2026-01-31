using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Level Music List")]
    public AudioClip[] levelMusicArray;

    [Header("Music Clips")]
    public AudioClip backgroundMusic;
    public AudioClip chaseMusic;
    public AudioClip winMusic;
    public AudioClip loseMusic;

    [Header("SFX Clips")]
    public AudioClip buttonClick;
    public AudioClip enterLevel;
    public AudioClip hurtSound;
    public AudioClip throwMask;
    public AudioClip runInGlass;
    public AudioClip runOnRoad;
    public AudioClip teleportSound;

    [Header("Enemy SFX Clips")]
    public AudioClip enemyDeath;
    public AudioClip enemyCream;
    public AudioClip enemyRoar;


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        PlayMusic(backgroundMusic);
    }

    public void PlayMusic(AudioClip clip)
    {
        if (musicSource.clip == clip) return;

        musicSource.clip = clip;
        musicSource.Play();
    }

    public void PlayMusicByIndex(int index)
    {
        if (index >= 0 && index < levelMusicArray.Length)
        {
            if (levelMusicArray[index] != null)
            {
                musicSource.clip = levelMusicArray[index];
                musicSource.Play();
            }
        }
    }

    public void PlaySFX(AudioClip clip)
    {
        if (clip != null)
        {
            sfxSource.PlayOneShot(clip);
        }
    }
}