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
    [SerializeField] AudioClip collectiblePickup;

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

    public void PlayNormalWallHit()
    {
        PlayOneShot(normalWallClip);
    }

    public void PlayCrackedWallHit()
    {
        PlayOneShot(crackedWallClip);
    }

    public void PlayMonsterHit()
    {
        PlayOneShot(monsterClip);
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

    public void PlayCollectiblePickup()
    {
        PlayOneShot(collectiblePickup);
    }

    void PlayOneShot(AudioClip clip)
    {
        if (clip == null || audioSource == null) return;
        audioSource.PlayOneShot(clip);
    }
}
