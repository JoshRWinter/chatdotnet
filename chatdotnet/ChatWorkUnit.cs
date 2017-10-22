using System;
using System.Threading;

namespace chatdotnet
{
    public enum ChatWorkUnitType{
        Connect, // user wants to connect to a server
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
        internal readonly string name; // name of chat
        internal readonly SubscribeCallback subscribeCallback;
        internal readonly MessageCallback msgCallback;

        internal ChatWorkUnitSubscribe(string n, SubscribeCallback sc, MessageCallback mc) : base(ChatWorkUnitType.Subscribe)
        {
            name = n;
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

        internal ChatWorkUnitMessage(MessageType mt, string t, byte[] r) : base(ChatWorkUnitType.Message)
        {
            messageType = mt;
            text = t;
            raw = r;
        }
    }
}
