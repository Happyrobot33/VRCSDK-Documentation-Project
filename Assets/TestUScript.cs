
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class TestUScript : UdonSharpBehaviour
{
    void Start()
    {

    }

    //contains all the player interactions
    private void PlayerInteractions()
    {
        //get the first player
        VRCPlayerApi[] players = new VRCPlayerApi[VRCPlayerApi.GetPlayerCount()];  
        VRCPlayerApi.GetPlayers(players);
        VRCPlayerApi player = players[0];
        //these are listed as close as I can to the order they show up in the docs
        d(player.IsValid());
        player.EnablePickups(true);
        d(player.displayName);
        d(player.isLocal);
        d(player.isMaster);
        d(player.GetPickupInHand(VRC_Pickup.PickupHand.Left));
        d(player.IsOwner(gameObject));
        d(player.IsUserInVR());
        player.PlayHapticEventInHand(VRC_Pickup.PickupHand.Left, 1, 1, 1);
        player.UseAttachedStation();
        player.UseLegacyLocomotion();

        //NON IMPLEMENTED METHODS
        #region NON_IMPLEMENTED
        player.CombatGetCurrentHitpoints();
        player.CombatGetDestructible();
        player.CombatSetCurrentHitpoints(1);
        player.CombatSetDamageGraphic(1);
        player.CombatSetMaxHitpoints(1);
        player.CombatSetRespawn(true, 1, transform);
        player.CombatSetup();
        #endregion

        d(VRCPlayerApi.GetCurrentLanguage());
        d(VRCPlayerApi.GetAvailableLanguages());
        d(VRCPlayerApi.GetPlayerById(1));

        //yes, both of these literally do THE EXACT SAME THING
        //kill me
        d(player.playerId);
        d(VRCPlayerApi.GetPlayerId(player));
        player.SetPlayerTag("tag");
        player.SetPlayerTag("tag", "tag value");
    }






    /// <summary>
    /// Debug.Log wrapper, makes it so IDEs dont complain about having hanging variable calls
    /// </summary>
    /// <param name="a"></param>
    private void d(object a)
    {
        Debug.Log(a.ToString());
    }
}
