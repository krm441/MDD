using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;

public class MenuController : MonoBehaviour
{
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private TutorialController tutorialScr;
    [SerializeField] private PartyPlayer player;

    [SerializeField] private GameObject mainOptionsPanel;
    [SerializeField] private GameObject tutorialOptionsPanel;

    // Start is called before the first frame update
    void Start()
    {
        Assert.IsNotNull(menuPanel);
        menuPanel.SetActive(false);
    }

    public void ShowHideMenu()
    {
        if(menuPanel.activeInHierarchy)
        {
            menuPanel.SetActive(false);
            return;
        }
        menuPanel.SetActive(true);
        SetStateOne();
        //mainOptionsPanel.SetActive(true);
        //tutorialOptionsPanel.SetActive(false);
    }

    // Button callbacks
    public void Cancel()
    {
        menuPanel.SetActive(false);
    }

    public void ShowTutorial()
    {
        SetStateTwo();
    }

    public void SetStateOne() // default state
    {
        mainOptionsPanel.SetActive(true);
        tutorialOptionsPanel.SetActive(false);
    }

    public void SetStateTwo() // tutorials
    {
        mainOptionsPanel.SetActive(false);
        tutorialOptionsPanel.SetActive(true);
    }

    #region Tutorial States
    public void TutStateShowTutOne()
    {
        Cancel(); // hide menu
        Assert.IsNotNull(tutorialScr);
        tutorialScr.ShowTutorialIntro();
    }

    public void TutStateShowTutTwo()
    {
        Cancel(); // hide menu
        Assert.IsNotNull(tutorialScr);
        tutorialScr.ShowTutorialCombat();
    }

    public void TutStateShowTutThree()
    {
        Cancel(); // hide menu
        Assert.IsNotNull(tutorialScr);
        tutorialScr.ShowTutorialFinished();
    }
    #endregion

    public void LoadLastCheckPoint()
    {
        Cancel(); // hide menu
        Assert.IsNotNull(player);
        tutorialOptionsPanel.SetActive(false);
        mainOptionsPanel.SetActive(false);


        // reload
        var scene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(scene.name);//, LoadSceneMode.Single);
        
    }
}
