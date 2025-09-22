using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Assertions;
using PartyManagement;
using TMPro;
using System.Collections.Generic;

public class StartGame : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject startPanel;
    [SerializeField] private GameObject introPannel;
    [SerializeField] private GameObject selectPanel;

    [SerializeField] private Button avaliableButton;
    [SerializeField] private Button unavaliableButton;

    [Header("Main characters")]
    [SerializeField] private GameObject mainCharacters;
    [SerializeField] private GameObject characterStatsPanel;

    [Header("Character Buttons")]
    [SerializeField] private Button[] characterButtons;

    [Header("Next Scene")]
    [SerializeField] private string introSceneName = "Tutorial";

    [Header("Character Display")]
    [SerializeField] private TextMeshProUGUI characterName;
    [SerializeField] private List<GameObject> characterList;

    // internal
    private GameObject currentCharacter = null;

    private void Awake()
    {        
        currentCharacter = characterList[0];
        ShowIntro();
        unavaliableButton.gameObject.SetActive(false);
    }

    public void OnStartGamePressed() => ShowSelect();
    public void OnBackPressed() => ShowStart();
    public void OnCreditsPressed() => RollCredits();
    public void OnShowNextPressed() => ShowNextCharacter();

    public void OnSelectCharacter(string name)
    {
        SceneManager.LoadScene(introSceneName);
        var data = CharacterMetaDataLoader.Load(name);
        data.isMainCharacter = true;
        PlayerPartyData party = new PlayerPartyData();
        party.party = new System.Collections.Generic.List<CharacterMetaData>();
        party.party.Add(data);
        GameSession.playerParty = party;
    }

    public void OnSelectCharacter()
    {
        var data = CharacterMetaDataLoader.Load(currentCharacter.GetComponentInChildren<CharacterUnit>().unitName);
        data.isMainCharacter = true;
        PlayerPartyData party = new PlayerPartyData();
        party.party = new System.Collections.Generic.List<CharacterMetaData>();
        party.party.Add(data);
        GameSession.playerParty = party;
        SceneManager.LoadSceneAsync(introSceneName);
    }

    private void ShowStart()
    {
        startPanel.SetActive(true);
        selectPanel.SetActive(false);
    }

    private void ShowSelect()
    {
        introPannel.SetActive(false);
        HideAllMainCharacters();
        ShowOneCharacter(currentCharacter);
        ShowStats(currentCharacter);

        mainCharacters.SetActive(true);

        startPanel.SetActive(false);
    }

    private void ShowIntro()
    {
        HideAllMainCharacters();

        startPanel.SetActive(false);
        selectPanel.SetActive(false);
        introPannel.SetActive(true);

        // stats
        characterStatsPanel.SetActive(false);
    }

    private void RollCredits()
    {

    }


    private void HideAllMainCharacters()
    {
        mainCharacters.SetActive(true);


        foreach (Transform child in mainCharacters.transform)
        {
            child.gameObject.SetActive(false);
        }
    }

    private void ShowOneCharacter(GameObject characterName)
    {
        //Transform target = mainCharacters.transform.Find(characterName);
        Assert.IsNotNull(characterName);
        characterName.gameObject.SetActive(true);
    }

    private void ShowStats(GameObject target)
    {
        characterStatsPanel.SetActive(true);
        CharacterUnit unit = target.GetComponentInChildren<CharacterUnit>();
        Assert.IsNotNull(unit);

        characterName.text = unit.unitName;

        if (unit.status == CharacterUnitStatus.Unavaliable)
        {
            avaliableButton.gameObject.SetActive(false);
            unavaliableButton.gameObject.SetActive(true);
        }
        else
        {
            avaliableButton.gameObject.SetActive(true);
            unavaliableButton.gameObject.SetActive(false);
        }
    }

    private void ShowNextCharacter()
    {
        currentCharacter.SetActive(false);

        int currentIndex = currentCharacter != null
        ? characterList.IndexOf(currentCharacter)
        : -1;

        int count = characterList.Count;

        GameObject next = null;
        for (int step = 1; step <= count; step++)
        {
            var candidate = characterList[(currentIndex + step) % count];
            if (candidate != null)
            {
                next = candidate;
                break;
            }
        }

        Assert.IsNotNull(next);

        currentCharacter = next;

        ShowOneCharacter(currentCharacter);
        ShowStats(currentCharacter);
    }
}
