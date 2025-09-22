using System;
using System.Collections;
using System.Collections.Generic;
using PartyManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EventSystemMDD
{
    public enum EventType
    {
        None = 0,
        CharPortratClick,
        EnemyPortratClick,
        SpellClick,
        EnemySpotted
    }

    public class ButtonEvent
    {
        public CharacterUnit targetUnit;
        public EventType eventType;
        public Spell spell;

        public bool isConsumed = false;
        public void Consume() => isConsumed = true;
    }

    public class EnemySpotterEvent
    {
        public CharacterUnit spotter;
    }

    public class PartyWipedEvent
    {
        public IParty party;
    }
    
    public class CheckPointReached
    {
        public PartyPlayer party;
        public CheckPoint checkPoint;
    }

    public class TutorialSceneReloaded {}

    public class ShowWelcomeScreenEvent {}
    public class ShowCombatTutorialEvent {}

    public static class EventSystemMDD
    {
        public static event Action<ButtonEvent> ButtonClick;
        public static event Action<EnemySpotterEvent> EnemySpotted;
        public static event Action<PartyWipedEvent> PartyWipe;

        // Checkpoints
        public static event Action<CheckPointReached> CheckPontReached;
        public static event Action<TutorialSceneReloaded> TutorialSceneReloaded;
        public static event Action<ShowWelcomeScreenEvent> welcomeScreenTutPopup;
        public static event Action<ShowCombatTutorialEvent> showCombatTutorialEvent;

        public static void Raise(ButtonEvent e) => ButtonClick?.Invoke(e);
        public static void Raise(EnemySpotterEvent e) => EnemySpotted?.Invoke(e);
        public static void Raise(PartyWipedEvent e) => PartyWipe?.Invoke(e);
        public static void Raise(CheckPointReached e) => CheckPontReached?.Invoke(e);
        public static void Raise(TutorialSceneReloaded e) => TutorialSceneReloaded?.Invoke(e);
        public static void Raise(ShowWelcomeScreenEvent e) => welcomeScreenTutPopup?.Invoke(e);
        public static void Raise(ShowCombatTutorialEvent e) => showCombatTutorialEvent?.Invoke(e);

        public static void ClearAll()
        {
            ButtonClick = null;
            EnemySpotted = null;
            PartyWipe = null;
            CheckPontReached = null;
            TutorialSceneReloaded = null;
            welcomeScreenTutPopup = null;
            showCombatTutorialEvent = null;
        }

        // Clear when a scene unloads/reloads
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void HookSceneEvents()
        {
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        private static void OnSceneUnloaded(Scene s) => ClearAll();
    }
}
