using System.Collections;
using System.Collections.Generic;
using PartyManagement;
using UnityEngine;

/*
 
1) spell range bool => return path to range
2) spell visualisation
3) spell cast => damage calculator

 */

public class SpellCasting : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public static bool CastSpell(Spell spell, CharacterUnit unit, Vector3 targetPos)
    {
        bool inRange = false;
        var path = GetRangePath(out inRange, unit.transform.position, targetPos, spell.radius);
        if (path != null && inRange)
        {
            RunVisualisationSpell(spell, path);
            CalculateDamage();
            return true;
        }

        return false;
    }

    public static List<Pathfinding.Node> GetRangePath(out bool inRange, Vector3 startLocation, Vector3 targetPos, float radius)
    {

        inRange = false;
        return new List<Pathfinding.Node>();
    }

    public static void RunVisualisationSpell(Spell spell, List<Pathfinding.Node> walkingPath)
    {

    }

    public static void CalculateDamage()
    {

    }
}
