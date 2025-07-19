using PartyManagement;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PortraitBarUI : MonoBehaviour
{
    public Image healthBarFill;
    public  CharacterUnit unit;

    public void SetUnit(CharacterUnit character)
    {
        unit = character;
    }

    void Update()
    {
        if (unit == null || healthBarFill == null) return;

        float currentHP = unit.attributeSet.stats.HP;
        float maxHP = unit.attributeSet.stats.MaxHP;
        float percent = Mathf.Clamp01(currentHP / maxHP);

        // scale method (X-axis)
        Vector3 scale = healthBarFill.rectTransform.localScale;
        scale.x = percent;
        healthBarFill.rectTransform.localScale = scale;

        // in release should change this to .fill method or use a mask
    }

    public void AnimateAndDestroy(float duration = 0.5f)
    {
        StartCoroutine(RemoveAfterAnimation(duration));
    }

    private IEnumerator RemoveAfterAnimation(float duration)
    {
        RectTransform rt = GetComponent<RectTransform>();
        CanvasGroup cg = GetComponent<CanvasGroup>();

        if (cg == null)
            cg = gameObject.AddComponent<CanvasGroup>();

        Vector3 startPos = rt.anchoredPosition;
        Vector3 endPos = startPos + new Vector3(0, -50f, 0); // move down

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float percent = t / duration;
            rt.anchoredPosition = Vector3.Lerp(startPos, endPos, percent);
            cg.alpha = Mathf.Lerp(1f, 0f, percent);
            yield return null;
        }

        Destroy(gameObject);
    }
}
