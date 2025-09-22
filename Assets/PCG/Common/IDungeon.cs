using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IDungeon
{
    Room GetPlayerStart();

    Room GetBossLocation();
    //
    //List<Room> GetRoomsA();

    void Generate();
    void Generate(int seed);
    void Clean();
}
