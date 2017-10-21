using System;
using System.Threading;

namespace chatdotnet
{
    class ChatService
    {
        private Thread serviceThread;
        volatile bool working;

        internal ChatService()
        {
            working = true;
            serviceThread = new Thread(this.Entry);
            serviceThread.Start();
        }

        // entry point for the service thread
        internal void Entry()
        {
            while (working)
            {
                Loop();
            }
        }

        // make sure the service thread exits
        internal void Join()
        {
            working = false;
            serviceThread.Join();
            Console.WriteLine("joined successfully");
        }

        // everything is processed here
        private void Loop()
        {
            
        }
    }
}
