using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;

public enum CursorTypesMDD
{
    None = 0, // for the first time the cursor is assigned, this triggers 'CursorMode.ForceSoftware'. otherwise the cursor may be too large
    Default,
    Melee,
    Arrow,

    Use,
    Talk,

    Forbidden,
    Unreachable,
}

public class CursorManager : MonoBehaviour
{
    [Header("Cursor Sprites")]
    [SerializeField] Texture2D cursorIdle;
    [SerializeField] Texture2D cursorMelee;
    [SerializeField] Texture2D cursorArrow;
    [SerializeField] Texture2D utilityUse;
    [SerializeField] Texture2D conversation;
    [SerializeField] Texture2D forbidden;

    [SerializeField] CursorTypesMDD currentType = CursorTypesMDD.None;

    [Header("Cursor Label")]        
    [SerializeField] TextMeshProUGUI labelText;

    void Awake()
    {
        // Set a starting cursor
        SetCursor(currentType);

        // Label
        labelText.gameObject.SetActive(false);
    }

    public void SetCursor(CursorTypesMDD cursorType)
        => SetCursorInternal(cursorType);
    public void SetLable(string text) => ShowLabel(text);
    public void ClearLable() => HideLabel();

    void ShowLabel(string text)
    {
        if (!labelText) return;
        if (string.IsNullOrEmpty(text)) { HideLabel(); return; }

        labelText.transform.position = Input.mousePosition;
        labelText.raycastTarget = false;
        labelText.enableAutoSizing = false;

        labelText.text = text;
        labelText.gameObject.SetActive(true);
    }

    void HideLabel()
    {
        if (labelText) labelText.gameObject.SetActive(false);
    }

    void SetCursorInternal(CursorTypesMDD cursorType)
    {
        if (currentType == cursorType) return; // early exit

        currentType = cursorType;

        switch (cursorType)
        {
            case CursorTypesMDD.Default:
                UnityEngine.Cursor.SetCursor(cursorIdle,    Vector2.zero, CursorMode.ForceSoftware);
                break;
            case CursorTypesMDD.Melee:
                UnityEngine.Cursor.SetCursor(cursorMelee,   Vector2.zero, CursorMode.ForceSoftware);
                break;
            case CursorTypesMDD.Arrow:
                UnityEngine.Cursor.SetCursor(cursorArrow,   Vector2.zero, CursorMode.ForceSoftware);
                break;
            case CursorTypesMDD.Use:
                UnityEngine.Cursor.SetCursor(utilityUse,    Vector2.zero, CursorMode.ForceSoftware);
                break;
            case CursorTypesMDD.Talk:
                UnityEngine.Cursor.SetCursor(conversation,  Vector2.zero, CursorMode.ForceSoftware);
                break;
            case CursorTypesMDD.Forbidden:
                UnityEngine.Cursor.SetCursor(forbidden,     Vector2.zero, CursorMode.ForceSoftware);
                break;
            default:
                break;
        }
    }

}
