﻿
using UdonSharp;

using UnityEngine;

using VRC.SDKBase;
using VRC.Udon;
using VRC.SDK3.Data;
using System;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class TestUScript : UdonSharpBehaviour
{
    void Start()
    {
    }

    /*
    void Update()
    {
        //test the language stuff
        string lang = VRCPlayerApi.GetCurrentLanguage();
        string[] languages = VRCPlayerApi.GetAvailableLanguages();

        string langstr = "Current Language: " + lang + "\nAvailable Languages: ";
        foreach (string l in languages)
        {
            langstr += l + ", ";
        }

        d(langstr);
    }
    */

    private void DataTokenInteractions()
    {
        DataToken token = new DataToken(true);
        token = new DataToken((SByte)1);
        token = new DataToken((Byte)1);
        token = new DataToken((short)1);
        token = new DataToken((ushort)1);
        token = new DataToken((int)1);
        token = new DataToken((uint)1);
        token = new DataToken((long)1);
        token = new DataToken((ulong)1);
        token = new DataToken((float)1);
        token = new DataToken((double)1);
        token = new DataToken("1");
        token = new DataToken(new DataToken[1]);

        d(token.TokenType);
        d(token.IsNumber);
        d(token.IsNull);
        d(token.Boolean);
        d(token.Number);
        d(token.SByte);
        d(token.Byte);
        d(token.Short);
        d(token.UShort);
        d(token.Int);
        d(token.UInt);
        d(token.Long);
        d(token.ULong);
        d(token.Float);
        d(token.Double);
        d(token.String);
        d(token.DataDictionary);
        d(token.DataList);
        d(token.Reference);
        d(token.Error);

        token.ToString();
        token.GetHashCode();
        token.CompareTo(token);
    }

    private void VRCJSONInteractions()
    {
        const string testjson = "{\"key\":\"value\"}";
        bool success = false;
        success = VRCJson.TryDeserializeFromJson(testjson, out DataToken result); //string input
        success = VRCJson.TrySerializeToJson(result, JsonExportType.Beautify, out DataToken json); //string output
    }

    private void DataDictionaryInteractions()
    {

    }

    private void DataListInteractions()
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
        Networking.CalculateServerDeltaTime(0, 0);
        Networking.SimulationTime(gameObject);
        Networking.SimulationTime(local);
        Networking.IsObjectReady(gameObject);
        Networking.GetUniqueName(gameObject);
    }

    private void VideoError()
    {
        VRC.SDK3.Components.Video.VideoError error = new VRC.SDK3.Components.Video.VideoError();
        d(VRC.SDK3.Components.Video.VideoError.Unknown);
        d(VRC.SDK3.Components.Video.VideoError.InvalidURL);
        d(VRC.SDK3.Components.Video.VideoError.AccessDenied);
        d(VRC.SDK3.Components.Video.VideoError.PlayerError);
        d(VRC.SDK3.Components.Video.VideoError.RateLimited);
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
        //d(player.isModerator);
        //d(player.isSuper);
        //d(VRCPlayerApi.AllPlayers);
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
        //player.GetBoneTransform(HumanBodyBones.Head);
        player.GetBonePosition(HumanBodyBones.Head);
        player.GetBoneRotation(HumanBodyBones.Head);
        player.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
        player.GetVelocity();
        player.SetVelocity(Vector3.zero);
        player.IsPlayerGrounded();
        player.TeleportTo(transform.position, transform.rotation);
        player.TeleportTo(transform.position, transform.rotation, VRC_SceneDescriptor.SpawnOrientation.AlignRoomWithSpawnPoint);
        player.TeleportTo(transform.position, transform.rotation, VRC_SceneDescriptor.SpawnOrientation.AlignRoomWithSpawnPoint, true);
        player.Respawn();
        player.Respawn(0);

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
