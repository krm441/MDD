using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ConsoleUIManager : MonoBehaviour
{
    public static ConsoleUIManager Instance;

    [SerializeField] private Text consoleText;
    [SerializeField] private int maxLines = 50;
    [SerializeField] private ScrollRect scrollRect;

    private Queue<string> lines = new Queue<string>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void Append(string msg)
    {
        if (lines.Count >= maxLines)
            lines.Dequeue();

        lines.Enqueue(msg);
        consoleText.text = string.Join("\n", lines);

        Canvas.ForceUpdateCanvases(); // Make sure UI layout is updated before scrolling
        scrollRect.verticalNormalizedPosition = 0f; // 0 = bottom, 1 = top
    }
}
