using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using PartyManagement;

public class APBarController : MonoBehaviour
{
    public GameObject apCirclePrefab;
    public Transform container;

    private List<Image> circles = new List<Image>();

    private Color availableColor = Color.green;
    private Color unavailableColor = Color.gray;

    void Update()
    {
        var selected = PartyManager.CurrentSelected;
        if (selected == null || selected.IsDead) return;

        UpdateAPDisplay(selected.stats.ActionPoints, selected.stats.MaxActionPoints);
    }

    public void UpdateAPDisplay(int currentAP, int maxAP)
    {
        // Create or reuse circles
        while (circles.Count < maxAP)
        {
            var obj = Instantiate(apCirclePrefab, container);
            var img = obj.GetComponent<Image>();
            circles.Add(img);
        }

        for (int i = 0; i < circles.Count; i++)
        {
            circles[i].gameObject.SetActive(i < maxAP);
            circles[i].color = (i < currentAP) ? availableColor : unavailableColor;
        }
    }
}
