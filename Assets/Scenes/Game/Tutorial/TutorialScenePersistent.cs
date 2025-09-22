using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;
using UnityEngine.Assertions;

/// <summary>
/// Special class to save/load check points in the Tutorial Scene
/// </summary>
public static class TutorialScenePersistentData 
{
    public static void Reset()
    {
        DoorOpened = false;
        EnemiesDefeated = false;
        isUsed = false;
    }

    public static bool DoorOpened = false;
    public static bool EnemiesDefeated = false;

    public static bool isUsed = false;
}

public class TutorialScenePersistent : MonoBehaviour
{
    [SerializeField] private TutorialDoorOpener doorOpener;
    [SerializeField] private PartyPlayer player;
    [SerializeField] private TutorialNPCs enemy;
    [SerializeField] private TutorialController tutController;
    [SerializeField] private IsometricCameraController isometricCameraController;

    private bool tutorialShown = false;

    private void Awake()
    {
        // add on enemies defeated event - unique to tutorial only
        EventSystemMDD.EventSystemMDD.PartyWipe += PartyWiped;
        EventSystemMDD.EventSystemMDD.welcomeScreenTutPopup += WelcomeScreenTutPopup;
        EventSystemMDD.EventSystemMDD.showCombatTutorialEvent += ShowCombatTutorial;

        if (TutorialScenePersistentData.isUsed)
        {
            // 1) door check pooint
            if(TutorialScenePersistentData.DoorOpened)
            {
                Assert.IsNotNull(doorOpener);
                doorOpener.InstantOpen();
            }            

            // 2) Enemy party defeated checkpoint
            if (TutorialScenePersistentData.EnemiesDefeated)
                enemy.isDefeated = true;

            // 3) load last check point
            CheckPointLoader.LoadLastCheckPoint(player);
        }
   

    }

    //private bool introCamLerp = true;
    private void Start()
    {
        //if(introCamLerp)
        isometricCameraController.LerpToCharacter(player.CurrentSelected.transform, 3f, ()=>
        {
            if (!tutorialShown)
                EventSystemMDD.EventSystemMDD.Raise(new EventSystemMDD.ShowWelcomeScreenEvent { });

        });
        //introCamLerp = false; //stop
    }

    private void PartyWiped(EventSystemMDD.PartyWipedEvent e)
    {
        if (e.party.partyType == PartyTypes.Player) return;

        TutorialScenePersistentData.isUsed = true;
        TutorialScenePersistentData.EnemiesDefeated = true;

        tutController.ShowTutorialFinished();

        FindObjectOfType<GameManagerMDD>().ExitCombat();
    }

    private void WelcomeScreenTutPopup(EventSystemMDD.ShowWelcomeScreenEvent e)
    {
        tutController.ShowTutorialIntro();
        tutorialShown = true;
    }

    private void ShowCombatTutorial(EventSystemMDD.ShowCombatTutorialEvent e)
    {
        tutController.ShowTutorialCombat();
    }
}
