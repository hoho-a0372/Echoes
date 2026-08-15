using UnityEngine;

// Optional hidden pickup - findable only by exploring off the critical path
// (dead ends, behind CrackedWalls, plaza corners). Purely a completion
// tracker; doesn't affect gameplay mechanics.
public class Collectible : MonoBehaviour
{
    [SerializeField] string collectibleId; // convention: "stage{N}_item{M}"

    bool collected;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (collected) return;
        if (!other.CompareTag("Player")) return;

        collected = true;

        if (ProgressManager.Instance != null)
        {
            ProgressManager.Instance.MarkCollectibleFound(collectibleId);
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayCollectiblePickup();
        }

        if (CameraShake.Instance != null)
        {
            CameraShake.Instance.Shake(0.08f, 0.06f);
        }

        gameObject.SetActive(false);
    }
}
