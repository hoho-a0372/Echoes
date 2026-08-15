using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class HybridPlayerController : MonoBehaviour
{
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float projectileSpeed = 20f;
    [SerializeField] GameObject projectilePrefab;
    [SerializeField] float pingCooldown = 1.5f;
    [SerializeField] Transform facingIndicator;
    [SerializeField] float indicatorOffset = 0.4f;
    [SerializeField] Image deathFlashOverlay;
    [SerializeField] Image pingCooldownIndicator;
    [SerializeField] VirtualJoystick joystick;
    [SerializeField] TouchAimFire touchAimFire;

    Rigidbody2D rb;
    bool controlsEnabled = true;
    float nextFireTime;
    Vector2 facingDirection = Vector2.up;
    Vector3 spawnPosition;

    public Vector2 FacingDirection => facingDirection;
    public bool ControlsEnabled => controlsEnabled;
    public bool IsInMossZone { get; set; }

    // Fired whenever a ping actually launches (cooldown passed) - used by
    // PingTooltip to know when to dismiss the "how to ping" onboarding hint.
    public static event System.Action OnPingFired;

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
        if (pingCooldownIndicator != null)
        {
            float remaining = nextFireTime - Time.time;
            pingCooldownIndicator.fillAmount = 1f - Mathf.Clamp01(remaining / pingCooldown);
        }

        if (!controlsEnabled) return;

        bool desktopFire = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        bool touchFire = touchAimFire != null && touchAimFire.ConsumeFireTrigger();

        if (touchFire && touchAimFire.AimDirection.sqrMagnitude > 0.01f)
        {
            // Drag-based aiming is independent of movement direction, per the
            // mobile control scheme - override facing only when the trigger
            // actually came from a touch drag release, not a desktop click.
            facingDirection = touchAimFire.AimDirection;
            if (facingIndicator != null)
            {
                facingIndicator.localPosition = facingDirection * indicatorOffset;
            }
        }

        if (desktopFire || touchFire)
        {
            TryFirePing();
        }
    }

    // Shared cooldown-gated entry point - desktop mouse click, mobile drag
    // release, and the mobile ping button (see PingButton.OnClick) all funnel
    // through here, same pattern as DecoyThrow.TryThrow().
    public void TryFirePing()
    {
        if (!controlsEnabled) return;
        if (Time.time < nextFireTime) return;

        Fire();
        nextFireTime = Time.time + pingCooldown;
        OnPingFired?.Invoke();
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

        // Joystick is a fallback, not an override - WASD (desktop/editor testing)
        // always wins if both happen to be active at once.
        if (input.sqrMagnitude < 0.01f && joystick != null)
        {
            input = joystick.InputVector;
        }

        if (input.sqrMagnitude > 0.01f)
        {
            float angle = Mathf.Atan2(input.x, input.y) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, -angle);
            facingDirection = input.normalized;
            // angle > 0 : facing right / angle < 0 facing left
            // Animation 변경하기
        }

        // ClampMagnitude(input, 1f) is equivalent to the old input.normalized for
        // WASD's discrete +-1 axes (diagonal magnitude 1.41 -> clamped to 1, same
        // as normalized; single-axis magnitude 1 -> unchanged either way), while
        // also preserving the joystick's partial-push magnitude for analog speed.
        rb.linearVelocity = Vector2.ClampMagnitude(input, 1f) * moveSpeed;
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

        PingProjectile ping = projectile.GetComponent<PingProjectile>();
        if (ping != null)
        {
            ping.SetDampened(IsInMossZone);
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

        string currentSceneName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentSceneName);
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
