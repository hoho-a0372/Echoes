using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class EndScreenController : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI elapsedTimeText;

    bool returning = false;

    void Start()
    {
        if (elapsedTimeText != null && GameManager.Instance != null)
        {
            elapsedTimeText.text = GameManager.Instance.GetElapsedTime();
        }
    }

    void Update()
    {
        if (returning) return;

        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
        {
            returning = true;
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ReturnToTitle();
            }
        }
    }
}
