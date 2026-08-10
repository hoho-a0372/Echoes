using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [SerializeField] AudioClip pingLaunch;
    [SerializeField] AudioClip normalWallClip;
    [SerializeField] AudioClip crackedWallClip;
    [SerializeField] AudioClip monsterClip;
    [SerializeField] AudioClip wallBreak;
    [SerializeField] AudioClip decoyLand;
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

    public void PlayNormalWallHit(float distance)
    {
        PlayDelayedClip(normalWallClip, distance);
    }

    public void PlayCrackedWallHit(float distance)
    {
        PlayDelayedClip(crackedWallClip, distance);
    }

    public void PlayMonsterHit(float distance)
    {
        PlayDelayedClip(monsterClip, distance);
    }

    void PlayDelayedClip(AudioClip clip, float distance)
    {
        if (clip == null || audioSource == null) return;
        float delay = Mathf.Max(0f, distance / speedOfSound);
        audioSource.clip = clip;
        audioSource.PlayDelayed(delay);
    }

    public void PlayWallBreak()
    {
        PlayOneShot(wallBreak);
    }

    public void PlayDecoyLand()
    {
        PlayOneShot(decoyLand);
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
