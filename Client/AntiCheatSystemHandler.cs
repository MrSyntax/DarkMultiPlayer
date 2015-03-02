using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AntiCheatSystem;

namespace DarkMultiPlayer
{
    public static class AntiCheatSystemHandler
    {
        private static string currentVesselGuid = "";
        private static bool ownerVerified = false;
        public static void SendACCheck()
        {
            if (FlightGlobals.ActiveVessel.id.ToString() != "" && FlightGlobals.ActiveVessel.id != null && Settings.fetch.playerName != null && Settings.fetch.playerName != "" && Settings.fetch.playerName != "Unknown")
            {
                KeyValuePair<bool, byte[]> sahscheck = AntiCheatSystem.AntiCheatPlugin.CreateMessage(FlightGlobals.ActiveVessel, Settings.fetch.playerName);
                //NetworkWorker.fetch.SendModMessage(sahscheck.Value, sahscheck.Key);
                DMPModInterface.fetch.SendDMPModMessage("ANTICHEAT", sahscheck.Value, false, sahscheck.Key);
            }
        }
        
        // The response from the server
        private static void PermissionCheckResponse(string ownerYesNo, string spectateYesNo, bool spectateAllowed)
        {
            ScreenMessages.print("Receiving permission check..");
            bool owner = false; // inserted for possible later usage.
            if (ownerYesNo == "")
            {
                // report no response was written thus an internal failure of the code
                DarkLog.Debug("Response was empty. Terminated code to prevent crashing.");
                return;
            }
            if (ownerYesNo == "VesselNotProtected")
            {
                owner = true;
                ReleaseFromSpectator();
            }
            if (ownerYesNo == "Yes")
            {
                owner = true;
                ReleaseFromSpectator();
            }
            if (owner)
            {
                ownerVerified = true;
            }
            if (!spectateAllowed)
            {
                // Send to KSC for prohibited spectating
                if (VesselWorker.fetch.isSpectating == true)
                {
                    HighLogic.LoadScene(GameScenes.SPACECENTER);
                    ScreenMessages.print("Kicked to Space Centre for trying to spectate a private vessel.");
                    return;
                }

                //NetworkWorker.fetch.Disconnect("Spectating on this vessel is not allowed.");
            }
            if (!VesselWorker.fetch.isSpectating)
            {
                // report vesselworker is reporting not spectating, but force it anyway because of permission system.
                if (ownerYesNo == "No")
                {
                    owner = false;
                    if (spectateYesNo == "Yes")
                    {
                        LockToSpectator();
                    }
                    else
                    {
                        // kick the player to ksc for spectating
                        HighLogic.LoadScene(GameScenes.SPACECENTER);
                        ScreenMessages.print("Kicked to Space Centre for trying to spectate a private vessel.");
                    }
                }
            }
        }
        #region SpectateMode
        static private void LockToSpectator()
        {
            InputLockManager.SetControlLock(VesselWorker.BLOCK_ALL_CONTROLS, "PermissionSystem-Spectator");
        }
        static private void ReleaseFromSpectator()
        {
            InputLockManager.RemoveControlLock("PermissionSystem-Spectator");
        }
        #endregion

        public static void ReceiveACCheck(string ownerYesNo, string spectateYesNo, bool spectateAllowed)
        {
            PermissionCheckResponse(ownerYesNo, spectateYesNo, spectateAllowed);
        }

        private static void AntiCheatModMessage(byte[] messagedata)
        {
            string ownerYesNo, spectateYesNo;
            bool spectateAllowed = false;

            using(MessageStream2.MessageReader mr = new MessageStream2.MessageReader(messagedata))
            {
                mr.Read<object>();
                mr.Read<object>();
                ownerYesNo = mr.Read<string>();
                spectateYesNo = mr.Read<string>();
                spectateAllowed = mr.Read<bool>();
            }
            ReceiveACCheck(ownerYesNo, spectateYesNo, spectateAllowed);
        }

        public static void RegisterAntiCheatPlugin()
        {
            DMPModInterface.fetch.RegisterRawModHandler("ANTICHEAT",new DMPMessageCallback(AntiCheatModMessage));
        }
    }
    
}
