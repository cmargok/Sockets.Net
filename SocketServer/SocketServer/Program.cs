using System;
namespace SocketServer
{
    internal class Program
    {
        static void Main(string[] args)
        {
            ServerSocket server = new ServerSocket();
            Console.WriteLine("SERVER VERSION 1.0.0\n");
            try {
                Console.ReadKey();
            }catch (Exception exception){
                Console.WriteLine(@"\_(o.O)_/");
            }
            
        }
    }
}
