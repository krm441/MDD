using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using TMPro;

public class TutorialController : MonoBehaviour
{
    [SerializeField] private GameObject tutorialPanel;

    [Header("Books")]
    [SerializeField] private TutorialBook tutorialBookIntro;
    [SerializeField] private TutorialBook tutorialBookCombat;
    [SerializeField] private TutorialBook tutorialBookTutorialFinished;

    [Header("Meta")]
    [SerializeField] private Text title;
    [SerializeField] private TextMeshProUGUI body;
    [SerializeField] private Image image;

    [Header("Buttons")]
    [SerializeField] private Button nextButton;
    [SerializeField] private Button finishButton;

    private int index = 0;
    private TutorialBook currentTutBook;

    public void Bind(TutorialBook.Page page)
    {
        Assert.IsNotNull(page);
        Assert.IsNotNull(title);
        Assert.IsNotNull(body);
        Assert.IsNotNull(image);

        title.text = page.title ?? "";
        body.text = page.body ?? "";

        bool hasImage = page.image != null;
        image.gameObject.SetActive(hasImage);

        if (hasImage)
        {
            image.sprite = page.image;
            image.preserveAspect = true;
        }
    }

    private void Start()
    {
        Assert.IsNotNull(tutorialPanel);
        tutorialPanel.SetActive(false);
        finishButton.gameObject.SetActive(false);
    }

    public void ShowTutorialIntro() => ShowTutorial(tutorialBookIntro);
    public void ShowTutorialCombat() => ShowTutorial(tutorialBookCombat);
    public void ShowTutorialFinished() {}// ShowTutorial(tutorialBookTutorialFinished);

    private void ShowTutorial(TutorialBook tutorialBook)
    {
        Assert.IsNotNull(tutorialBook);

        currentTutBook = tutorialBook;

        tutorialPanel.SetActive(true);
        finishButton.gameObject.SetActive(false);
        nextButton.gameObject.SetActive(true);
        Bind(tutorialBook.pages[0]);
        index = 0;

        //if book is small
        if (tutorialBook.pages.Count == 1)
        {
            finishButton.gameObject.SetActive(true);
            nextButton.gameObject.SetActive(false);
        }
    }

    public void HideTutorial()
    {
        tutorialPanel.SetActive(false);
        currentTutBook = null;
    }

    public void NextPage()
    {
        Assert.IsNotNull(currentTutBook);

        if (index < currentTutBook.pages.Count - 1)
        {
            Bind(currentTutBook.pages[index + 1]);
            ++index;

            if (index + 1 >= currentTutBook.pages.Count)
            {
                finishButton.gameObject.SetActive(true);
                nextButton.gameObject.SetActive(false);
            }
            else
            {
                finishButton.gameObject.SetActive(false);
                nextButton.gameObject.SetActive(true);
            }
        }
    }

    public void PreviousPage()
    {
        

        Assert.IsNotNull(currentTutBook);
        if (index > 0)
        {
            Bind(currentTutBook.pages[--index]);

            if (index + 1 >= currentTutBook.pages.Count)
            {
                finishButton.gameObject.SetActive(true);
                nextButton.gameObject.SetActive(false);
            }
            else
            {
                finishButton.gameObject.SetActive(false);
                nextButton.gameObject.SetActive(true);
            }
        }
    }

}
