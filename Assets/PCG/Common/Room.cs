using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum RoomLabel { Unassigned, Start, Boss, A, B, C }

[System.Serializable]
public class Room
{
    public int id;                          
    public RoomLabel label = RoomLabel.Unassigned;

    public Vector3 worldPos;
}
