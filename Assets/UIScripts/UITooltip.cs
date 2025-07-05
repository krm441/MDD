using UnityEngine;
using UnityEngine.UI;

public class UITooltip : MonoBehaviour
{
    public static UITooltip Instance;
    public CanvasGroup canvasGroup;
    public Text tooltipText;
    public RectTransform background;

    private void Awake() => Instance = this;

    public void Show(string text, Vector2 screenPos)
    {
        tooltipText.text = text;
        background.sizeDelta = new Vector2(tooltipText.preferredWidth + 20, tooltipText.preferredHeight + 20);
        transform.position = screenPos;
        canvasGroup.alpha = 1;
    }

    public void Hide() => canvasGroup.alpha = 0;
}
