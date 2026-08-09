using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class PingProjectile : MonoBehaviour
{
    [SerializeField] float flashRadius = 3f;
    [SerializeField] float flashDuration = 0.4f;
    [SerializeField] float lifespan = 4f;

    Light2D light2D;
    float initialOuterRadius;
    float initialIntensity;
    Rigidbody2D rb;

    void Start()
    {
        light2D = GetComponent<Light2D>();
        rb = GetComponent<Rigidbody2D>();

        if (light2D != null)
        {
            initialOuterRadius = light2D.pointLightOuterRadius;
            initialIntensity = light2D.intensity;
        }

        Destroy(gameObject, lifespan);

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayPingLaunch();
        }
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        // Collisions are restricted to Wall via the physics layer matrix;
        // this tag check is a defensive fallback.
        if (!col.gameObject.CompareTag("Wall")) return;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        if (AudioManager.Instance != null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            float distance = player != null ? Vector2.Distance(transform.position, player.transform.position) : 0f;
            AudioManager.Instance.PlayWallHit(distance);
        }

        if (CameraShake.Instance != null)
        {
            CameraShake.Instance.Shake(0.05f, 0.05f);
        }

        // Disable the collider immediately so this projectile stops being a
        // physical obstacle for the rest of its (invisible) flash-and-destroy
        // lifetime.
        Collider2D col2D = GetComponent<Collider2D>();
        if (col2D != null)
        {
            col2D.enabled = false;
        }

        // Hide via alpha=0 rather than disabling the SpriteRenderer outright -
        // an object with no active Renderer gets culled from the URP 2D
        // per-object render list entirely, which also suppressed this same
        // object's own Light2D from rendering during the flash.
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            Color c = sr.color;
            c.a = 0f;
            sr.color = c;
        }

        StartCoroutine(FlashAndDestroy());
    }

    IEnumerator FlashAndDestroy()
    {
        if (light2D == null)
        {
            Destroy(gameObject);
            yield break;
        }

        float t = 0f;
        float rampDuration = 0.1f;
        while (t < rampDuration)
        {
            t += Time.deltaTime;
            light2D.pointLightOuterRadius = Mathf.Lerp(initialOuterRadius, flashRadius, t / rampDuration);
            yield return null;
        }
        light2D.pointLightOuterRadius = flashRadius;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, flashRadius);
        foreach (Collider2D hit in hits)
        {
            ShadowEnemy enemy = hit.GetComponent<ShadowEnemy>();
            if (enemy != null)
            {
                enemy.Reveal(0.5f);
            }
        }

        yield return new WaitForSeconds(flashDuration);

        float fadeDuration = 0.3f;
        float startIntensity = light2D.intensity;
        t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            light2D.intensity = Mathf.Lerp(startIntensity, 0f, t / fadeDuration);
            yield return null;
        }
        light2D.intensity = 0f;

        Destroy(gameObject);
    }
}
