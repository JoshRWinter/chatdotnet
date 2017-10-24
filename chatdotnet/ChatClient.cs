using System;
using System.Threading;
using System.Collections.Generic;

namespace chatdotnet
{
    public enum MessageType : byte
    {
        Text,
        Image,
        File
    }

    public class Chat
    {
        readonly public ulong id;
        readonly public string name;
        readonly public string creator;
        readonly public string description;

        public Chat(ulong i, string n, string c, string d) { id = i; name = n; creator = c; description = d; }
        public override string ToString()
        {
            return name + " [" + creator + "]";
        }
    }

    public class Message
    {
        readonly public MessageType type;
        readonly public ulong id;
        readonly public string text;
        readonly public string sender;
        readonly public byte[] raw;

        public Message(MessageType t, ulong i, string e, string s, byte[] r) { type = t; id = i; text = e; sender = s; raw = r; }
    }

    // callbacks
    public delegate void ConnectCallback(bool success, List<Chat> chats);
    public delegate void NewChatCallback(bool success);
    public delegate void SubscribeCallback(bool success, List<Message> msgs);
    public delegate void MessageCallback(Message msg);

    public class ChatClient
    {
        private ChatService service;

        public ChatClient() 
        {
            service = new ChatService();
        }

        // shut down the service thread
        public void Shutdown()
        {
            // make sure the service thread is shut down
            service.Join();
        }

        // allow the user to connect to server, and receive list of "chats" back
        public void Connect(string target, string name, ConnectCallback sc)
        {
            var unit = new ChatWorkUnitConnect(target, name, sc);

            service.AddWork(unit);
        }

        // allow the user to make a new chat
        public void NewChat(string name, string description, NewChatCallback newChatCallback)
        {
            var unit = new ChatWorkUnitNewChat(name, description, newChatCallback);

            service.AddWork(unit);
        }

        // allow user to subscribe to a chat
        public void Subscribe(Chat chat, SubscribeCallback sc, MessageCallback mc)
        {
            var unit = new ChatWorkUnitSubscribe(chat, sc, mc);

            service.AddWork(unit);
        }

        // allow the user to send a message
        public void Message(string text)
        {
            var unit = new ChatWorkUnitMessage(MessageType.Text, text, null);

            service.AddWork(unit);
        }

    }
}
