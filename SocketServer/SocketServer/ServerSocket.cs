using Models;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace SocketServer
{    
        enum Remitente
        {
            AllClientsOnline,
            NewClient_Linked,
            Incoming_Message,
            SendingServerDat,
            ClientConnClosed,
        }
        public class ServerSocket
        {
            private Socket socketServer;
            private Thread ListeningThread;
            private Hashtable usersTable;
            private Guid Server_Id;
            private ServerData serverData;
        /*
         * generando informacion para uso del servidor
         * isntanciando los objetos necesarios para su funcionamiento*/
        public ServerSocket()
            {
            try
                {
                    IPHostEntry host = Dns.GetHostEntry("localhost");
                    IPAddress addr = host.AddressList[0];
                    IPEndPoint endPoint = new IPEndPoint(addr, 4404);

                    socketServer = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    Console.WriteLine("\n********************************************\n            Configuring Server");                    
                    usersTable = new Hashtable();
                    Server_Id = Guid.NewGuid();
                    serverData = new ServerData { ServerId = Server_Id,  Name = ".NET SERVER", };
                    socketServer.Bind(endPoint);
                    socketServer.Listen(10);
                    Console.WriteLine("             Starting Server");
                    ListeningThread = new Thread(Listen); 
                    ListeningThread.Start();  
                    for (int i = 0; i <= 100; i++)
                    {
                        Console.Write("\r                  {0}    ", i + "%");
                        Thread.Sleep(3);
                    }
                    Thread.Sleep(80);
                    Console.WriteLine("\nID ->"+serverData.ServerId);
                    Console.WriteLine("         Success Configuration\n             Server Running\n********************************************\n");                

                }
                catch (Exception ex)
                {

                Console.WriteLine("CONFIGURATING SERVER ERROR");
                Console.WriteLine(ex.Message + "\nSOURCE\n" + ex.Source + "\nTARGET\n" + ex.TargetSite + "\nSTACKTRACE\n" + ex.StackTrace);
                
                   
                }
        }

            //poniendo el servidor a escuchar cualquier conexion entrantes
            //Group S functions
            private void Listen()
            {
                Socket clientSocket;
                while (true)
                {
                    clientSocket = socketServer.Accept();
                    ListeningThread = new Thread(()=>ListenClient(clientSocket));
                    ListeningThread.Start();
                }
            }

            private void ListenClient(Socket NewClientConnected)
            {               
                object received;
                var client = ResponseToNewClientConnection(NewClientConnected);
                try
                {            
              
                    this.usersTable.Add(client._clientes[0], NewClientConnected);
                    if (usersTable.Count > 1) this.BroadCast(client._clientes[0], false);
                    this.SendAllUsersToClient(NewClientConnected);

                    while (true)
                    {
                        received = this.Receive(NewClientConnected);
                        if (received is null)
                        {
                            break;
                        }
                        else if (received is ClientMessage)
                        {
                            var message = (ClientMessage)received;

                            if (message.remitente == Remitente.ClientConnClosed.ToString())
                            {
                                Console.WriteLine("Cliente Disconnected -> "+ message.clientFrom.ClientName );
                                var clientT = message.clientFrom;
                                this.BroadCast(client._clientes[0], true);
                                usersTable.Remove(client._clientes[0]);
                                NewClientConnected.Close();
                                break;
                        }
                            else
                            {
                                if(message.clientTo.ClientId == serverData.ServerId)
                                    {
                                        Console.WriteLine("Mensaje Para todos los usuarios -->"); 
                                        Console.WriteLine("from :" + message.clientFrom.ClientName + " ||| to ALL");
                                        //Console.WriteLine("mensaje: ->>" + message.Message);
                                        SendMessage(message, true);
                                }
                                else
                                {
                                    Console.WriteLine("Mensaje Privado");
                                   // Console.WriteLine("from :" + message.clientFrom.ClientName + " ||| to : " + message.clientTo.ClientName);
                                   // Console.WriteLine("mensaje: ->>" + "**********");
                                    SendMessage(message, false);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {                
                     Console.WriteLine(ex.Message + "\nSOURCE\n" + ex.Source + "\nTARGET\n" + ex.TargetSite + "\nSTACKTRACE\n" + ex.StackTrace); 
                }
            }


            //Group A functions
            private ClientsSendListModel ResponseToNewClientConnection(Socket NewClientConnected)
            {
                object received;
                try
                {
                    do
                    {
                        Console.WriteLine("********************************************\n           Connection received");
                        received = this.Receive(NewClientConnected, true);
                        string SendFirstData = generateBytesToSend(serverData, Remitente.SendingServerDat, true, "none");
                        NewClientConnected.Send(Encoding.ASCII.GetBytes(SendFirstData));

                    } while (!(received is ClientsSendListModel));
                    return (ClientsSendListModel)received;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message + "\nSOURCE\n" + ex.Source + "\nTARGET\n" + ex.TargetSite + "\nSTACKTRACE\n" + ex.StackTrace);
                    return null;
                }
            }


        private void SendMessage(ClientMessage _clienteSendModel, bool toALL)
        {

            //aqui debo especificar si el mensaje es all
            ClientModel tmpUser;
            if (toALL)
            {
               
                foreach (DictionaryEntry dictionaryEntry in this.usersTable)
                {
                    tmpUser = (ClientModel)dictionaryEntry.Key;
                    if (tmpUser.ClientId != _clienteSendModel.clientFrom.ClientId)
                    {
                        this.Send((Socket)dictionaryEntry.Value, _clienteSendModel, Remitente.Incoming_Message);
                       
                    }
                }
            }
            else
            {
              
                foreach (DictionaryEntry dictionaryEntry in this.usersTable)
                {
                    tmpUser = (ClientModel)dictionaryEntry.Key;

                    if (tmpUser.ClientId == _clienteSendModel.clientTo.ClientId)
                    {
                        this.Send((Socket)dictionaryEntry.Value, _clienteSendModel, Remitente.Incoming_Message);
                        break;
                    }
                }
            }
        }


        /// Send a object to all users connected
        private void BroadCast(object client, bool salida)
        {
            if (!salida)
            {
                foreach (DictionaryEntry dictionaryEntry in this.usersTable)
                {

                    if ((ClientModel)client != dictionaryEntry.Key)
                    {
                        this.Send((Socket)dictionaryEntry.Value, client, Remitente.NewClient_Linked);
                    }
                }
            }
            else
            {
                foreach (DictionaryEntry dictionaryEntry in this.usersTable)
                {

                    if ((ClientModel)client != dictionaryEntry.Key)
                    {
                        this.Send((Socket)dictionaryEntry.Value, client, Remitente.ClientConnClosed);
                    }
                }
            }
        }

            /// Send all connected users to the client
            private void SendAllUsersToClient(Socket socket)
            {
                //Creating a list with all the clients

                List<ClientModel> ClientsONLINE = new List<ClientModel>();
                foreach (DictionaryEntry dictionaryEntry in this.usersTable)
                {
                    var dictionaryKeyClient = (ClientModel)dictionaryEntry.Key;

                    ClientsONLINE.Add(dictionaryKeyClient);
                }

                foreach (DictionaryEntry dictionaryEntry in this.usersTable)
                {
                    this.Send((Socket)dictionaryEntry.Value, ClientsONLINE, Remitente.AllClientsOnline);
                }
            }


            private void Send(Socket socket, object client, Remitente remitente)
            {
                string sendDataClient;
                try
                {
                    List<ClientModel> ListOfClientsModel = new List<ClientModel>();

                    if (remitente == Remitente.AllClientsOnline)
                    {
                        sendDataClient = this.generateBytesToSend(client, remitente, false, "none");
                        socket.Send(Encoding.ASCII.GetBytes(sendDataClient));
                    }
                    else if (remitente == Remitente.NewClient_Linked)
                    {
                        var moment2 = (ClientModel)client;
                        ListOfClientsModel.Add(moment2);
                        sendDataClient = this.generateBytesToSend(ListOfClientsModel, remitente, false, "none");
                        socket.Send(Encoding.ASCII.GetBytes(sendDataClient));

                    }
                    else if (remitente == Remitente.ClientConnClosed) 
                    { 
                        var close = (ClientModel)client;
                        ListOfClientsModel.Add(close);
                        sendDataClient = this.generateBytesToSend(ListOfClientsModel, remitente, false, "none");
                        socket.Send(Encoding.ASCII.GetBytes(sendDataClient));
                    }else if(remitente == Remitente.Incoming_Message)
                    {
                            var toSend = (ClientMessage)client;
                            sendDataClient = this.generateBytesToSend(toSend, remitente, false, "none");
                            socket.Send(Encoding.ASCII.GetBytes(sendDataClient));

                    }
                }
                catch (Exception ex)
                {
                    sendDataClient = this.generateBytesToSend(client, remitente, false, "wrong-data");
                    socket.Send(Encoding.ASCII.GetBytes(sendDataClient));
                    Console.WriteLine(ex.Message + "\nSOURCE\n" + ex.Source + "\nTARGET\n" + ex.TargetSite + "\nSTACKTRACE\n" + ex.StackTrace);
                }
            }



            private string generateBytesToSend(object client, Remitente remitente, bool success, string Error, Socket to = null)
            {
                ClientsSendListModel ListClients;
                try
                {
                    switch (remitente)
                    {

                        case Remitente.SendingServerDat:
                            ServerDataResponse serverDataResponse  = new ServerDataResponse();
                            ServerData tempServerData = (ServerData)client;
                            tempServerData.ClientCount = usersTable.Count + 1;
                            serverDataResponse.ServerData = tempServerData;
                            serverDataResponse.Succcess = success;
                            serverDataResponse.Error = Error;
                            serverDataResponse.ErrorDetail = "";
                            serverDataResponse.remitente = remitente.ToString();
                            serverDataResponse.NumberOfRecords = 1;
                            return JsonConvert.SerializeObject(serverDataResponse);


                        case Remitente.AllClientsOnline:
                            ListClients = new ClientsSendListModel();
                            List<ClientModel> tempClientList = (List<ClientModel>)client;
                            ListClients._clientes = tempClientList;
                            ListClients.Succcess = success;
                            ListClients.Error = Error;
                            ListClients.ErrorDetail = "";
                            ListClients.remitente = remitente.ToString();
                            ListClients.NumberOfRecords = ListClients._clientes.Count;
                            return JsonConvert.SerializeObject(ListClients);


                        case Remitente.ClientConnClosed:
                            ListClients = new ClientsSendListModel();
                            var tempClientgone = (List<ClientModel>)client;
                            ListClients._clientes = tempClientgone;
                            ListClients.Succcess = success;
                            ListClients.Error = Error;
                            ListClients.ErrorDetail = "";
                            ListClients.remitente = remitente.ToString();
                            ListClients.NumberOfRecords = ListClients._clientes.Count;
                            return JsonConvert.SerializeObject(ListClients);


                        case Remitente.NewClient_Linked:
                            ListClients = new ClientsSendListModel();
                            var tempClientLists = (List<ClientModel>)client;
                            ListClients._clientes = tempClientLists;
                            ListClients.Succcess = success;
                            ListClients.Error = Error;
                            ListClients.ErrorDetail = "";
                            ListClients.remitente = remitente.ToString();
                            ListClients.NumberOfRecords = ListClients._clientes.Count;
                            return JsonConvert.SerializeObject(ListClients);


                        case Remitente.Incoming_Message:
                             var tempMessage = (ClientMessage)client;
                             ClientMessage clientMessage = tempMessage;
                            clientMessage.Succcess = success;
                            clientMessage.Error = Error;
                            clientMessage.ErrorDetail = "";
                            clientMessage.remitente = remitente.ToString();
                            clientMessage.NumberOfRecords = 1;
                             return JsonConvert.SerializeObject(clientMessage);


                        default:
                            client = null;
                            return null;
                    }
                }
                catch (Exception ex)
                {                                  
                    Console.WriteLine(ex.Message + "\nSOURCE\n" + ex.Source + "\nTARGET\n" + ex.TargetSite + "\nSTACKTRACE\n" + ex.StackTrace);
                    return null;
                }
            }
            

            /// <summary>
            /// Receive all the serialized object
            /// </summary>
            /// <param name="socket">Socket that receive the object</param>
            /// <returns>Object received from client</returns>
            private object Receive(Socket socket, bool FirstConnection = false)
            {
               
                byte[] buffer = new byte[2048];
                string dataClient="";
                try
                {
                if (socket.Connected)
                {
                    int BytesAmountReceived = socket.Receive(buffer);
                    dataClient = Encoding.ASCII.GetString(buffer, 0, BytesAmountReceived);              
                }
                else
                {
                    Console.WriteLine("ya valimos");
                    ClientMessage clientMessage = JsonConvert.DeserializeObject<ClientMessage>(dataClient);
                    return clientMessage;
                }

                if (FirstConnection)
                    {
                        ClientsSendListModel firstConnection = JsonConvert.DeserializeObject<ClientsSendListModel>(dataClient);
                        Console.WriteLine("          Connection Succes : " + firstConnection.Succcess);
                        foreach (var client in firstConnection._clientes)
                        {
                            Console.WriteLine("             || New Client ||\n          Name: --> " + client.ClientName + " <--\nID: -> " + client.ClientId);
                            Console.WriteLine("********************************************\n");
                        }
                        return firstConnection;
                    }
                    else
                    {                    
                        
                        ClientMessage clientMessage = JsonConvert.DeserializeObject<ClientMessage>(dataClient);                       
                        return clientMessage;                   
                       
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message + "\nSOURCE\n" + ex.Source + "\nTARGET\n" + ex.TargetSite + "\nSTACKTRACE\n" + ex.StackTrace);
                    return null;
                }

            }
        }    
}
