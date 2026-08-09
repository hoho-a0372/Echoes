using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Canvas))]
public class SceneTransition : MonoBehaviour
{
    public static SceneTransition Instance { get; private set; }

    [SerializeField] Image fadeImage;
    [SerializeField] float fadeDuration = 0.5f;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        Canvas canvas = GetComponent<Canvas>();
        canvas.sortingOrder = 999;

        if (fadeImage != null)
        {
            Color c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;
        }
    }

    public void FadeOut(Action onComplete)
    {
        StartCoroutine(FadeRoutine(0f, 1f, onComplete));
    }

    public void FadeIn(Action onComplete = null)
    {
        StartCoroutine(FadeRoutine(1f, 0f, onComplete));
    }

    IEnumerator FadeRoutine(float from, float to, Action onComplete)
    {
        if (fadeImage == null)
        {
            onComplete?.Invoke();
            yield break;
        }

        float t = 0f;
        Color c = fadeImage.color;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(from, to, t / fadeDuration);
            fadeImage.color = c;
            yield return null;
        }
        c.a = to;
        fadeImage.color = c;

        onComplete?.Invoke();
    }
}
