using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;

public class SpellIconMDD : MonoBehaviour 
{
    [SerializeField] private TextMeshProUGUI toolTipTextPlaceHolder;
    public string toolTipText;
    [SerializeField] private GameObject toolTip;

    private void Start()
    {
        HideToolTip();
    }

    public void ShowToolTip()
    {
        Assert.IsNotNull(toolTip);
        toolTip.SetActive(true);
        toolTipTextPlaceHolder.text = toolTipText;
    }

    public void HideToolTip()
    {
        toolTip.SetActive(false);
    }
}
