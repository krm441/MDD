using System.Collections;
using System.Collections.Generic;
using EventSystemMDD;
using PartyManagement;
using UnityEngine;
using UnityEngine.Assertions;
using System.Linq;

public class CombatManagement : MonoBehaviour 
{
    private HashSet<IParty> parties = new HashSet<IParty>();
    private List<IParty> partyList = new List<IParty>();
    private List<CharacterUnit> engaged = new List<CharacterUnit>();
    private PartyPlayer playerParty;
    [SerializeField] private CharacterUnitReg registry;
    [SerializeField] private GameManagerMDD gameManager;

    public List<CharacterUnit> GetEngagedUnits() => engaged;
    public CharacterUnitReg GetRegistry() => registry;

    private void Start()
    {
        EventSystemMDD.EventSystemMDD.EnemySpotted += StartCombat;
        EventSystemMDD.EventSystemMDD.PartyWipe += PartyWiped;
    }

    public void RegisterParty(IParty party) // :D yaay!
    {
        Assert.IsNotNull(party, "CombatManagement::RegisterParty: party is nULL");
        if (parties.Add(party))
        {
            partyList.Add(party);
        }

        if (party is PartyPlayer pp)
            playerParty = pp;
    }


    private void StartCombat(EnemySpotterEvent e)
    {
        Assert.IsNotNull(e.spotter);
        
        var context = registry.GetContext(e.spotter);

        var partyOfEngaged = context.parent;
        partyOfEngaged.State = PartyState.Combat;
        partyOfEngaged.StopAllMovement();

        engaged.Clear();
        engaged = engaged.Concat(partyOfEngaged.partyMembers).Concat(playerParty.partyMembers).OrderByDescending(p => p.attributeSet.stats.Initiative).ToList();
        Assert.IsTrue(engaged.Count > 0, "CombatManagement::StartCombat:No engaged units after Concat");

        gameManager.EnterCombat();
    }

    private void EndCombat()
    {
        engaged.Clear();
    }

    private void PartyWiped(EventSystemMDD.PartyWipedEvent e)
    {
        if(e.party.partyType == PartyTypes.Player)
        {
            // game over
            gameManager.GameOverLoss();
            return;
        }

        parties.Remove(e.party);
        partyList.Remove(e.party);

        if(partyList.Count == 1 ) // guarantees that this is the player party
        {
            EndCombat();
            gameManager.ExitCombat();
        }
    }
}
