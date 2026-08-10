using UnityEngine;

public class CrackedWall : MonoBehaviour
{
    [SerializeField] int hitsToBreak = 3;
    [SerializeField] GameObject brokenVisualPrefab;

    int remainingHits;
    bool broken;

    void Awake()
    {
        remainingHits = hitsToBreak;
    }

    public void RegisterHit()
    {
        if (broken) return;

        remainingHits--;
        if (remainingHits <= 0)
        {
            Break();
        }
    }

    public void BreakImmediately()
    {
        if (broken) return;
        Break();
    }

    void Break()
    {
        broken = true;

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.enabled = false;
        }

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.enabled = false;
        }

        if (brokenVisualPrefab != null)
        {
            Instantiate(brokenVisualPrefab, transform.position, transform.rotation);
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayWallBreak();
        }
    }
}
