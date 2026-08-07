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
    bool hasHit;

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
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        if (hasHit) return;

        if (col.gameObject.CompareTag("Wall"))
        {
            hasHit = true;
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
            }
            StartCoroutine(FlashAndDestroy());
        }
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
