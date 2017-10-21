using System;
using System.Threading;

namespace chatdotnet
{
    public enum MessageType
    {
        Text,
        Image,
        File
    }

    public struct Chat
    {
        string name;
        string creator;
        string description;
    }

    public struct Message
    {
        internal MessageType type;
        internal long id;
        internal string text;
        internal string sender;
    }

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
    }
}
