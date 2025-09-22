using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

/// <summary>
/// Simple controller to setup the selected image to the left down corner of the selected character.
/// </summary>
public class FooterController : MonoBehaviour
{
    [SerializeField] private Image selectedCharImage;

    public void Setup(PartyManagement.CharacterUnit unit)
    {
        Assert.IsNotNull(unit);
        Assert.IsNotNull(selectedCharImage);
        selectedCharImage.sprite = unit.portraitSprite;
    }
}
