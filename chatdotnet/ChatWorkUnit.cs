using System;
using System.Threading;
using System.Collections.Generic;

namespace chatdotnet
{
    public enum ChatWorkUnitType{
        Connect, // user wants to connect to a server
        NewChat, // user wants to create a new chat
        Subscribe, // user wants to subscribe to a chat
        Message // user wants to send a message
    }

    // callbacks
    internal delegate void SuccessCallback(bool success, List<Chat> chats);
    internal delegate void NewChatCallback(bool success);
    internal delegate void SubscribeCallback(bool success, List<Message> msgs);
    internal delegate void MessageCallback(Message msg);

    // base class
    class ChatWorkUnit
    {
        protected readonly ChatWorkUnitType type;

        internal ChatWorkUnit(ChatWorkUnitType t)
        {
            type = t;
        }

        internal ChatWorkUnitType Type
        {
            get { return type; }
        }
    }

    // implements ChatWorkUnit.Connect
    class ChatWorkUnitConnect : ChatWorkUnit
    {

        internal readonly string target; // url of server
        internal readonly string name; // name of user
        internal readonly SuccessCallback successCallback;

        internal ChatWorkUnitConnect(string t, string n, SuccessCallback sc) : base(ChatWorkUnitType.Connect)
        {
            target = t;
            name = n;
            successCallback = sc;
        }
    }

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

    class ChatWorkUnitMessage : ChatWorkUnit
    {
        internal readonly MessageType messageType;
        internal readonly string text;

        internal ChatWorkUnitMessage(MessageType mt, string t) : base(ChatWorkUnitType.Message)
        {
            messageType = mt;
            text = t;
        }
    }
}
