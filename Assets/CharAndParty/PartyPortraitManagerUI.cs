using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PartyManagement;

public class PartyPortraitManagerUI : MonoBehaviour
{
    [SerializeField] private GameObject portraitButtonPrefab;
    [SerializeField] private Transform portraitParent;

    public void BuildPortraitBar()
    {
        foreach (Transform child in portraitParent)
            Destroy(child.gameObject);

        for (int i = 0; i < PartyManager.partyMembers.Count; i++)
        {
            int index = i;
            CharacterUnit unit = PartyManager.partyMembers[i];

            GameObject btn = Instantiate(portraitButtonPrefab, portraitParent);
            btn.GetComponentInChildren<Image>().sprite = unit.portraitSprite;

            btn.GetComponent<Button>().onClick.AddListener(() =>
            {
                PartyManager.selectedIndex = index;
                Debug.Log($"Selected: {unit.unitName}");
            });
        }
    }
}
