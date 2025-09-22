using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PartyManagement;
using UnityEngine.Assertions;

public class CUContext
{
    public IParty parent;
}

public class CharacterUnitReg : MonoBehaviour
{
    private Dictionary<CharacterUnit, CUContext> contexts = new Dictionary<CharacterUnit, CUContext>();

    private void OnDestroy()
    {
        contexts.Clear();
        contexts = null;
    }

    public void RegisterCharacterUnit(CharacterUnit unit, IParty party)
    {
        if(contexts == null) contexts = new Dictionary<CharacterUnit, CUContext>();

        contexts[unit] = new CUContext { parent = party };
    }

    public CUContext GetContext(CharacterUnit unit)
    {
        Assert.IsNotNull(unit, "unit is null");
        Assert.IsNotNull(contexts, "unit is null");

        if (contexts.TryGetValue(unit, out var ctx))
            return ctx;

        Console.Error(ctx);
        Console.Error(unit);
        Console.Error(contexts);

        Assert.IsTrue(false, $"Context not found for {unit?.unitName}, context size {contexts.Count}");
        throw new KeyNotFoundException($"No context for unit {unit.unitName}.");
    }

    public void Clear()
    {
        contexts.Clear();
    }
}
