using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DarkMultiPlayerServer;
using DarkMultiPlayerServer.Messages;
using AntiCheatSystemServer;
using System.IO;
using SyntaxSystemsCommon;

namespace SyntaxServerMessageHandler
{
    public class SyntaxAntiCheatSystemHandler : DMPPlugin
    {
        public override void OnClientAuthenticated(ClientObject client)
        {
            // cache client info if the vessel hasn't been cached for another client yet.
            if (client.activeVessel != "" || client.playerName != "Unknown")
            {
                if (!AntiCheatSystemCore.AntiCheatSystem.SAHSCheck(client, client.activeVessel))
                {
                    client.disconnectClient = true;
                    ConnectionEnd.SendConnectionEnd(client, "Kicked for hijacking protected vessel.");
                    ClientHandler.DisconnectClient(client);
                    DarkLog.Debug("Player kicked for trying to hijack a vessel: " + client.activeVessel + " client kicked: " + client.playerName);
                    return;
                }
                AntiCheatSystemCore.AntiCheatSystem.CacheClient(client);

            }

        }
        public override void OnMessageReceived(ClientObject client, DarkMultiPlayerCommon.ClientMessage messageData)
        {
            if (messageData.type == DarkMultiPlayerCommon.ClientMessageType.MOD_DATA)
            {
                ModData.HandleModDataMessage(client, messageData.data);
                messageData.handled = true;
                return;
            }
        }
        public override void OnServerStart()
        {
            AntiCheatDirectoryControl.Initialize();
        }
        public override void OnServerStop()
        {
            AntiCheatDirectoryControl.SaveToFile();
            AntiCheatSystemCore.AntiCheatSystem.Refresh();
        }
        public override void OnClientDisconnect(ClientObject client)
        {
            AntiCheatDirectoryControl.SaveClientToFile(client.playerName);
            AntiCheatDirectoryControl.RemoveClientFromCache(client.playerName);
        }
    }

    public static class AntiCheatDirectoryControl
    {
        internal static void Initialize()
        {
            if(DirectoriesAndFilesCheck())
            {
                ReadFromFile();
            }
            DMPModInterface.RegisterModHandler("ANTICHEAT", SAHSHandler);
        }

        private static void SAHSHandler(ClientObject client, byte[] modData)
        {
            if (modData != null)
            {
                SyntaxAntiCheatMessageType msgType = SyntaxAntiCheatMessageType.Check;
                string username, vesselid;
                using (MessageStream2.MessageReader modReader = new MessageStream2.MessageReader(modData))
                {
                    msgType = (SyntaxAntiCheatMessageType)modReader.Read<int>();
                    username = modReader.Read<string>();
                    vesselid = modReader.Read<string>();
                    //DarkLog.Debug("AntiCheat: MessageType: " + msgType.ToString() + " user: " + username + " vesselguid: " + vesselid);
                }
                switch (msgType)
                {
                    case SyntaxAntiCheatMessageType.Check:
                        if (client.activeVessel == null)
                        {
                            return;
                        }
                        bool result = AntiCheatSystemCore.AntiCheatSystem.SAHSCheck(client, client.activeVessel);
                        //DarkLog.Debug("AntiCheat: Action chosen. Performed Anti Cheat Check. Result is: " + result.ToString());
                        break;
                    default:
                        DarkLog.Debug("Syntax Anti Cheat messagetype unknown.");
                        break;
                }
            }

            else
            {
                DarkLog.Debug("Ignored information because modData is null. ");
            }
        }
        internal static void ReInitialize()
        {
            SaveToFile();
            Initialize();
        }

        internal static void SaveClientToFile(string clientname)
        {
            string clientLine = AntiCheatSystemCore.AntiCheatSystem.FormatClient(clientname);
            string filepath = Path.Combine(DarkMultiPlayerServer.Server.universeDirectory, Path.Combine("SyntaxPlugins", "AntiCheat"));
            StreamWriter sw = new StreamWriter(Path.Combine(filepath, "cache.txt"));

            sw.WriteLine(clientLine);
            sw.Close();
        }

        internal static void RemoveClientFromCache(string clientName)
        {
            AntiCheatSystemCore.AntiCheatSystem.RemoveClient(clientName);
        }

        internal static void SaveToFile()
        {
            string filepath = Path.Combine(DarkMultiPlayerServer.Server.universeDirectory, Path.Combine("SyntaxPlugins", "AntiCheat"));
            string[] linestoWrite = AntiCheatSystemCore.AntiCheatSystem.FormatClients;

            StreamWriter sw = new StreamWriter(Path.Combine(filepath, "cache.txt"));

            foreach (string line in linestoWrite)
            {
                sw.WriteLine(line);
            }
            sw.Close();
        }

        internal static void ReadFromFile()
        {
            string filepath = Path.Combine(DarkMultiPlayerServer.Server.universeDirectory, Path.Combine("SyntaxPlugins", "AntiCheat"));
            StreamReader sr = new StreamReader(Path.Combine(filepath,"cache.txt"));

            while (!sr.EndOfStream)
            {
                string line = sr.ReadLine();
                string[] lineArgs = line.Split(',');
                if (lineArgs.Count() > 1)
                {
                    string username = lineArgs[0];
                    List<string> foundVesselIds = new List<string>();
                    string[] vessels = lineArgs[1].Split(';');
                    foundVesselIds.AddRange(vessels);

                    AntiCheatSystemCore.AntiCheatSystem.CacheClientFromFile(username, vessels);
                }
            }
            sr.Close();

        }

        internal static bool DirectoriesAndFilesCheck()
        {
            bool flag = false;
            string universedirectory = DarkMultiPlayerServer.Server.universeDirectory;
            string syntaxdirectory = Path.Combine("SyntaxPlugins" , "AntiCheat");
            if (!Directory.Exists(Path.Combine(universedirectory, "SyntaxPlugins")))
            {
                Directory.CreateDirectory(Path.Combine(universedirectory, "SyntaxPlugins"));
            }

            if (Directory.Exists(Path.Combine(universedirectory, "SyntaxPlugins")))
            {
                if (!Directory.Exists(Path.Combine(universedirectory, syntaxdirectory)))
                {
                    Directory.CreateDirectory(Path.Combine(universedirectory, syntaxdirectory));
                    Directory.SetAccessControl(Path.Combine(universedirectory, syntaxdirectory), new System.Security.AccessControl.DirectorySecurity(Path.Combine(universedirectory, syntaxdirectory), System.Security.AccessControl.AccessControlSections.All));
                    if (!File.Exists(Path.Combine(universedirectory, Path.Combine(syntaxdirectory, "cache.txt"))))
                    {
                        File.CreateText(Path.Combine(universedirectory, Path.Combine(syntaxdirectory, "cache.txt")));
                        File.SetAccessControl(Path.Combine(universedirectory, Path.Combine(syntaxdirectory, "cache.txt")), new System.Security.AccessControl.FileSecurity(Path.Combine(universedirectory, Path.Combine(syntaxdirectory, "cache.txt")), System.Security.AccessControl.AccessControlSections.All));
                    }
                }
                else
                {
                    flag = true;
                }
            }
            return flag;
        }


    }
}

//if (messageData.data != null)
//{
//    SyntaxAntiCheatMessageType msgType = SyntaxAntiCheatMessageType.Check;
//    string username, vesselid;
//    //DarkLog.Debug("AntiCheat: Reading Messagebytes from client..");
//    using (MessageStream2.MessageReader mr = new MessageStream2.MessageReader(messageData.data))
//    {
//        //SyntaxSystemsCommonMessageType syntaxbridgeType = (SyntaxSystemsCommonMessageType)mr.Read<int>();
//        string modname = mr.Read<string>();
//        DarkLog.Debug("Receiving information for modname: " + modname);
//        if (modname == "ANTICHEAT")
//        {
//            //DarkLog.Debug("AntiCheat: Done Receiving Messagebytes from client..: modname " + modname);
//            bool relay = mr.Read<bool>();
//            //DarkLog.Debug("AntiCheat: Done Receiving Messagebytes from client..: relay ");
//            bool priorityHigh = mr.Read<bool>();
//            //DarkLog.Debug("AntiCheat: Done Receiving Messagebytes from client..: priority ");

//            using (MessageStream2.MessageReader modReader = new MessageStream2.MessageReader(mr.Read<byte[]>()))
//            {
//                msgType = (SyntaxAntiCheatMessageType)modReader.Read<int>();
//                username = modReader.Read<string>();
//                vesselid = modReader.Read<string>();
//                DarkLog.Debug("AntiCheat: MessageType: " + msgType.ToString() + " user: " + username + " vesselguid: " + vesselid);
//            }
//            //DarkLog.Debug("AntiCheat: Done Receiving Messagebytes from client..: modbytes ");

//            //DarkLog.Debug("AntiCheat: Interpreting Messagebytes from client..");
//            //DarkLog.Debug("AntiCheat: Choosing action..");
//            switch (msgType)
//            {
//                case SyntaxAntiCheatMessageType.Check:
//                    if (client.activeVessel == null)
//                    {
//                        return;
//                    }
//                    bool result = AntiCheatSystemCore.AntiCheatSystem.SAHSCheck(client, client.activeVessel);
//                    DarkLog.Debug("AntiCheat: Action chosen. Performed Anti Cheat Check. Result is: " + result.ToString());
//                    break;
//                default:
//                    DarkLog.Debug("Syntax Anti Cheat messagetype unknown.");
//                    break;
//            }
//            //messageData.handled = true;
//        }
//        else
//        {
//            DarkLog.Debug("Ignored information because not directed at anti cheat plugin for modname: " + modname);
//        }
//    }
//}
