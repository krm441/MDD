using UnityEngine;

public class DungeonController : MonoBehaviour
{
    [Header("Generator & Mesher")]
    public BSPLayoutGenerator generator;
    public BSPMeshing mesher;           

    [ContextMenu("Generate + Build")]
    public void GenerateAndBuild()
    {
        if (generator == null || mesher == null)
        {
            return;
        }

        generator.Generate();            
        mesher.generator = generator;    
        mesher.Rebuild();                
    }
}
