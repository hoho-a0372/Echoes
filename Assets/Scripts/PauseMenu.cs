using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Day 8.1/8.2. Shared pause menu hosting both Restart (in-stage reset) and
// Back-to-Select, per the master prompt's default ("a pause menu is likely
// cleaner since it can also host the Day 8.2 back button"). Toggled by a
// persistent on-screen pause button (works for both mouse clicks and touch,
// covering the mobile scheme from Day 6.5) and, for desktop, the Escape key -
// this project doesn't pick one input scheme over the other anywhere else
// (HybridPlayerController.FixedUpdate/Update supports WASD+mouse and
// joystick+touch side by side), so PauseMenu follows the same convention.
public class PauseMenu : MonoBehaviour
{
    [SerializeField] GameObject pausePanel;

    HybridPlayerController player;
    bool isPaused;
    bool controlsWereEnabledBeforePause;

    void Start()
    {
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.GetComponent<HybridPlayerController>();
        }

        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        if (isPaused) Resume();
        else Pause();
    }

    public void Pause()
    {
        if (isPaused) return;
        isPaused = true;

        Time.timeScale = 0f;
        if (pausePanel != null) pausePanel.SetActive(true);

        // Freezes ping/decoy input too (both gate on ControlsEnabled), not
        // just movement - Time.timeScale=0 alone doesn't stop Update()-driven
        // input reads. Remembers the prior value rather than force-enabling
        // on Resume, so this composes correctly with Die()/StageExit, which
        // also drive ControlsEnabled and could otherwise be clobbered by an
        // unconditional re-enable.
        if (player != null)
        {
            controlsWereEnabledBeforePause = player.ControlsEnabled;
            player.SetControlsEnabled(false);
        }
    }

    public void Resume()
    {
        if (!isPaused) return;
        isPaused = false;

        Time.timeScale = 1f;
        if (pausePanel != null) pausePanel.SetActive(false);

        if (player != null)
        {
            player.SetControlsEnabled(controlsWereEnabledBeforePause);
        }
    }

    // Simplest robust reset (per the master prompt): reloading the scene
    // recreates every per-scene GameObject from scratch, so enemy positions,
    // cracked-wall states, and player position all reset with no manual
    // state-reset code needed. ProgressManager survives this - it's a
    // DontDestroyOnLoad singleton bootstrapped in TitleScreen, not in any
    // Stage scene, so reloading a Stage scene never touches it (confirmed
    // live via Unity_RunCommand - see the Day 8 checklist entry).
    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void BackToStageSelect()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("StageSelect");
    }
}
