using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ClientSocketS
{
    public class App
    {
        public string name { get; set; }
        public App()
        {
            Console.WriteLine("\n********************************************");
            Console.WriteLine("      Welcome to the .NET SERVER CLIENT");
            Console.WriteLine("               ***********");
            Console.WriteLine("  Enter your Username to sign into the server\n");

            Console.Write("********  USERNAME: ");
            name = Console.ReadLine();
            Console.WriteLine("*********************************************");
            Console.WriteLine("******************Accepted*******************");
            Console.WriteLine("*********************************************");
            Thread.Sleep(500);
            Console.Clear();

            int time = 100;
            string[] welcome = new string[8];
            welcome[0] = "********************************************";
            welcome[1] = "*****************WELCOME********************";
            welcome[2] = "*******************TO***********************";
            welcome[3] = "**************.NET SERVER*******************";
            welcome[4] = "********************************************";
            welcome[5] = "*************VERSION 0.8.7******************";
            welcome[6] = "************MAY - 27 - 2022*****************";
            welcome[7] = "********************************************";
            for (int i = 0; i < 8; i++)
            {
                Console.WriteLine(welcome[i]);
                Thread.Sleep(time);
            }

            Thread.Sleep(time * 4);
            Console.Clear();

            ClientSocket client = new(name);
        }
        
    }
}
