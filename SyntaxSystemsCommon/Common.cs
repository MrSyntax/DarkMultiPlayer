using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MessageStream2;

namespace SyntaxSystemsCommon
{
    static public class Common
    {


    }



    public class SyntaxSystemsCommonMessage
    {
        public SyntaxSystemsCommonMessageType type;
        public byte[] data;
    }

    public enum SyntaxSystemsCommonMessageType
    {
        SYNTAX_BRIDGE = 1,
        PERMISSIONSYSTEMMESSAGE = 2,
        PERMISSIONSYSTEMGROUPMESSAGE = 3
    }
    
    public enum SyntaxAntiCheatMessageType
    {
        Check
    }

    public enum PermissionSystemMessageType
    {
        Check = 4,
        Claim = 5,
        Unclaim = 6
    }

    public enum PermissionSystemGroupMessageType
    {
        Create = 7,
        Invite = 8,
        Remove = 9
    }

    public enum TradingSystemMessageType
    {
        REGISTER,
        INVENTORY_ADD,
        INVENTORY_REMOVE,
        INVENTORY_UPDATE,
        BUYLIST_ADD,
        BUYLIST_REMOVE,
        BUYLIST_UPDATE,
        SELL,
        BUY
    }
}
