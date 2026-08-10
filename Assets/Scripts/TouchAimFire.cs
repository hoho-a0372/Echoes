using UnityEngine;
using UnityEngine.EventSystems;

// Invisible full-zone drag surface (right half of screen) - drag direction from
// the press point becomes the aim direction, release fires the ping. Decoy stays
// on a dedicated button (see DecoyThrow.TryThrow) rather than sharing this zone,
// so a drag-to-fire gesture can never be misread as a decoy throw or vice versa.
public class TouchAimFire : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [SerializeField] float deadzonePixels = 10f;

    public Vector2 AimDirection { get; private set; }
    public bool IsDragging { get; private set; }

    bool fireTriggered;
    Vector2 startScreenPos;

    public void OnPointerDown(PointerEventData eventData)
    {
        startScreenPos = eventData.position;
        IsDragging = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 delta = eventData.position - startScreenPos;
        if (delta.sqrMagnitude > deadzonePixels * deadzonePixels)
        {
            AimDirection = delta.normalized;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        IsDragging = false;
        if (AimDirection.sqrMagnitude > 0.01f)
        {
            fireTriggered = true;
        }
    }

    // Polled from HybridPlayerController.Update() - true at most once per drag release.
    public bool ConsumeFireTrigger()
    {
        if (!fireTriggered) return false;
        fireTriggered = false;
        return true;
    }
}
