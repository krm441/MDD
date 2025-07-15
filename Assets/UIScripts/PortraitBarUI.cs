using PartyManagement;
using UnityEngine;
using UnityEngine.UI;

public class PortraitBarUI : MonoBehaviour
{
    public Image healthBarFill;
    private CharacterUnit unit;

    public void SetUnit(CharacterUnit character)
    {
        unit = character;
    }

    void Update()
    {
        if (unit == null || healthBarFill == null) return;

        float currentHP = unit.stats.HP;
        float maxHP = unit.stats.MaxHP;
        float percent = Mathf.Clamp01(currentHP / maxHP);

        // scale method (X-axis)
        Vector3 scale = healthBarFill.rectTransform.localScale;
        scale.x = percent;
        healthBarFill.rectTransform.localScale = scale;

        // in release should change this to .fill method or use a mask
    }
}
