using System;
namespace SocketServer
{
    internal class Program
    {
        static void Main(string[] args)
        {
            ServerSocket server = new ServerSocket();
            Console.WriteLine("SERVER VERSION 0.8.9");
            Console.ReadKey();
        }
    }
}
