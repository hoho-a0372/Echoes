using UnityEngine;
using UnityEngine.InputSystem;

public class TitleScreenController : MonoBehaviour
{
    bool starting = false;

    void Update()
    {
        if (starting) return;

        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame
            || (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame))
        {
            starting = true;
            if (GameManager.Instance != null)
            {
                GameManager.Instance.GoToStageSelect();
            }
        }
    }
}
