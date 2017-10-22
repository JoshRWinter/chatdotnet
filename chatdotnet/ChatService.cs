using System;
using System.Threading;
using System.Collections.Generic;
using System.Net.Sockets;
using System.IO;
using System.Text;

namespace chatdotnet
{
    class ChatService
    {
        // network commands sent from the client
        private enum ClientCommand : System.Byte
        {
            Introduce, // client wants to tell server user's name
            ListChats, // client wants a chat list from the server
            NewChat, // client wants to make a new chat
            Subscribe, // client wants to subscribe
            Message, // client wants to send a message
            Heartbeat // client is sending a heartbeat to the server
        }

        // network commands sent from the server
        private enum ServerCommand : System.Byte
        {
            ListChats, // server is responding to client request to list chats
            NewChat, // server is sending new chat receipt
            Subscribe, // server is sending subscribe receipt
            Message, // server is sending client a message
            Heartbeat // server is sending client a heartbeat
        }

        private const int CHAT_PORT = 28859;

        private Thread serviceThread;

        private Queue<ChatWorkUnit> units; // work units to be processed
        private Mutex unitsLock; // protects <units>

        volatile bool working;
        private Mutex workingLock; // protects <working> variable

        private NetworkStream tcpstream = null;
        private BinaryWriter tcpout = null;
        private BinaryReader tcpin = null;

        private string clientname; // the clients name

        // callbacks
        private ConnectCallback connectCallback;
        private NewChatCallback newChatCallback;
        private SubscribeCallback subscribeCallback;
        private MessageCallback msgCallback;

        internal ChatService()
        {
            serviceThread = new Thread(this.Entry);
            units = new Queue<ChatWorkUnit>();
            unitsLock = new Mutex();
            working = true;
            workingLock = new Mutex();

            serviceThread.Start();
        }

        internal bool Working
        {
            get
            {
                workingLock.WaitOne();
                bool cached = working;
                workingLock.ReleaseMutex();

                return cached;
            }
            set
            {
                workingLock.WaitOne();
                working = value;
                workingLock.ReleaseMutex();
            }
        }

        // entry point for the service thread
        internal void Entry()
        {
            while (Working)
            {
                try
                {
                    Loop();
                }
                catch(SocketException e)
                {
                    Log.Write("socket exception: " + e.Message);
                }

                // don't spin the processor
                Thread.Sleep(100);
            }
        }

        // make sure the service thread exits
        // this is called from main thread
        internal void Join()
        {
            Working = false;
            serviceThread.Join();
            Console.WriteLine("joined successfully");
        }

        internal void AddWork(ChatWorkUnit unit)
        {
            unitsLock.WaitOne();
            try
            {
                units.Enqueue(unit);
            }
            finally
            {
                unitsLock.ReleaseMutex();
            }
        }

        // remove and return unit
        private ChatWorkUnit GetWork()
        {
            ChatWorkUnit cached;
            unitsLock.WaitOne();
            try
            {
                if (units.Count == 0)
                    cached = null;
                else
                    cached = units.Dequeue();
            }
            finally
            {
                unitsLock.ReleaseMutex();
            }

            return cached;
        }

        // everything is processed here
        private void Loop()
        {
            ChatWorkUnit unit;
            while ((unit = GetWork()) != null)
            {

                if (unit == null)
                    return;

                switch (unit.type)
                {
                    case ChatWorkUnitType.Connect:
                        ProcessConnect((ChatWorkUnitConnect)unit);
                        break;
                    case ChatWorkUnitType.NewChat:
                        ProcessNewChat((ChatWorkUnitNewChat)unit);
                        break;
                    case ChatWorkUnitType.Subscribe:
                        ProcessSubscribe((ChatWorkUnitSubscribe)unit);
                        break;
                    case ChatWorkUnitType.Message:
                        ProcessMessage((ChatWorkUnitMessage)unit);
                        break;
                    default:
                        Log.Write("illegal ChatWorkUnitType: " + unit.type);
                        break;
                }
            }

            RecvCommand();
        }

        private void RecvCommand()
        {
            if (tcpstream == null)
                return;

            if (tcpstream.DataAvailable)
            {
                ServerCommand type = (ServerCommand)tcpin.ReadByte();

                switch (type)
                {
                    case ServerCommand.ListChats:
                        ServerCmdListChats();
                        break;
                    case ServerCommand.NewChat:
                        ServerCmdNewChat();
                        break;
                    case ServerCommand.Subscribe:
                        ServerCmdSubscribe();
                        break;
                    case ServerCommand.Message:
                        ServerCmdMessage();
                        break;
                    case ServerCommand.Heartbeat:
                        // ignore
                        break;
                    default:
                        Log.Write("illegal ServerCommand: " + type);
                        break;
                }
            }
        }

        // process ChatWorkUnitType.Connect
        private void ProcessConnect(ChatWorkUnitConnect unit)
        {
            connectCallback = unit.connectCallback;
            clientname = unit.name;

            // connect to unit.target
            try
            {
                TcpClient client = new TcpClient(unit.target, CHAT_PORT);
                tcpstream = client.GetStream();
                tcpout = new BinaryWriter(tcpstream, Encoding.ASCII);
                tcpin = new BinaryReader(tcpstream, Encoding.ASCII);
            }
            catch (SocketException e)
            {
                // notify the client
                connectCallback(false, null);
                return;
            }

            // tell server my name
            ClientCmdIntroduce(unit.name);

            // ask server for chat list
            ClientCmdListChats();
        }

        // process ChatWorkUnitType.NewChat
        private void ProcessNewChat(ChatWorkUnitNewChat unit)
        {
            newChatCallback = unit.newChatCallback;

            ClientCmdNewChat(unit.name,unit.desc);
        }

        // process ChatWorkUnitType.Subscribe
        private void ProcessSubscribe(ChatWorkUnitSubscribe unit)
        {
            subscribeCallback = unit.subscribeCallback;
            msgCallback = unit.msgCallback;

            ClientCmdSubscribe(unit.name, 0);
        }

        // process ChatWorkUnitType.Message
        private void ProcessMessage(ChatWorkUnitMessage unit)
        {
            ClientCmdMessage(unit.messageType, unit.text, unit.raw);
        }

        // send a string
        private void SendString(string str)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(str);
            // send the length
            tcpout.Write((UInt32)str.Length);

            // send the bytes
            tcpout.Write(bytes);
        }

        // get a string off the network
        private string GetString()
        {
            UInt32 len = tcpin.ReadUInt32();
            byte[] bytes = new byte[len];
            tcpin.Read(bytes, 0, (Int32)len);

            return Encoding.ASCII.GetString(bytes);
        }

        /**
         * Command functions implementing enum ClientCommand.*
         */

        // introduce client to the server
        // implements ClientCommand.Introduce
        private void ClientCmdIntroduce(string name)
        {
            ClientCommand type = ClientCommand.Introduce;
            tcpout.Write((byte)type);

            SendString(clientname);
        }

        // ask the server for list of chats
        // implements ClientCommand.ListChats
        private void ClientCmdListChats()
        {
            ClientCommand type = ClientCommand.ListChats;
            tcpout.Write((byte)type);
        }

        // ask the server to make a new chat
        // implements ClientCommand.NewChat
        private void ClientCmdNewChat(string chatname, string description)
        {
            ClientCommand type = ClientCommand.NewChat;
            tcpout.Write((byte)type);

            SendString(chatname);
            SendString(clientname);
            SendString(description);
        }

        // subscribe to a chat
        // implements ClientCommand.Subscribe
        private void ClientCmdSubscribe(string chatname, int latestMessageID)
        {
            ClientCommand type = ClientCommand.Subscribe;
            tcpout.Write((byte)type);

            SendString(chatname);
            tcpout.Write((UInt64)latestMessageID);
        }

        // send a message
        // implements ClientCommand.Message
        private void ClientCmdMessage(MessageType mtype, string text, byte[] raw)
        {
            ClientCommand type = ClientCommand.Message;
            tcpout.Write((byte)type);

            tcpout.Write((byte)mtype);
            SendString(text);

            UInt64 rawSize = raw == null ? 0 : (UInt64)raw.Length;
            tcpout.Write(rawSize);

            if(raw != null)
                tcpout.Write(raw);
        }

        // send a heartbeat to the server
        // implements ClientCmd.Heartbeat
        private void ClientCmdHeartbeat()
        {
            ClientCommand type = ClientCommand.Heartbeat;
            tcpout.Write((byte)type);
        }

        /**
         * Command functions implementing enum ServerCommand.*
         */
        
        // server is sending a chat list
        // implements ServerCommand.ListChats
        private void ServerCmdListChats()
        {
            string serverName = GetString();
            // get the number of chat
            UInt64 count = tcpin.ReadUInt64();

            var list = new List<Chat>();
            for(UInt64 i = 0; i < count; ++i)
            {
                UInt64 id = tcpin.ReadUInt64();
                string name = GetString();
                string creator = GetString();
                string description = GetString();

                list.Add(new Chat(name, creator, description));
            }

            connectCallback(true, list);
        }

        // server is sending receipt of new chat
        // implements ServerCommand.NewChat
        private void ServerCmdNewChat()
        {
            byte worked = tcpin.ReadByte();

            newChatCallback(worked == 1);
        }

        // server is sending subscription receipt
        // implements ServerCommand.Subscribe
        private void ServerCmdSubscribe()
        {
            byte worked = tcpin.ReadByte();
            if (worked != 1)
            {
                subscribeCallback(false, null);
                return;
            }

            var list = new List<Message>();

            UInt64 count = tcpin.ReadUInt64();

            for(UInt64 i = 0; i < count; ++i)
            {
                UInt64 id = tcpin.ReadUInt64();
                MessageType type = (MessageType)tcpin.ReadByte();
                string text = GetString();
                string sender = GetString();
                byte[] raw = null;
                UInt64 rawSize = tcpin.ReadUInt64();
                if(rawSize > 0)
                    raw = tcpin.ReadBytes((int)rawSize);

                list.Add(new Message(type, id, text, sender, raw));
            }

            // notify the user
            subscribeCallback(true, list);
        }

        // server is sending a message
        // implements ServerCommand.Message
        private void ServerCmdMessage()
        {
            UInt64 id = tcpin.ReadUInt64();
            MessageType type = (MessageType)tcpin.ReadByte();
            string text = GetString();
            string sender = GetString();
            byte[] raw = null;
            UInt64 rawSize = tcpin.ReadUInt64();
            if (rawSize > 0)
                raw = tcpin.ReadBytes((int)rawSize);

            msgCallback(new Message(type, id, text, sender, raw));
        }
    }
}
