using UnityEngine;

public class MossZone : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        HybridPlayerController controller = other.GetComponent<HybridPlayerController>();
        if (controller != null)
        {
            controller.IsInMossZone = true;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        HybridPlayerController controller = other.GetComponent<HybridPlayerController>();
        if (controller != null)
        {
            controller.IsInMossZone = false;
        }
    }
}
