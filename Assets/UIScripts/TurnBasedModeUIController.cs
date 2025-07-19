using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TurnBasedModeUIController : MonoBehaviour
{
    private Text buttonText;

    [SerializeField] private GameManagerMDD gameManager;

    private string goToTurnMode = "Enter Turn Based Mode";
    private string goToExploreMode = "Enter Exploration Mode";

    private bool isTurnMode = false; // simple control bool

    // Start is called before the first frame update
    void Start()
    {
        buttonText = transform.GetChild(0).GetComponent<Text>();
        buttonText.text = goToTurnMode;
    }

    // Handle on click
    public void ChangeState()
    {
        isTurnMode = !isTurnMode;
        if (isTurnMode)
        {
            buttonText.text = goToExploreMode;
            gameManager.EnterCombat();
        }
        else
        {
            buttonText.text = goToTurnMode;
            gameManager.ExitCombat();
        }
    }
}
