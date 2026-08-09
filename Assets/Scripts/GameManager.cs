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

    public void StartGame()
    {
        TransitionTo(() =>
        {
            currentStageIndex = 1;
            totalElapsedTime = 0f;
            timerRunning = true;
            LoadStageByIndex(currentStageIndex);
        });
    }

    public void LoadNextStage()
    {
        TransitionTo(() =>
        {
            currentStageIndex++;
            int stageCount = SceneManager.sceneCountInBuildSettings - 2; // exclude Title + End
            if (currentStageIndex > stageCount)
            {
                timerRunning = false;
                SceneManager.LoadScene(endSceneName);
            }
            else
            {
                LoadStageByIndex(currentStageIndex);
            }
        });
    }

    void LoadStageByIndex(int stageIndex)
    {
        SceneManager.LoadScene("Stage" + stageIndex);
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
