using System;
using UnityEngine;

public class DecoyObject : MonoBehaviour
{
    [SerializeField] float lifespan = 2.5f;
    [SerializeField] float noiseRadius = 6f;

    public static event Action<Vector2, float> OnDecoySpawned;

    void Start()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayDecoyLand();
        }

        OnDecoySpawned?.Invoke(transform.position, noiseRadius);

        Destroy(gameObject, lifespan);
    }
}
