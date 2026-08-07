using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class PingProjectile : MonoBehaviour
{
    [SerializeField] float flashRadius = 3f;
    [SerializeField] float flashDuration = 0.4f;
    [SerializeField] float lifespan = 4f;
    [SerializeField] int maxBounces = 1;

    Light2D light2D;
    float initialOuterRadius;
    float initialIntensity;
    Rigidbody2D rb;
    int bouncesRemaining;
    bool isFinished;

    public int BouncesRemaining => bouncesRemaining;
    public bool IsFinished => isFinished;

    void Start()
    {
        light2D = GetComponent<Light2D>();
        rb = GetComponent<Rigidbody2D>();
        bouncesRemaining = maxBounces;

        if (light2D != null)
        {
            initialOuterRadius = light2D.pointLightOuterRadius;
            initialIntensity = light2D.intensity;
        }

        Destroy(gameObject, lifespan);
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        if (isFinished) return;
        if (!col.gameObject.CompareTag("Wall")) return;

        if (bouncesRemaining > 0)
        {
            bouncesRemaining--;
            Vector2 normal = col.GetContact(0).normal;
            if (rb != null)
            {
                // The physics solver has already resolved the collision (and damped
                // rb.linearVelocity into the wall) by the time this callback fires, so
                // reflect the pre-impact approach velocity instead of the current one.
                // Collision2D.relativeVelocity is (other velocity - this velocity), so
                // negate it to recover this body's own incoming direction of travel.
                rb.linearVelocity = Vector2.Reflect(-col.relativeVelocity, normal);
            }
            StartCoroutine(BounceFlash());
        }
        else
        {
            isFinished = true;
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
            }
            StartCoroutine(FlashAndDestroy());
        }
    }

    IEnumerator BounceFlash()
    {
        if (light2D == null) yield break;

        float bounceRadius = flashRadius * 0.5f;
        float rampDuration = 0.08f;
        float holdDuration = 0.1f;

        float t = 0f;
        float startRadius = light2D.pointLightOuterRadius;
        while (t < rampDuration)
        {
            t += Time.deltaTime;
            light2D.pointLightOuterRadius = Mathf.Lerp(startRadius, bounceRadius, t / rampDuration);
            yield return null;
        }
        light2D.pointLightOuterRadius = bounceRadius;

        yield return new WaitForSeconds(holdDuration);

        t = 0f;
        startRadius = light2D.pointLightOuterRadius;
        while (t < rampDuration)
        {
            t += Time.deltaTime;
            light2D.pointLightOuterRadius = Mathf.Lerp(startRadius, initialOuterRadius, t / rampDuration);
            yield return null;
        }
        light2D.pointLightOuterRadius = initialOuterRadius;
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
