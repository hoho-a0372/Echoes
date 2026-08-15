using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

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

        int stageIndex = GetCurrentStageIndex();
        if (ProgressManager.Instance != null)
        {
            ProgressManager.Instance.MarkStageCleared(stageIndex);
        }

        // With Stage Select in place, clearing a stage returns the player to
        // the select screen (their choice what to play next) instead of
        // auto-chaining into "the next stage" - except on the last stage,
        // which still goes to the EndScreen. See the Day 7 checklist entry.
        int stageCount = SceneManager.sceneCountInBuildSettings - 3; // exclude Title, StageSelect, End
        bool isFinalStage = stageIndex >= stageCount;

        if (GameManager.Instance != null)
        {
            if (isFinalStage)
            {
                GameManager.Instance.LoadEndScreen();
            }
            else
            {
                GameManager.Instance.GoToStageSelect();
            }
        }
    }

    int GetCurrentStageIndex()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        return int.Parse(sceneName.Substring("Stage".Length));
    }
}
