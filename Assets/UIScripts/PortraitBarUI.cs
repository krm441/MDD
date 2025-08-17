using PartyManagement;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;

public class PortraitBarUI : MonoBehaviour
{
    public Image healthBarFill;
    public Image mannaBarFill;
    public Image armorBarFill;
    public Image moraleBarFill;
    public  CharacterUnit unit;

    public void SetUnit(CharacterUnit character)
    {
        unit = character;
    }

    void Update()
    {
        if (unit == null || healthBarFill == null || armorBarFill == null || mannaBarFill == null || moraleBarFill == null) return;

        float currentHP = unit.attributeSet.stats.HP;
        float maxHP = unit.attributeSet.stats.MaxHP;
        float percent = (maxHP > 0f) ? Mathf.Clamp01(currentHP / maxHP) : 0f;

        // scale method (X-axis)
        Vector3 scale = healthBarFill.rectTransform.localScale;
        scale.x = percent;
        healthBarFill.rectTransform.localScale = scale;

        float currentManna = unit.attributeSet.armorStat.magicArmor;
        float maxMagicArmor = unit.attributeSet.armorStat.maxMagicArmor;
        percent = (maxMagicArmor > 0f) ? Mathf.Clamp01(currentManna / maxMagicArmor) : 0f;

        // scale method (X-axis)
        scale = mannaBarFill.rectTransform.localScale;
        scale.x = percent;
        mannaBarFill.rectTransform.localScale = scale;

        float currentMorale = unit.attributeSet.armorStat.moraleLevel;
        float maxMorale = 100f;
        percent = (maxMorale > 0f) ? Mathf.Clamp01(currentMorale / maxMorale) : 0f;

        // scale method (X-axis)
        scale = moraleBarFill.rectTransform.localScale;
        scale.x = percent;
        moraleBarFill.rectTransform.localScale = scale;

        FillBar(unit.attributeSet.armorStat.maxPhysicalArmor, unit.attributeSet.armorStat.physicalArmor, armorBarFill);
    }

    private void FillBar(float maxUnit, float currentUnit, Image reference)
    {
        float percent = (maxUnit > 0f) ? Mathf.Clamp01(currentUnit / maxUnit) : 0f;

        // scale method (X-axis)
        Vector3 scale = reference.rectTransform.localScale;
        scale.x = percent;
        reference.rectTransform.localScale = scale;
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
