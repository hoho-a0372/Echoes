using System.Collections;
using UnityEngine;
using TMPro;

// Small persistent onboarding hint shown only on Stage1, until the player's
// first ping fire - then fades out and never reappears for the rest of that
// play session. Deliberately session-only (a plain runtime bool, not
// PlayerPrefs): it only needs to survive the in-scene Player.Die()/respawn,
// which doesn't reload the scene, so persisting it across app runs isn't
// needed yet - see the Day 7 checklist entry.
public class PingTooltip : MonoBehaviour
{
    [SerializeField] CanvasGroup canvasGroup;
    [SerializeField] TextMeshProUGUI label;
    [SerializeField] float fadeDuration = 0.4f;
    [TextArea]
    [SerializeField] string message = "핑 버튼을 누르거나 클릭해서 핑을 발사하세요";

    static bool dismissedThisSession;

    void OnEnable()
    {
        HybridPlayerController.OnPingFired += HandlePingFired;
    }

    void OnDisable()
    {
        HybridPlayerController.OnPingFired -= HandlePingFired;
    }

    void Start()
    {
        if (label != null) label.text = message;

        if (dismissedThisSession)
        {
            gameObject.SetActive(false);
            return;
        }

        if (canvasGroup != null) canvasGroup.alpha = 1f;
    }

    void HandlePingFired()
    {
        if (dismissedThisSession) return;
        dismissedThisSession = true;
        StartCoroutine(FadeOutAndHide());
    }

    IEnumerator FadeOutAndHide()
    {
        if (canvasGroup == null)
        {
            gameObject.SetActive(false);
            yield break;
        }

        float t = 0f;
        float start = canvasGroup.alpha;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(start, 0f, t / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
    }
}
