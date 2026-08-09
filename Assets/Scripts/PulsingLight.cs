using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Light2D))]
public class PulsingLight : MonoBehaviour
{
    [SerializeField] float minIntensity = 0.2f;
    [SerializeField] float maxIntensity = 0.5f;
    [SerializeField] float pulseSpeed = 1f;

    Light2D light2D;

    void Awake()
    {
        light2D = GetComponent<Light2D>();
    }

    void Update()
    {
        float t = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
        light2D.intensity = Mathf.Lerp(minIntensity, maxIntensity, t);
    }
}
