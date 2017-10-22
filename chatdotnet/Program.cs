using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace chatdotnet
{
    class Program
    {
        static void Main(string[] args)
        {
            ChatClient client = new ChatClient("chatdb");

            client.Connect("192.168.1.50", "ayy", (success,chats) =>
            {
                if (!success)
                    Console.WriteLine("couldn't connect");
                else
                {
                    Console.WriteLine("connected! here are the chats");
                    foreach(Chat chat in chats)
                    {
                        Console.WriteLine("name: " + chat.name + ", created by " + chat.creator);
                    }
                }

                // subscribe to the first one
                if(chats != null)
                {
                    client.Subscribe(chats[0], (subscribeSuccess, msgs) =>
                     {
                         if (!subscribeSuccess)
                         {
                             Console.WriteLine("no bueno for subscribo");
                             return;
                         }

                         Console.WriteLine("already have " + msgs.Count + " messages stored locally. here they are:");
                         foreach (Message msg in msgs)
                         {
                             Console.WriteLine("" + msg.id + " " + msg.sender + ": " + msg.text);
                         }
                     }, (msg) =>
                     {
                         Console.WriteLine("newmsg " + msg.id + " " + msg.sender + ": " + msg.text);
                     });
                }
            });

            client.NewChat("coolchat", "this is awsome chat", (success) =>
            {
                Console.WriteLine("new chat success = " + success);
            });

            string line = "";
            while(true)
            {
                line = Console.ReadLine();
                if (line.Equals("quit"))
                    break;
                client.Message(line);
            }

            Console.ReadKey();
            client.Shutdown();
            Console.ReadKey();
        }
    }
}
