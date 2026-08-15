using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StageSelectController : MonoBehaviour
{
    [System.Serializable]
    public class StageButtonRefs
    {
        public int stageIndex;
        public Button button;
        public Image background;
        public GameObject lockIcon;
        public TextMeshProUGUI collectibleCountText; // wired by 7.4
    }

    [SerializeField] StageButtonRefs[] stageButtons;
    [SerializeField] Color unlockedColor = new Color(0.25f, 0.55f, 0.9f);
    [SerializeField] Color lockedColor = new Color(0.3f, 0.3f, 0.3f);

    void Start()
    {
        foreach (var sb in stageButtons)
        {
            bool unlocked = ProgressManager.Instance == null || ProgressManager.Instance.IsStageUnlocked(sb.stageIndex);

            sb.button.interactable = unlocked;
            if (sb.background != null)
            {
                sb.background.color = unlocked ? unlockedColor : lockedColor;
            }
            if (sb.lockIcon != null)
            {
                sb.lockIcon.SetActive(!unlocked);
            }

            if (sb.collectibleCountText != null && ProgressManager.Instance != null)
            {
                int found = ProgressManager.Instance.GetCollectibleFoundCountForStage(sb.stageIndex);
                int total = ProgressManager.Instance.GetCollectibleTotalForStage(sb.stageIndex);
                sb.collectibleCountText.text = found + "/" + total;
            }

            int stageIndex = sb.stageIndex;
            sb.button.onClick.AddListener(() =>
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.LoadStage(stageIndex);
                }
            });
        }
    }
}
