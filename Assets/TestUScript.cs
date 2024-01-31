
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class TestUScript : UdonSharpBehaviour
{
    void Start()
    {
        Debug.Log(VRCPlayerApi.GetPlayerCount());
    }
}
