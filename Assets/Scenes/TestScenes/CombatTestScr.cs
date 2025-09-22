using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CombatTestScr : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        var data = CharacterMetaDataLoader.Load(name);
        data.isMainCharacter = true;
        PlayerPartyData party = new PlayerPartyData();
        party.party = new System.Collections.Generic.List<CharacterMetaData>();
        party.party.Add(data);
        GameSession.playerParty = party;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
