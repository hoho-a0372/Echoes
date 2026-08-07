using UnityEngine;

public class StageExit : MonoBehaviour
{
    [SerializeField] GameObject clearUIPanel;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            HybridPlayerController controller = other.GetComponent<HybridPlayerController>();
            if (controller != null)
            {
                controller.SetControlsEnabled(false);
            }

            if (clearUIPanel != null)
            {
                clearUIPanel.SetActive(true);
            }
        }
    }
}
