using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using TMPro;

// Onboarding beat: briefly shows the world at a faint ambient light, then
// visibly fades it down to the game's real starting darkness (intensity 0),
// so the player watches the world go dark instead of just starting there.
// Stage1 uses the full version (longer hold, diegetic text); Stages 2-5 use
// a much shorter, text-less version of the same component - see the Day 7
// checklist entry for why this wasn't built as two separate scripts.
public class DarknessIntro : MonoBehaviour
{
    [SerializeField] Light2D globalLight;
    [SerializeField] HybridPlayerController player;
    [SerializeField] TextMeshProUGUI introText;
    [SerializeField] float elevatedIntensity = 0.35f;
    [SerializeField] float defaultIntensity = 0.2f;
    [SerializeField] float holdDuration = 1.75f;
    [SerializeField] float fadeDuration = 1f;
    [SerializeField] float textFadeDuration = 0.4f;
    [TextArea]
    [SerializeField] string message = "칠흑 같은 어둠. 당신은 혼자다.";

    void Start()
    {
        if (globalLight == null) return;
        StartCoroutine(IntroRoutine());
    }

    IEnumerator IntroRoutine()
    {
        if (player != null) player.SetControlsEnabled(false);

        float startIntensity = globalLight.intensity;
        globalLight.intensity = elevatedIntensity;

        if (introText != null && !string.IsNullOrEmpty(message))
        {
            introText.text = message;
            yield return StartCoroutine(FadeText(0f, 1f, textFadeDuration));
        }

        float hold = Mathf.Max(0f, holdDuration - textFadeDuration);
        if (hold > 0f) yield return new WaitForSeconds(hold);

        float t = 0f;
        float textFadeStart = fadeDuration - textFadeDuration;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            globalLight.intensity = Mathf.Lerp(elevatedIntensity, defaultIntensity, t / fadeDuration);
            if (introText != null && t >= textFadeStart)
            {
                float ft = (t - textFadeStart) / textFadeDuration;
                SetTextAlpha(Mathf.Lerp(1f, 0f, Mathf.Clamp01(ft)));
            }
            yield return null;
        }
        globalLight.intensity = defaultIntensity;
        if (introText != null) SetTextAlpha(0f);

        if (player != null) player.SetControlsEnabled(true);
    }

    IEnumerator FadeText(float from, float to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            SetTextAlpha(Mathf.Lerp(from, to, t / duration));
            yield return null;
        }
        SetTextAlpha(to);
    }

    void SetTextAlpha(float a)
    {
        Color c = introText.color;
        c.a = a;
        introText.color = c;
    }
}
