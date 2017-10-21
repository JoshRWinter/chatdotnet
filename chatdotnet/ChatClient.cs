﻿using System;
using System.Threading;
using System.Collections.Generic;

namespace chatdotnet
{
    public enum MessageType
    {
        Text,
        Image,
        File
    }

    public class Chat
    {
        readonly public string name;
        readonly public string creator;
        readonly public string description;

        public Chat(string n, string c, string d) { name = n; creator = c; description = d; }
    }

    public class Message
    {
        readonly public MessageType type;
        readonly public long id;
        readonly public string text;
        readonly public string sender;

        public Message(MessageType t, long i, string e, string s) { type = t; id = i; text = e; sender = s; }
    }

    // callbacks
    public delegate void ConnectCallback(bool success, List<Chat> chats);
    public delegate void NewChatCallback(bool success);
    public delegate void SubscribeCallback(bool success, List<Message> msgs);
    public delegate void MessageCallback(Message msg);

    public class ChatClient
    {
        private ChatService service;

        public ChatClient(string dbname) 
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
        public void Subscribe(string name, SubscribeCallback sc, MessageCallback mc)
        {
            var unit = new ChatWorkUnitSubscribe(name, sc, mc);

            service.AddWork(unit);
        }

        // allow the user to send a message
        public void Message(string text)
        {
            var unit = new ChatWorkUnitMessage(MessageType.Text, text);

            service.AddWork(unit);
        }

    }
}
