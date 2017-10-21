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

            client.Connect("target", " ayy", (success,chats) =>
            {
                Console.WriteLine("success = " + success);
            });

            client.NewChat("coolchat", "this is awsome chat", (success) =>
            {
                Console.WriteLine("new chat success = " + success);
            });

            client.Subscribe("coolchat", (success, msgs) =>
            {
                Console.WriteLine("subscribe success = " + success);
            }, (msg) =>
            {
                Console.WriteLine("message");
            });

            client.Message("ayy");

            Console.ReadKey();
            client.Shutdown();
            Console.ReadKey();
        }
    }
}
