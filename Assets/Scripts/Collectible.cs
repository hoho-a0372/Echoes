using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// Optional hidden pickup - findable only by exploring off the critical path
// (dead ends, behind CrackedWalls, plaza corners). Purely a completion
// tracker; doesn't affect gameplay mechanics.
public class Collectible : MonoBehaviour, IRevealable
{
    [SerializeField] string collectibleId; // convention: "stage{N}_item{M}"

    bool collected;
    Coroutine revealCoroutine;
    float dimAlpha = 0.35f;
    SpriteRenderer sr;
    UnityEngine.Rendering.Universal.Light2D light;

    void Awake()
    {
        light = GetComponent<UnityEngine.Rendering.Universal.Light2D>();
        if (light != null)
        {
            light.enabled = false;
        }
        sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.enabled = true;
            SetAlpha(dimAlpha);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (collected) return;
        if (!other.CompareTag("Player")) return;

        collected = true;

        if (ProgressManager.Instance != null)
        {
            ProgressManager.Instance.MarkCollectibleFound(collectibleId);
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayCollectiblePickup();
        }

        if (CameraShake.Instance != null)
        {
            CameraShake.Instance.Shake(0.08f, 0.06f);
        }

        gameObject.SetActive(false);
    }

    public void Reveal(float duration)
    {
        if (revealCoroutine != null)
        {
            StopCoroutine(revealCoroutine);
        }
        if (light != null)
        {
            light.enabled = true;
        }
        SetAlpha(1f);
        revealCoroutine = StartCoroutine(DimAfter(duration));
    }

    public IEnumerator DimAfter(float duration)
    {
        yield return new WaitForSeconds(duration);
        SetAlpha(dimAlpha);
        revealCoroutine = null;
        if (light != null)
        {
            light.enabled = false;
        }
    }

    void SetAlpha(float alpha)
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();

        if (sr == null) return;
        Color c = sr.color;
        c.a = alpha;
        sr.color = c;
    }
}
