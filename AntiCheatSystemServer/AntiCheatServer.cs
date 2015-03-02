using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DarkMultiPlayerServer;
using DarkMultiPlayerServer.Messages;

namespace AntiCheatSystemServer
{
    public static class AntiCheatSystemCore
    {
        // Access codes for the codes behind the anti hijack and cheat system.

        /// <summary>
        /// Anti Cheat System to check for vessel hijacking and ownership.
        /// </summary>
        public static class AntiCheatSystem
        {
            static private AntiCheatCore SPSHandler = new AntiCheatCore();

            /// <summary>
            /// Initializes and Caches the current clients into the Anti Cheat System. 
            /// If already initialized, it will refresh the internal list thus deleting all current entries.
            /// </summary>
            /// <returns>True if no errors and thus successful</returns>
            static public bool init()
            {
                bool flag = false;
                try
                {
                    SPSHandler.RefreshList();
                    SPSHandler.CacheClients(ClientHandler.GetClients());
                    flag = true;
                }
                catch(Exception ex)
                {
                    flag = false;
                    DarkLog.Debug("AntiCheat: Failed to initialize. Errorcode: " + ex.ToString());
                }
                return flag;
            }

            /// <summary>
            /// Caches clients and their vessels from file.
            /// </summary>
            /// <param name="clientName">The client to cache</param>
            /// <param name="vessels">The formatted vessels to cache for the client</param>
            /// <returns>True if successful</returns>
            static public bool CacheClientFromFile(string clientName, string[] vessels)
            {
                bool flag = false;
                try
                {
                    foreach (string vessel in vessels)
                    {
                        SPSHandler.CacheNewVessel(clientName, vessel);
                    }
                    flag = true;
                }
                catch(Exception ex)
                {
                    flag = false;
                    DarkLog.Debug("AntiCheat: Failed to cache client from file. Client: " + clientName + " . Errorcode: " + ex.ToString());
                }


                return flag;
            }

            /// <summary>
            /// Caches the specific client and his/her active vessel in the list.
            /// </summary>
            /// <param name="client">The client to cache</param>
            /// <returns>True if client has been cached with the active vessel</returns>
            static public bool CacheClient(ClientObject client)
            {
                bool flag = false;

                if(SPSHandler.CacheClient(client))
                {
                    flag = true;
                }
                return flag;
            }

            /// <summary>
            /// Cache a new vessel for the specified client, if the client isn't cached yet, it will cache it.
            /// </summary>
            /// <param name="client">The client of which to cache the active vessel</param>
            /// <returns>True if vessel hasn't been cached (or by someone else) yet</returns>
            static public bool CacheVessel(ClientObject client)
            {
                bool flag = false;

                if(SPSHandler.CacheNewVessel(client.playerName,client.activeVessel))
                {
                    flag = true;
                }

                return flag;
            }

            /// <summary>
            /// Gets all cached clients in string format, within an array.
            /// Format: clientname,vesselid1;vesselid2;
            /// </summary>
            static public string[] FormatClients
            {
                get
                {
                    return SPSHandler.GetClientLines;
                }
            }

            /// <summary>
            /// Returns the specified client in a formatted string.
            /// Format: clientname,vesselid1;vesselid2;
            /// </summary>
            /// <param name="clientname">The client to retrieve from cache and format</param>
            /// <returns>Formatted client string</returns>
            static public string FormatClient(string clientname)
            {
                return SPSHandler.GetClientLine(clientname);
            }
             static public bool RemoveClient(string clientname)
            {
                return SPSHandler.RemoveClient(clientname);
            }
            

            /// <summary>
            /// Syntax Anti Hijack System Check(SAHS): Checks wether the requested vessel belongs to the client requesting it.
            /// If it belongs to the client, it will allow sending it. If not, it will only send the vessel if the AccessType is either 'public'
            /// or 'spectate'. In case of 'spectate' it will keep the vessel locked to prevent taking over the vessel.
            /// </summary>
            /// <param name="client">The client requesting the vessel</param>
            /// <param name="requestedVesselGuid">The requested vessel</param>
            /// <returns>True if allowed to request, false if not</returns>
            public static bool SAHSCheck(ClientObject client, string requestedVesselGuid)
            {
                bool flag = false;
                bool spectatingIsAllowed = false;

                if (SPSHandler.SAHSCheck(client, requestedVesselGuid, out spectatingIsAllowed))
                {
                    flag = true;
                }
                else
                {
                    // Report attempt to overwrite a lock has been diagnosed
                    // Or an attempt to take over a protected vessel
                    client.disconnectClient = true;
                    ConnectionEnd.SendConnectionEnd(client, "Kicked for trying to take over a protected vessel.");
                    //ClientHandler.DisconnectClient(client);
                    DarkLog.Debug("Client kicked from permission handler. Section 2");
                    return false;

                }
                return flag;
            }



            public static void Refresh()
            {
                SPSHandler.RefreshList();
            }
        }


        /// <summary>
        /// Codes behind the Syntax Anti cheat/hijack system
        /// </summary>
        private class AntiCheatCore
        {
            // We have to cache the current clients and their vesselids for future usage of this system.
            protected Dictionary<string, List<string>> cachedClientVesselList = new Dictionary<string, List<string>>();

            public string[] GetClientLines
            {
                get
                {
                    List<string> returnlist = new List<string>();
                    foreach(string client in cachedClientVesselList.Keys)
                    {
                        returnlist.Add(GetClientLine(client));
                    }
                    string[] returnLines = returnlist.ToArray();
                    return returnLines;
                }
            }

            public int GetClientCount
            {
                get { return cachedClientVesselList.Count; }
            }
            public string GetClientLine(string clientname)
            {
                string returnline = "";

                if (cachedClientVesselList.ContainsKey(clientname))
                {
                    returnline = clientname;
                    string vesselLine = "";
                    foreach (string vesselid in cachedClientVesselList[clientname])
                    {
                        vesselLine = vesselid + ";";
                    }
                    returnline = string.Format("{0},{1}",clientname,vesselLine);
                }
                return returnline;
            }

            internal bool RemoveClient(string clientname)
            {
                bool flag = false;
                if(cachedClientVesselList.ContainsKey(clientname))
                {
                    try
                    {
                        cachedClientVesselList.Remove(clientname);
                        flag = true;
                        DarkLog.Debug("AntiCheat: Removing of client from cache succeeded.");
                    }
                    catch(Exception ex)
                    {
                        flag = false;
                        DarkLog.Debug("AntiCheat: Removing of client from cache failed. Errorcode: " + ex.ToString());
                    }
                }
                return flag;
            }

            #region Client and vessel caching
            /// <summary>
            /// Refresh the cached client / vesselid list. Deletes all entries.
            /// </summary>
            /// <returns>true if deletion successful</returns>
            public bool RefreshList()
            {
                bool flag = false;

                try
                {
                    cachedClientVesselList.Clear();
                    flag = true;
                }
                catch(Exception ex)
                {
                    flag = false;
                    DarkLog.Debug("AntiCheat: List refreshing failed. Errorcode: " + ex.ToString());
                }
                return flag;
            }
            /// <summary>
            /// Refreshes the cached client / vessel list. Deletes all entries and adds specified entries.
            /// </summary>
            /// <param name="clients">The clients to cache</param>
            /// <returns>True if deletion and caching successful</returns>
            public bool RefreshList(ClientObject[] clients)
            {
                bool flag = false;

                try
                {
                    cachedClientVesselList.Clear(); // remove all existing clients and their vessels
                    if(CacheClients(clients))
                    {
                        flag = true; // All specified clients and their vessels have been added
                    }
                }
                catch (Exception ex)
                {
                    flag = false;
                    DarkLog.Debug("AntiCheat: List refreshing failed. Errorcode: " + ex.ToString());
                }

                return flag;
            }
            /// <summary>
            /// Caches the specific client and his/her active vessel in the list.
            /// </summary>
            /// <param name="client">The client to chache</param>
            /// <returns>True if caching successful, false if not or client already cached.</returns>
            public bool CacheClient(ClientObject client)
            {
                bool flag = false;
                try
                {
                     if(!cachedClientVesselList.ContainsKey(client.playerName))
                     {
                         List<string> newVesselList = new List<string>();
                         //cachedClientVesselList.Add(client.playerName, newVesselList);
                         if (!CacheNewVessel(client.playerName, client.activeVessel))
                         {
                             flag = false;
                             return flag;
                         }
                     }
                     flag = true; // The client playername and vessel have been added to the list, so flag true
                }
                catch(Exception ex)
                {
                    flag = false;
                    DarkLog.Debug("AntiCheat: Caching of client failed. " + client.playerName + " Errorcode: " + ex.ToString());
                }
                return flag;
            }
            /// <summary>
            /// Caches the specified clients in the list.
            /// </summary>
            /// <param name="clients">The clients to cache</param>
            /// <returns>True if caching successful.</returns>
            public bool CacheClients(ClientObject[] clients)
            {
                bool flag = false;

                try
                {
                    foreach(ClientObject client in clients)
                    {
                        if (!cachedClientVesselList.ContainsKey(client.playerName))
                        {
                            List<string> newVesselList = new List<string>();
                            cachedClientVesselList.Add(client.playerName, newVesselList);
                            if(!CacheNewVessel(client.playerName, client.activeVessel))
                            {
                                flag = false;
                                return flag;
                            }
                        }
                    }
                    flag = true; // all vessels have been added to their respective client entries, so flag true
                }
                catch(Exception ex)
                {
                    flag = false;
                    DarkLog.Debug("AntiCheat: Caching of clients failed. Errorcode: " + ex.ToString());
                }
                return flag;
            }
            /// <summary>
            /// Cache a new vessel for the specified client, if the client isn't cached yet, it will cache it.
            /// </summary>
            /// <param name="client">The client to cache the active vesselid for</param>
            /// <returns>True if vesselid doesn't belong to someone else</returns>
            public bool CacheNewVessel(ClientObject client)
            {
                bool flag = false;

                try
                {
                    if (!cachedClientVesselList.ContainsKey(client.playerName))
                    {
                        List<string> newVesselList = new List<string>();
                        cachedClientVesselList.Add(client.playerName, newVesselList);
                    }
                    foreach (List<string> vesselIdList in cachedClientVesselList.Values)
                    {
                        if (vesselIdList.Contains(client.activeVessel))
                        {
                            flag = false; // vessel belongs to someone else, so report false and return.
                            return flag;
                        }
                    }
                    // Out of the loop, and thus vessel does not belong to anyone yet.
                    cachedClientVesselList[client.playerName].Add(client.activeVessel);
                    flag = true;

                }
                catch (Exception ex)
                {
                    flag = false;
                    DarkLog.Debug("AntiCheat: Caching of vessel crashed for some reason. Errorcode: " + ex.ToString());
                }

                return flag;
            }
            /// <summary>
            /// Cache a new vessel for the specified client, if the client isn't cached yet, it will cache it.
            /// </summary>
            /// <param name="clientName">The client to cache the active id for</param>
            /// <param name="vesselID">The vesselid to cache for the client</param>
            /// <returns>True if vesselid doesn't belong to someone else</returns>
            public bool CacheNewVessel(string clientName, string vesselID)
            {
                bool flag = false;

                try
                {
                    if (!cachedClientVesselList.ContainsKey(clientName))
                    {
                        List<string> newVesselList = new List<string>();
                        cachedClientVesselList.Add(clientName, newVesselList);
                    }
                    foreach (List<string> vesselIdList in cachedClientVesselList.Values)
                    {
                        if (vesselIdList.Contains(vesselID))
                        {
                            flag = false; // vessel belongs to someone else, so report false and return.
                            return flag;
                        }
                    }
                    // Out of the loop, and thus vessel does not belong to anyone yet.
                    cachedClientVesselList[clientName].Add(vesselID);
                    flag = true;

                }
                catch (Exception ex)
                {
                    flag = false;
                    DarkLog.Debug("AntiCheat: Caching of vessel crashed for some reason. Errorcode: " + ex.ToString());
                }

                return flag;
            }
            #endregion

            public bool SAHSCheck(ClientObject client, string requestedVesselid, out bool SpectatingAllowed)
            {
                bool flag = false;
                bool spectateIsAllowed = false;

                if (AntiHijackVessel(client.playerName,requestedVesselid))
                {
                    flag = true; // Allow request because player is recognised as owner of the cached vesselid
                }
                else
                {
                    if(CacheNewVessel(client.playerName,client.activeVessel))
                    {
                        flag = true; // Allow request because vessel is not owned by anyone yet
                    }
                }
                SpectatingAllowed = spectateIsAllowed;
                return flag;
            }

            // Checks wether a client is the actual owner of the vesselid, and if not wether the vesselid has an accesstype of public or spectate

            private bool AntiHijackVessel(string clientName, string vesselID)
            {
                bool flag = false;

                if (vesselID == "" || vesselID == "Unknown" || clientName == "Unknown")
                {
                    return true;
                }
                if(cachedClientVesselList.ContainsKey(clientName))
                {
                    if(cachedClientVesselList[clientName].Contains(vesselID))
                    {
                        flag = true; // flag true because the client has been recognised as the vessel owner.
                    }
                }
                return flag;
            }

        }
    }
}
