using System.Collections;
using System.Collections.Generic;
using EventSystemMDD;
using UnityEngine;

public class SceneReloadTut : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        EventSystemMDD.EventSystemMDD.TutorialSceneReloaded += OnReload;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnReload(TutorialSceneReloaded e)
    {
        TutorialScenePersistentData.isUsed = true;
    }
}
