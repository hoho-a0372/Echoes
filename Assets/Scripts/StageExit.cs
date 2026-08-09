using System.Collections;
using UnityEngine;

public class StageExit : MonoBehaviour
{
    [SerializeField] GameObject clearUIPanel;
    [SerializeField] float nextStageDelay = 2f;

    bool triggered = false;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered) return;

        if (other.CompareTag("Player"))
        {
            triggered = true;
            StartCoroutine(ClearRoutine(other));
        }
    }

    IEnumerator ClearRoutine(Collider2D playerCollider)
    {
        HybridPlayerController controller = playerCollider.GetComponent<HybridPlayerController>();
        if (controller != null)
        {
            controller.SetControlsEnabled(false);
        }

        if (clearUIPanel != null)
        {
            clearUIPanel.SetActive(true);
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayStageClear();
        }

        yield return new WaitForSeconds(nextStageDelay);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.LoadNextStage();
        }
    }
}
