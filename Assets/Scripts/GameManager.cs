using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] string titleSceneName = "TitleScreen";
    [SerializeField] string endSceneName = "EndScreen";

    public int currentStageIndex = 0;
    public float totalElapsedTime = 0f;

    bool timerRunning = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        if (timerRunning)
        {
            totalElapsedTime += Time.deltaTime;
        }
    }

    [SerializeField] string stageSelectSceneName = "StageSelect";

    // Title's "press any key" now leads to Stage Select, not straight into
    // Stage1 - see the Day 7 checklist entry. Doesn't touch the timer; that
    // now starts on the first actual stage entry (LoadStage), not on
    // leaving Title, since sitting in a menu shouldn't count as play time.
    public void GoToStageSelect()
    {
        TransitionTo(() => SceneManager.LoadScene(stageSelectSceneName));
    }

    // Stage Select buttons route through here (instead of a bare
    // SceneManager.LoadScene) so they keep the existing fade transition and
    // keep currentStageIndex/the elapsed-time timer meaningful.
    public void LoadStage(int stageIndex)
    {
        TransitionTo(() =>
        {
            if (!timerRunning)
            {
                totalElapsedTime = 0f;
                timerRunning = true;
            }
            currentStageIndex = stageIndex;
            SceneManager.LoadScene("Stage" + stageIndex);
        });
    }

    public void LoadEndScreen()
    {
        TransitionTo(() =>
        {
            timerRunning = false;
            SceneManager.LoadScene(endSceneName);
        });
    }

    void TransitionTo(System.Action loadAction)
    {
        if (SceneTransition.Instance != null)
        {
            SceneTransition.Instance.FadeOut(() =>
            {
                loadAction();
                SceneTransition.Instance.FadeIn();
            });
        }
        else
        {
            loadAction();
        }
    }

    public string GetElapsedTime()
    {
        int minutes = Mathf.FloorToInt(totalElapsedTime / 60f);
        int seconds = Mathf.FloorToInt(totalElapsedTime % 60f);
        return string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    public void ReturnToTitle()
    {
        TransitionTo(() =>
        {
            timerRunning = false;
            SceneManager.LoadScene(titleSceneName);
        });
    }
}
