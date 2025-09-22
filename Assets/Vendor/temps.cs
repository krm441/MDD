using UnityEngine;

public class temps : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        // =============================== //
        /*
        UI:
            DONE - intro screen
            DONE - chose your character screen
            DONE - start game
            - journal

            Beautification faze:
            - add portraits, use the ui package

        Credits:

        API:
            - separation of concerns session: sit and think carefully how to decouple refractor the classes

        tutorial - intro - game

        tutorial:
        DONE - add bob and conversations
        DONE - add portal to into 1 and 2
        - add fire surface as a party pooper
        - add ap costs
        - add healer to the enemy party and 2 rdd 1 mdd
        - add casting and picking animation
        - fix bugs:
            a) after combat, if you dont select the main char, its not selected 
            DONE b) stopping distace maintained

        intro:
            - add construction site
            - add portals
            - add animation for the final boss

        game:
        common:
        - spawn party
        - spawn boss
        - decide on themes

        ca:
        - spawn intro party
        - spawn farming resources

        gg:
        - construct shores, and more grammar and islands
        - simple enemies mdd rdd and boss

        bsp:
        - use enemy skeletons
        - zombies(melee + status)

        */

        /*
        
        Tiers:
        COMPLETE: 1) Connectors: Tutorial - Intro - Dungeon - Placement of party (C:1)
            - party manager persistent object (maybe just do one for the sake of the game) || JSON
            - check the SceneManagerMDD for minor bug (y != z in pos)
            - complete other dungeons - easy (but pay attention to inconsistence in naming)
        3) Spawn existing NPCs: (C: 2.3)
            - make resources spawn (C: 0.5)
            - add ap to combat (C: 1)
            - add new NPCs: use the asset pack + animations (C: 0.3)
                - skeletons
                - archers
                - healers
            - spawn in dungeons (C: 0.2)
            - add fire surface as a party pooper (C: 0.3)
        2) Quests: Accept quest - quest completion - journal entry (UI) (C: 2)
        4) Finilise CA and GG (C: 1) but time: 2
            - carve obstacles (or solve it other way - hz)
            - water shader stuff
            DONE - gg upscale
        5) party manager persistent object (maybe just do one for the sake of the game) || JSON
         
        */

        /*
        
        BUGS:
            - on select self - no dialogue (or silent fail)

        UI and party progression:
            - chose your character:
                DONE - make meta data stuff
                DONE - make meta loader
                DONE - make loader party spawner from meta
                DONE - spawn party based on the meta data loaded from json
                DONE - finilize the char spawner + portraits and spells + save path 
                NEXT - make character editor in editor window
                NEXT - make ui chosing character with meta data displayed
                DONE - plan the basic spells and their icons displayed in the character choser
                - plan spawn the remaining characters in the dungeons
                - plan 4 - character party            
            - make another Bob - to guide you

        Checkpoint save:

        Quests:
            - quest journal

        Spawn existing NPCs:
            - make CA good (plus carve)
            - resource spawn locations
            - spawn existing NPCs at resource spawn locations

        Better spells and VFX and SFX:
            - player enjoyment of combat

        Fire/Elemental surface as invisible party member

        Final step: BT
            - debugged combat
            - debugged animations
            - add ap calculation and planning accordingly
            - more NPCs
         
        */

        /*
        
        Debug AI strategy:
            DONE - empty scene with basic npc (obstacles)
            DONE - debug mage standalone:
                - debug the combat entry/exit:
                    - debug the blast
            DONE - debug barbarian
                - enemy not playing die animation
                - axe swinging:
                    - check if we are using targets transform and not the hit point
                    DONE - check if we are using the stoping distance
            - elemental blast surface as char unit and member of turn based put always at last, or queue at first... probably at first


        */

        /*
        
        Fixes:
            - fix attack before combat
            - fix cancel combat issue
            - mini map
        
        */
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
