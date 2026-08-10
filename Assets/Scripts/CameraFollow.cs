using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] float smoothTime = 0.15f;
    [SerializeField] Vector2 minBounds;
    [SerializeField] Vector2 maxBounds;

    Vector3 velocity;
    Vector3 basePosition;
    bool baseInitialized;

    void LateUpdate()
    {
        if (target == null) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        if (!baseInitialized)
        {
            basePosition = transform.position;
            baseInitialized = true;
        }

        // If the camera's current view would be wider/taller than the map,
        // the position clamp below has no valid range. Zoom in (never out)
        // just enough to keep the view within the map bounds.
        float mapHalfWidth = (maxBounds.x - minBounds.x) * 0.5f;
        float mapHalfHeight = (maxBounds.y - minBounds.y) * 0.5f;
        float maxSizeForWidth = mapHalfWidth / cam.aspect;
        float maxOrthoSize = Mathf.Min(mapHalfHeight, maxSizeForWidth);
        if (maxOrthoSize > 0f && cam.orthographicSize > maxOrthoSize)
        {
            cam.orthographicSize = maxOrthoSize;
        }

        Vector3 desiredPos = new Vector3(target.position.x, target.position.y, basePosition.z);
        // SmoothDamp from the last unshaken base position, not transform.position -
        // otherwise CameraShake's offset would feed back into the smoothing
        // (transform.position would include the shake jitter, which SmoothDamp
        // would then try to smooth away from, compounding into worse jitter).
        Vector3 smoothed = Vector3.SmoothDamp(basePosition, desiredPos, ref velocity, smoothTime);

        // Clamp to bounds, accounting for camera's half-width/half-height (orthographic)
        float camHalfHeight = cam.orthographicSize;
        float camHalfWidth = camHalfHeight * cam.aspect;

        smoothed.x = Mathf.Clamp(smoothed.x, minBounds.x + camHalfWidth, maxBounds.x - camHalfWidth);
        smoothed.y = Mathf.Clamp(smoothed.y, minBounds.y + camHalfHeight, maxBounds.y - camHalfHeight);

        basePosition = smoothed;

        // Shake is added on top of the already-clamped base position - without
        // re-clamping here, shake jitter could push the camera's actual rendered
        // position past the map edge (e.g. right when a wall-hit ping shake fires
        // near a boundary), revealing empty space beyond the walls.
        Vector3 shakeOffset = CameraShake.Instance != null ? CameraShake.Instance.CurrentOffset : Vector3.zero;
        Vector3 finalPos = smoothed + shakeOffset;
        finalPos.x = Mathf.Clamp(finalPos.x, minBounds.x + camHalfWidth, maxBounds.x - camHalfWidth);
        finalPos.y = Mathf.Clamp(finalPos.y, minBounds.y + camHalfHeight, maxBounds.y - camHalfHeight);
        transform.position = finalPos;
    }
}
