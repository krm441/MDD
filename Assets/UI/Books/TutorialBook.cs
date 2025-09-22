using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(menuName = "Tutorial/Tutorial Book", fileName = "TutorialBook")]
public class TutorialBook : ScriptableObject
{
    [System.Serializable]
    public class Page
    {
        [TextArea(2, 4)] public string title;
        [TextArea(4, 10)] public string body;
        public Sprite image;
    }

    public List<Page> pages = new List<Page>();
}