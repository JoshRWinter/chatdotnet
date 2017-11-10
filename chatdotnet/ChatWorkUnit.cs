using System;
using System.Threading;

namespace chatdotnet
{
    public enum ChatWorkUnitType{
        Connect, // user wants to connect to a server
        ListChats, // user wants to refresh the chat list
        NewChat, // user wants to create a new chat
        Subscribe, // user wants to subscribe to a chat
        Message // user wants to send a message
    }

    // base class
    class ChatWorkUnit
    {
        internal readonly ChatWorkUnitType type;

        internal ChatWorkUnit(ChatWorkUnitType t)
        {
            type = t;
        }
    }

    // implements ChatWorkUnit.Connect
    class ChatWorkUnitConnect : ChatWorkUnit
    {
        internal readonly string target; // url of server
        internal readonly string name; // name of user
        internal readonly ConnectCallback connectCallback;

        internal ChatWorkUnitConnect(string t, string n, ConnectCallback cc) : base(ChatWorkUnitType.Connect)
        {
            target = t;
            name = n;
            connectCallback = cc;
        }
    }

    // implements ChatWorkUnit.ListChats
    class ChatWorkUnitListChats : ChatWorkUnit
    {
        internal readonly ListChatCallback callback;

        internal ChatWorkUnitListChats(ListChatCallback lcc) : base(ChatWorkUnitType.ListChats)
        {
            callback = lcc;
        }
    }

    // implements ChatWorkUnit.NewChat
    class ChatWorkUnitNewChat : ChatWorkUnit
    {
        internal readonly string name; // name of new chat
        internal readonly string desc; // description for new chat
        internal readonly NewChatCallback newChatCallback;

        internal ChatWorkUnitNewChat(string n,string d,NewChatCallback ncc) : base(ChatWorkUnitType.NewChat)
        {
            name = n;
            desc = d;
            newChatCallback = ncc;
        }
    }

    // implements ChatWorkUnit.Subscribe
    class ChatWorkUnitSubscribe : ChatWorkUnit
    {
        internal readonly Chat chat;
        internal readonly SubscribeCallback subscribeCallback;
        internal readonly MessageCallback msgCallback;

        internal ChatWorkUnitSubscribe(Chat c, SubscribeCallback sc, MessageCallback mc) : base(ChatWorkUnitType.Subscribe)
        {
            chat = c;
            subscribeCallback = sc;
            msgCallback = mc;
        }
    }

    // implements ChatWorkUnit.Message
    class ChatWorkUnitMessage : ChatWorkUnit
    {
        internal readonly MessageType messageType;
        internal readonly string text;
        internal readonly byte[] raw;
        internal readonly MessageReceipt callback;

        internal ChatWorkUnitMessage(MessageType mt, string t, byte[] r, MessageReceipt cb) : base(ChatWorkUnitType.Message)
        {
            messageType = mt;
            text = t;
            raw = r;
            callback = cb;
        }
    }
}
