using System;

namespace ClientSocketS
{
    internal class Program
    {
        static void Main(string[] args)
        {
            new App();
            Console.WriteLine("Bye bye");
            try {
                Console.ReadKey();
            }catch (Exception exception){

            }
        }
    }
}
