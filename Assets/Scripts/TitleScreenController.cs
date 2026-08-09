using UnityEngine;
using UnityEngine.InputSystem;

public class TitleScreenController : MonoBehaviour
{
    bool starting = false;

    void Update()
    {
        if (starting) return;

        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
        {
            starting = true;
            if (GameManager.Instance != null)
            {
                GameManager.Instance.StartGame();
            }
        }
    }
}
