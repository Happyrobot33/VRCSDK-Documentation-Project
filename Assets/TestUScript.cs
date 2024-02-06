
using UdonSharp;

using UnityEngine;

using VRC.SDKBase;
using VRC.Udon;

public class TestUScript : UdonSharpBehaviour
{
    void Start()
    {

    }

    private void NetworkingInteractions()
    {
        VRCPlayerApi local = Networking.LocalPlayer;
        d(Networking.IsNetworkSettled);
        d(Networking.IsClogged);
        d(Networking.IsInstanceOwner);
        d(Networking.IsMaster);
        Networking.IsOwner(gameObject);
        Networking.IsOwner(local, gameObject);
        Networking.GetOwner(gameObject);
        Networking.SetOwner(local, gameObject);
        Networking.GetNetworkDateTime();
        Networking.GetServerTimeInSeconds();
        Networking.GetServerTimeInMilliseconds();
        Networking.SimulationTime(gameObject);
        Networking.SimulationTime(local);
        Networking.IsObjectReady(gameObject);
        Networking.GetUniqueName(gameObject);
    }

    //contains all the player interactions
    private void PlayerInteractions()
    {
        //get the first player
        VRCPlayerApi[] players = new VRCPlayerApi[VRCPlayerApi.GetPlayerCount()];
        VRCPlayerApi.GetPlayers(players);
        VRCPlayerApi player = players[0];
        //these are listed as close as I can to the order they show up in the docs
        player.IsValid();
        player.EnablePickups(true);
        d(player.displayName);
        d(player.isLocal);
        d(player.isMaster);
        player.GetPickupInHand(VRC_Pickup.PickupHand.Left);
        player.IsOwner(gameObject);
        player.IsUserInVR();
        player.PlayHapticEventInHand(VRC_Pickup.PickupHand.Left, 1, 1, 1);
        player.UseAttachedStation();
        player.UseLegacyLocomotion();

        //NON IMPLEMENTED METHODS
        #region NON_IMPLEMENTED
        player.CombatGetCurrentHitpoints();
        player.CombatGetDestructible();
        player.CombatSetCurrentHitpoints(1);
        player.CombatSetDamageGraphic(gameObject);
        player.CombatSetMaxHitpoints(1);
        player.CombatSetRespawn(true, 1, transform);
        player.CombatSetup();
        #endregion

        VRCPlayerApi.GetCurrentLanguage();
        VRCPlayerApi.GetAvailableLanguages();
        VRCPlayerApi.GetPlayerById(1);

        //yes, both of these literally do THE EXACT SAME THING
        //kill me
        d(player.playerId);
        VRCPlayerApi.GetPlayerId(player);
        player.SetPlayerTag("tag");
        player.GetPlayerTag("tag");
        player.SetPlayerTag("tag", "tag value");
        player.ClearPlayerTags();
        //WHAT THE FUCK VRC DEVS, WHY ARE THESE INSTANCE METHODS AND NOT STATIC ONES!!!!!!!!!!!
        //WHO WROTE THIS GARBAGE!!!!!
        player.GetPlayersWithTag("tag");
        player.GetPlayersWithTag("tag", "tag value");

        //player audio
        player.SetVoiceGain(1);
        player.SetVoiceDistanceNear(1);
        player.SetVoiceDistanceFar(1);
        player.SetVoiceVolumetricRadius(1);
        player.SetVoiceLowpass(true);

        //avatar audio
        player.SetAvatarAudioGain(1);
        player.SetAvatarAudioFarRadius(1);
        player.SetAvatarAudioNearRadius(1);
        player.SetAvatarAudioVolumetricRadius(1);
        player.SetAvatarAudioForceSpatial(true);
        player.SetAvatarAudioCustomCurve(true); //cant even really figure out what this is supposed to do

        //avatar scaling
        player.GetManualAvatarScalingAllowed();
        player.SetManualAvatarScalingAllowed(true);
        player.GetAvatarEyeHeightMinimumAsMeters();
        player.SetAvatarEyeHeightMinimumByMeters(1);
        player.GetAvatarEyeHeightMaximumAsMeters();
        player.SetAvatarEyeHeightMaximumByMeters(1);
        player.GetAvatarEyeHeightAsMeters();
        player.SetAvatarEyeHeightByMeters(1);
        player.SetAvatarEyeHeightByMultiplier(1);

        //player forces
        player.GetWalkSpeed();
        player.SetWalkSpeed(1);
        player.GetRunSpeed();
        player.SetRunSpeed(1);
        player.GetStrafeSpeed();
        player.SetStrafeSpeed(1);
        player.GetJumpImpulse();
        player.SetJumpImpulse(1);
        player.GetGravityStrength();
        player.SetGravityStrength(1);
        player.Immobilize(true);

        //player positions
        player.GetPosition();
        player.GetRotation();
        player.GetBonePosition(HumanBodyBones.Head);
        player.GetBoneRotation(HumanBodyBones.Head);
        player.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
        player.GetVelocity();
        player.SetVelocity(Vector3.zero);
        player.IsPlayerGrounded();
        player.TeleportTo(transform.position, transform.rotation);
        player.TeleportTo(transform.position, transform.rotation, VRC_SceneDescriptor.SpawnOrientation.AlignRoomWithSpawnPoint);
        player.TeleportTo(transform.position, transform.rotation, VRC_SceneDescriptor.SpawnOrientation.AlignRoomWithSpawnPoint, true);

        d(player.isInstanceOwner);
        d(VRCPlayerApi.TrackingDataType.Head);
        VRCPlayerApi.TrackingData td = player.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
        d(td.position);
        d(td.rotation);
    }

    //player collisions TODO: See if XML docs are displayed properly for overloads
    public override void OnPlayerTriggerEnter(VRCPlayerApi player)
    {
        d(player.displayName + " entered trigger");
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
