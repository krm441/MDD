using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#region Dialogue Data

[System.Serializable]
public class DialogueChoice
{
    public string text;              // what player sees
    public string nextLineId;        // ID of next line (optional)
    public string action;            // e.g. "StartCombat", "GiveItem", etc.
}

[System.Serializable]
public class DialogueLine
{
    public string id;                // Unique ID for jump targets
    public string speaker;
    public string text;
    public string voiceClip;
    public List<DialogueChoice> choices; // null if no choices
}

[System.Serializable]
public class DialogueData
{
    public string id;
    public List<DialogueLine> lines;
}


#endregion

#region Scripted Scenes
public interface IScriptedSubstates
{
    void Enter();
    void Update();
    void Exit();

    void HandleButtonEvent(ButtonEvent customEvent);
}

public abstract class ScriptedSubstate : IScriptedSubstates
{
    public virtual void Enter() { }
    public virtual void Update() { }
    public virtual void Exit() { }

    public virtual void HandleButtonEvent(ButtonEvent customEvent) { }
}

public class DialogueSequence : ScriptedSubstate
{
}

#endregion
