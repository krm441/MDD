using System.Collections;
using System.Collections.Generic;
using PartyManagement;
using UnityEngine;
using UnityEngine.UI;

public class CharcterMotionScn : MonoBehaviour
{

    [SerializeField] private CharacterUnit character;
    static readonly RaycastHit[] raycastHits = new RaycastHit[32];
    int mask;

    private void Start()
    {
        mask = LayerMask.GetMask("Walkable");
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetMouseButton(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            int count = Physics.RaycastNonAlloc(ray, raycastHits, 100f, mask, QueryTriggerInteraction.Ignore);
            character.WalkTo(raycastHits[0].point);
        }
    }
}
