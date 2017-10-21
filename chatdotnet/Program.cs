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
            client.Shutdown();
            Console.ReadKey();
        }
    }
}
