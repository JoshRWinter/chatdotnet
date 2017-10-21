using System;
using System.Threading;
using System.Collections.Generic;

namespace chatdotnet
{
    class ChatService
    {
        private Thread serviceThread;

        private Queue<ChatWorkUnit> units; // work units to be processed
        private Mutex unitsLock; // protects <units>

        volatile bool working;
        private Mutex workingLock; // protects <working> variable

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
                Loop();

                // don't spin the processor
                Thread.Sleep(400);
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
        }

        private void ProcessConnect(ChatWorkUnitConnect unit)
        {
            connectCallback = unit.connectCallback;
            unit.connectCallback(true, null);
        }

        private void ProcessNewChat(ChatWorkUnitNewChat unit)
        {
            newChatCallback = unit.newChatCallback;
            unit.newChatCallback(true);
        }

        private void ProcessSubscribe(ChatWorkUnitSubscribe unit)
        {
            subscribeCallback = unit.subscribeCallback;
            msgCallback = unit.msgCallback;
            unit.subscribeCallback(true, null);
        }

        private void ProcessMessage(ChatWorkUnitMessage unit)
        {
            msgCallback(null);
        }
    }
}
