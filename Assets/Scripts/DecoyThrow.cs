using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class DecoyThrow : MonoBehaviour
{
    [SerializeField] GameObject decoyPrefab;
    [SerializeField] float decoyThrowDistance = 3f;
    [SerializeField] float decoyCooldown = 3f;
    [SerializeField] Image decoyCooldownIndicator;

    HybridPlayerController controller;
    float nextThrowTime;

    void Awake()
    {
        controller = GetComponent<HybridPlayerController>();
    }

    void Update()
    {
        if (decoyCooldownIndicator != null)
        {
            float remaining = nextThrowTime - Time.time;
            decoyCooldownIndicator.fillAmount = 1f - Mathf.Clamp01(remaining / decoyCooldown);
        }

        // Placeholder desktop binding, distinct from ping's mouse-click fire -
        // mobile uses TryThrow() directly from a UI Button's OnClick instead.
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            TryThrow();
        }
    }

    // Shared cooldown-gated entry point - desktop Space key and the mobile decoy
    // button both call this so the gating logic only lives in one place.
    public void TryThrow()
    {
        if (controller != null && !controller.ControlsEnabled) return;
        if (Time.time < nextThrowTime) return;

        Throw();
        nextThrowTime = Time.time + decoyCooldown;
    }

    void Throw()
    {
        if (decoyPrefab == null || controller == null) return;

        Vector2 facing = controller.FacingDirection;
        Vector3 spawnPos = transform.position + (Vector3)(facing * decoyThrowDistance);
        Instantiate(decoyPrefab, spawnPos, Quaternion.identity);
    }
}
