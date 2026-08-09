using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class HybridPlayerController : MonoBehaviour
{
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float projectileSpeed = 10f;
    [SerializeField] GameObject projectilePrefab;
    [SerializeField] float pingCooldown = 1.5f;
    [SerializeField] Transform facingIndicator;
    [SerializeField] float indicatorOffset = 0.4f;
    [SerializeField] Image deathFlashOverlay;
    [SerializeField] Image cooldownIndicator;

    Rigidbody2D rb;
    bool controlsEnabled = true;
    float nextFireTime;
    Vector2 facingDirection = Vector2.down;
    Vector3 spawnPosition;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        spawnPosition = transform.position;
    }

    void Update()
    {
        if (cooldownIndicator != null)
        {
            float remaining = nextFireTime - Time.time;
            cooldownIndicator.fillAmount = 1f - Mathf.Clamp01(remaining / pingCooldown);
        }

        if (!controlsEnabled) return;

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && Time.time >= nextFireTime)
        {
            Fire();
            nextFireTime = Time.time + pingCooldown;
        }
    }

    void FixedUpdate()
    {
        if (!controlsEnabled)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 input = Vector2.zero;
        Keyboard kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.wKey.isPressed) input.y += 1f;
            if (kb.sKey.isPressed) input.y -= 1f;
            if (kb.aKey.isPressed) input.x -= 1f;
            if (kb.dKey.isPressed) input.x += 1f;
        }

        if (input.sqrMagnitude > 0.01f)
        {
            facingDirection = input.normalized;
            if (facingIndicator != null)
            {
                facingIndicator.localPosition = facingDirection * indicatorOffset;
            }
        }

        rb.linearVelocity = input.normalized * moveSpeed;
    }

    void Fire()
    {
        if (projectilePrefab == null) return;

        GameObject projectile = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        Rigidbody2D projectileRb = projectile.GetComponent<Rigidbody2D>();
        if (projectileRb != null)
        {
            projectileRb.linearVelocity = facingDirection * projectileSpeed;
        }
    }

    public void SetControlsEnabled(bool enabled)
    {
        controlsEnabled = enabled;
        if (!enabled)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    public void Die()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayDeath();
        }

        if (CameraShake.Instance != null)
        {
            CameraShake.Instance.Shake(0.3f, 0.2f);
        }

        StartCoroutine(DieRoutine());
    }

    IEnumerator DieRoutine()
    {
        SetControlsEnabled(false);

        if (deathFlashOverlay != null)
        {
            yield return StartCoroutine(FlashOverlay());
        }

        transform.position = spawnPosition;
        SetControlsEnabled(true);
    }

    IEnumerator FlashOverlay()
    {
        float halfDuration = 0.15f;
        Color c = deathFlashOverlay.color;

        float t = 0f;
        while (t < halfDuration)
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(0f, 1f, t / halfDuration);
            deathFlashOverlay.color = c;
            yield return null;
        }
        c.a = 1f;
        deathFlashOverlay.color = c;

        t = 0f;
        while (t < halfDuration)
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(1f, 0f, t / halfDuration);
            deathFlashOverlay.color = c;
            yield return null;
        }
        c.a = 0f;
        deathFlashOverlay.color = c;
    }
}
