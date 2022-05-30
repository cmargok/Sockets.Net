using System;
namespace SocketServer
{
    internal class Program
    {
        static void Main(string[] args)
        {
            ServerSocket server = new ServerSocket();
            Console.WriteLine("SERVER VERSION 0.9.4\n");
            Console.ReadKey();
        }
    }
}
