using UnityEngine;
public class FPSCounter : MonoBehaviour
{
    void OnGUI()
    {
        float fps = 1f / Time.unscaledDeltaTime;
        GUI.Label(new Rect(10, 10, 120, 25), $"{fps:0} FPS");
    }
}
