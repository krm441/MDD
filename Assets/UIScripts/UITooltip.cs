using UnityEngine;
using UnityEngine.UI;

public class UITooltip : MonoBehaviour
{
    public static UITooltip Instance;
    public CanvasGroup canvasGroup;
    public Text tooltipText;
    public Image image;
    public RectTransform background;

    private void Awake()
    {
        Instance = this; Hide();
        //Show("Some nice tooltip", new Vector2(500, 200)); DEBUG
    }

    public void Show(string text, Vector2 screenPos)
    {
        tooltipText.text = text;
        background.sizeDelta = new Vector2(tooltipText.preferredWidth + 8, tooltipText.preferredHeight + 8);
        transform.position = screenPos;
        gameObject.SetActive(true);
    }

    public void Hide() => gameObject.SetActive(false);
}
