using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#region Dialogue Data

[System.Serializable]
public class DialogueData
{
    public string id;
    public List<DialogueNode> nodes;
}

[System.Serializable]
public class DialogueNode
{
    public string id;                       // unique ID for each line
    public string speaker;                  // NPC or Player
    public string text;                     // text shown in the dialogue
    public string voiceClip;                // audio
    public List<DialogueChoice> choices;    // null if it's a single line
}

[System.Serializable]
public class DialogueChoice
{
    public string text;              // player's choice
    public string nextNodeId;        // links to the next DialogueNode by ID
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
