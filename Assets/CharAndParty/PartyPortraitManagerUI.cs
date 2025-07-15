using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PartyManagement;

public class PartyPortraitManagerUI : MonoBehaviour
{
    //[SerializeField] 
    private static GameObject portraitButtonPrefab;
    //[SerializeField] 
    private static Transform[] portraitParent = new Transform[2]; // this could be a Dictionary
    //[SerializeField] private SpellMap spellMap;

    public static void BuildTurnQueuePortraits(Queue<CharacterUnit> units)
    {
        // Load prefab if not loaded earlier
        if (portraitButtonPrefab == null)
        {
            portraitButtonPrefab = Resources.Load<GameObject>("UI/PortraitButtonPref");
            if (portraitButtonPrefab == null)
            {
                Console.Error("PortraitButton prefab not found in Resources/UI/");
                return;
            }
        }

        // Find portrait parent in scene by name
        //if (portraitParent[1] == null)
        {
            GameObject parentObj = GameObject.Find("HorizontalTurnPortraits");
            if (parentObj == null)
            {
                Console.Error("Could not find 'HorizontalTurnPortraits' in the scene!");
                return;
            }
            portraitParent[1] = parentObj.transform;
        }

        // Clear old portraits
        foreach (Transform child in portraitParent[1])
            Object.Destroy(child.gameObject);

        foreach(CharacterUnit unit in units)
        {
            GameObject btn = Object.Instantiate(portraitButtonPrefab, portraitParent[1]);
            //var img = btn.GetComponentInChildren<Image>();
            //if (img != null)
            //    img.sprite = unit.portraitSprite;

            // Inject CharacterUnit to the UI script
            PortraitBarUI barUI = btn.GetComponent<PortraitBarUI>();
            if (barUI != null)
            {
                // Assign the CharacterUnit
                barUI.SetUnit(unit);
            }

            var portraitImg = btn.transform.Find("Panel/PortraitButton").GetComponent<Image>();
            if (portraitImg != null)
                portraitImg.sprite = unit.portraitSprite;

            btn.GetComponentInChildren<Button>().onClick.AddListener(() =>
            {
                // show stats in a tooltip
            });
        }
    }

    public static void BuildPortraitBar()
    {
        // Load prefab once
        if (portraitButtonPrefab == null)
        {
            portraitButtonPrefab = Resources.Load<GameObject>("UI/PortraitButtonPref");
            if (portraitButtonPrefab == null)
            {
                Console.Error("PortraitButton prefab not found in Resources/UI/");
                return;
            }
        }

        // Find portrait parent in scene by name
        //if (portraitParent[0] == null)
        {
            //GameObject parentObj = GameObject.Find("PartyPortraitParent");
            GameObject parentObj = GameObject.Find("VerticalLayout");
            if (parentObj == null)
            {
                Console.Error("Could not find 'VerticalLayout' in the scene!");
                return; 
            }
            portraitParent[0] = parentObj.transform;
        }

        // Clear old portraits
        foreach (Transform child in portraitParent[0])
            Object.Destroy(child.gameObject);

        // Build buttons
        for (int i = 0; i < PartyManager.partyMembers.Count; i++)
        {
            int index = i;
            CharacterUnit unit = PartyManager.partyMembers[i];

            GameObject btn = Object.Instantiate(portraitButtonPrefab, portraitParent[0]);
           //var img = btn.GetComponentInChildren<Image>();
           //if (img != null)
           //    img.sprite = unit.portraitSprite;

            var portraitImg = btn.transform.Find("Panel/PortraitButton").GetComponent<Image>();
            if (portraitImg != null)
                portraitImg.sprite = unit.portraitSprite;

            // Inject CharacterUnit to the UI script
            PortraitBarUI barUI = btn.GetComponent<PortraitBarUI>();
            if (barUI != null)
            {
                // Assign the CharacterUnit
                barUI.SetUnit(unit);
            }

            btn.GetComponentInChildren<Button>().onClick.AddListener(() =>
            {
                PartyManager.SelectMember(index);
                SpellMap.BuildIconBar(unit);
                Console.Log("Selected:", unit.unitName);
            });
        }
        /*
        foreach (Transform child in portraitParent)
            Destroy(child.gameObject);

        for (int i = 0; i < PartyManager.partyMembers.Count; i++)
        {
            int index = i;
            CharacterUnit unit = PartyManager.partyMembers[i];

            GameObject btn = Instantiate(portraitButtonPrefab, portraitParent);
            btn.GetComponentInChildren<Image>().sprite = unit.portraitSprite;

            Console.Warn("party member added");
            btn.GetComponent<Button>().onClick.AddListener(() =>
            {
                // set member selected
                PartyManager.SelectMember(index); 

                // populate spells
                //if (spellMap != null)
                    SpellMap.BuildIconBar(PartyManager.partyMembers[index]);
                Console.Log($"Selected: {unit.unitName}");
            });
        }*/
    }
}
