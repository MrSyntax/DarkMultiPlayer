using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SyntaxSystemsCommon;

namespace AntiCheatSystem
{
    static public class AntiCheatPlugin
    {
        /// <summary>
        /// Prevents controlling already claimed vessels.
        /// </summary>
        /// <param name="vesseltocheck">Vessel to check for claim</param>
        /// <param name="playername">The player trying to access the vessel</param>
        static public KeyValuePair<bool,byte[]> CreateMessage(Vessel vesseltocheck, string playername)
        {
            return PermissionChecker(vesseltocheck, playername);
        }

        #region Permission Check

        // syntaxcode connection
        private static KeyValuePair<bool, byte[]> PermissionChecker(Vessel vesselToCheck, string username)
        {
            ScreenMessages.print("Sending permission check..");
            string vesselguid = vesselToCheck.id.ToString();
            using (MessageStream2.MessageWriter mw = new MessageStream2.MessageWriter())
            {
                //mw.Write<int>((int)SyntaxSystemsCommonMessageType.SYNTAX_BRIDGE);
                mw.Write<int>((int)SyntaxAntiCheatMessageType.Check);
                mw.Write<string>(username);
                mw.Write<string>(vesselguid);
                return new KeyValuePair<bool, byte[]>(true, mw.GetMessageBytes());
            }
        }

        #endregion
    }
}
