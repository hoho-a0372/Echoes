using UnityEngine;
using System.Collections;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }

    // CameraFollow.LateUpdate() sets transform.position every frame; rather
    // than fight that by also writing the transform directly, CameraShake
    // just exposes the current shake offset and CameraFollow adds it in on
    // top of its own smoothed/clamped position.
    public Vector3 CurrentOffset { get; private set; }

    void Awake()
    {
        Instance = this;
    }

    public void Shake(float duration, float magnitude)
    {
        StartCoroutine(ShakeRoutine(duration, magnitude));
    }

    IEnumerator ShakeRoutine(float duration, float magnitude)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            Vector2 offset2D = Random.insideUnitCircle * magnitude;
            CurrentOffset = new Vector3(offset2D.x, offset2D.y, 0f);
            elapsed += Time.deltaTime;
            yield return null;
        }
        CurrentOffset = Vector3.zero;
    }
}
