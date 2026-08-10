using UnityEngine;
using UnityEngine.EventSystems;

// Fixed-position joystick (background ring stays put; handle drags within it and
// snaps back on release) rather than a floating "appears where you touch" joystick -
// simpler to build/verify and predictable to reach without looking at the screen.
public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [SerializeField] RectTransform background;
    [SerializeField] RectTransform handle;
    [SerializeField] float handleRange = 100f;

    public Vector2 InputVector { get; private set; }

    public void OnPointerDown(PointerEventData eventData)
    {
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (background == null || handle == null) return;

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            background, eventData.position, eventData.pressEventCamera, out localPoint);

        Vector2 clamped = Vector2.ClampMagnitude(localPoint, handleRange);
        handle.anchoredPosition = clamped;
        InputVector = clamped / handleRange;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (handle != null)
        {
            handle.anchoredPosition = Vector2.zero;
        }
        InputVector = Vector2.zero;
    }
}
