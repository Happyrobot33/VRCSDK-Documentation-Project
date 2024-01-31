
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class TestUScript : UdonSharpBehaviour
{
    void Start()
    {
        Debug.Log(VRCPlayerApi.GetPlayerCount());
        //get the first player
        VRCPlayerApi[] players = new VRCPlayerApi[VRCPlayerApi.GetPlayerCount()];  
        VRCPlayerApi.GetPlayers(players);
        VRCPlayerApi player = players[0];
        Debug.Log(player.isLocal);
        Debug.Log(player.displayName);
    }
}
