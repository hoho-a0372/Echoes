using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [SerializeField] AudioClip pingLaunch;
    [SerializeField] AudioClip wallHit;
    [SerializeField] AudioClip enemyReveal;
    [SerializeField] AudioClip playerDeath;
    [SerializeField] AudioClip stageClear;

    const float speedOfSound = 20f;

    AudioSource audioSource;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        audioSource = GetComponent<AudioSource>();
    }

    public void PlayPingLaunch()
    {
        PlayOneShot(pingLaunch);
    }

    public void PlayWallHit(float distance)
    {
        if (wallHit == null || audioSource == null) return;
        float delay = Mathf.Max(0f, distance / speedOfSound);
        audioSource.clip = wallHit;
        audioSource.PlayDelayed(delay);
    }

    public void PlayEnemyReveal()
    {
        PlayOneShot(enemyReveal);
    }

    public void PlayDeath()
    {
        PlayOneShot(playerDeath);
    }

    public void PlayStageClear()
    {
        PlayOneShot(stageClear);
    }

    void PlayOneShot(AudioClip clip)
    {
        if (clip == null || audioSource == null) return;
        audioSource.PlayOneShot(clip);
    }
}
