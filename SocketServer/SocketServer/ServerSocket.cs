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
            GeneralSvMessage,
            SendingServerDat,
            ClientConnClosed,
            PrivateUsMessage,
            requesttToServer
    }
        public class ServerSocket
        {
            private Socket socketServer;
            private Thread ListeningThread;
            private Hashtable usersTable;
            private Dictionary<Guid, Guid> ListUsersInPrivateChat;
            private Guid Server_Id;
            private ServerData serverData;
        /*
         * generando informacion para uso del servidor
         * isntanciando los objetos necesarios para su funcionamiento*/
        public ServerSocket()
            {
            try
                {
                    IPAddress addr = IPAddress.Parse("0.0.0.0");
                    IPEndPoint endPoint = new IPEndPoint(addr, 4404);

                    socketServer = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    Console.WriteLine("made by Ing. Kevin camargo");
                    Console.WriteLine("\n********************************************\n            Configuring Server");                    
                    usersTable = new Hashtable();
                    ListUsersInPrivateChat = new Dictionary<Guid, Guid>();
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
                        else if (received is MessageModel)
                        {
                            var message = (MessageModel)received;

                        if (message.remitente == Remitente.ClientConnClosed.ToString())
                        {
                            Console.WriteLine("\n**** -> USER DISCONNECTED -> " + message.clientFrom.ClientName);
                            var clientT = message.clientFrom;
                            this.BroadCast(client._clientes[0], true);
                            usersTable.Remove(client._clientes[0]);
                            this.SendAllUsersToClient(NewClientConnected);
                            NewClientConnected.Close();
                            break;
                        }

                        else if (message.remitente == Remitente.PrivateUsMessage.ToString())
                        {
                            
                            
                                if (message.Message.Length >0 && message.Message[0] == '?')
                                {
                                    var newPrivateChat = message.Message.Split('|');
                                    if (newPrivateChat[2].Substring(0, 2) == "-c")
                                    {
                                      
                                        Console.ForegroundColor = ConsoleColor.Blue;
                                        Console.WriteLine("Private Chat Dispose Request");
                                        Console.ForegroundColor = ConsoleColor.White;                                     
                                        message.Message = "Private Chat Disposed";                                       
                                        message.clientFrom.status = true;
                                        message.clientTo.status = true;

                                        SendMessage(message, false, true);
                                        message.Message = newPrivateChat[1] + " " + newPrivateChat[3];
                                        SendMessage(message, false);
                                        ListUsersInPrivateChat.Remove(message.clientTo.ClientId);
                                        Console.ForegroundColor = ConsoleColor.Green;
                                        Console.WriteLine("Private Chat Disposed");
                                        Console.ForegroundColor = ConsoleColor.White;
                                    }    
                                    
                                }
                                else  if (message.Message.Length>0 && message.Message[0] == '*')
                                {
                                        Console.ForegroundColor = ConsoleColor.Blue;
                                        Console.WriteLine("New Private Chat Request");
                                        Console.ForegroundColor = ConsoleColor.White;

                                    if (!ListUsersInPrivateChat.ContainsKey(message.clientTo.ClientId) && !ListUsersInPrivateChat.ContainsValue(message.clientTo.ClientId))
                                    {                                        
                                        var mm = message.Message;
                                        message.Message = mm;
                                        ListUsersInPrivateChat.Add(message.clientTo.ClientId, message.clientFrom.ClientId);
                                        message.clientTo.status = false;
                                        ResponseRequest(message, NewClientConnected, true);
                                        message.Message = mm;
                                        SendMessage(message, false);
                                        Console.ForegroundColor = ConsoleColor.Green;
                                        Console.WriteLine("Guaranteed");
                                        Console.ForegroundColor = ConsoleColor.White;
                                     }
                                    else
                                    {
                                        message.Message = "User is not available right now";
                                        message.clientFrom.status = true;
                                        var changingFrom = message.clientFrom;
                                        message.clientFrom = message.clientTo;
                                        message.clientTo = changingFrom;
                                        message.ErrorDetail = "User is not available right now";
                                        message.Succcess = false;
                                        ResponseRequest(message, NewClientConnected, false);
                                        SendMessage(message, false);
                                        Console.ForegroundColor = ConsoleColor.Red;
                                        Console.WriteLine("Denied");
                                        Console.ForegroundColor = ConsoleColor.White;

                                    }
                                }
                              
                                else if (message.Message.Length > 0)
                                {
                                    message.clientTo.status = false;
                                    SendMessage(message, false);                                   
                                    Console.WriteLine("Private Message");
                                }

                               

                            
                        }
                        else if (message.clientTo.ClientId == serverData.ServerId)
                        {
                            Console.WriteLine("To ALL --> From --> " + message.clientFrom.ClientName);
                            //Console.WriteLine("mensaje: ->>" + message.Message);
                            SendMessage(message, true);
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
            private ListUsersModel ResponseToNewClientConnection(Socket NewClientConnected)
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

                    } while (!(received is ListUsersModel));
                    return (ListUsersModel)received;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message + "\nSOURCE\n" + ex.Source + "\nTARGET\n" + ex.TargetSite + "\nSTACKTRACE\n" + ex.StackTrace);
                    return null;
                }
            }

        private void ResponseRequest(MessageModel response, Socket socket, bool success)
        {
            this.Send(socket, response, Remitente.requesttToServer, success);
        }
        private void SendMessage(MessageModel _clienteSendModel, bool toALL, bool msgpri = false)
        {

            //aqui debo especificar si el mensaje es all
            ClientDataModel tmpUser;
            if (toALL)
            {
               
                foreach (DictionaryEntry dictionaryEntry in this.usersTable)
                {
                    tmpUser = (ClientDataModel)dictionaryEntry.Key;
                    if (tmpUser.ClientId != _clienteSendModel.clientFrom.ClientId)
                    {
                        this.Send((Socket)dictionaryEntry.Value, _clienteSendModel, Remitente.GeneralSvMessage, true);
                       
                    }
                }
            }
            else if (msgpri)
            {
                bool from = false;
                bool to = false;
                foreach (DictionaryEntry dictionaryEntry in this.usersTable)
                {
                    tmpUser = (ClientDataModel)dictionaryEntry.Key;

                    if (tmpUser.ClientId == _clienteSendModel.clientTo.ClientId)
                    {
                        to = true;
                        this.Send((Socket)dictionaryEntry.Value, _clienteSendModel, Remitente.PrivateUsMessage, true);
                        
                    }
                    else if (tmpUser.ClientId == _clienteSendModel.clientFrom.ClientId)
                    {
                        from = true;
                        this.Send((Socket)dictionaryEntry.Value, _clienteSendModel, Remitente.PrivateUsMessage, true);
                        
                    }

                    if(from && to)
                    {
                      
                        break;
                    }
                }
            }
            else 
            {

                foreach (DictionaryEntry dictionaryEntry in this.usersTable)
                {
                    tmpUser = (ClientDataModel)dictionaryEntry.Key;

                    if (tmpUser.ClientId == _clienteSendModel.clientTo.ClientId)
                    {
                        this.Send((Socket)dictionaryEntry.Value, _clienteSendModel, Remitente.PrivateUsMessage, true);
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

                    if ((ClientDataModel)client != dictionaryEntry.Key)
                    {
                        this.Send((Socket)dictionaryEntry.Value, client, Remitente.NewClient_Linked);
                    }
                }
            }
            else
            {
                foreach (DictionaryEntry dictionaryEntry in this.usersTable)
                {

                    if ((ClientDataModel)client != dictionaryEntry.Key)
                    {
                        this.Send((Socket)dictionaryEntry.Value, client, Remitente.ClientConnClosed);
                    }
                }
            }
        }

            /// Send all LIST connected users to the client
            private void SendAllUsersToClient(Socket socket)
            {
                //Creating a list with all the clients

                List<ClientDataModel> ClientsONLINE = new List<ClientDataModel>();
                foreach (DictionaryEntry dictionaryEntry in this.usersTable)
                {
                    var dictionaryKeyClient = (ClientDataModel)dictionaryEntry.Key;

                    ClientsONLINE.Add(dictionaryKeyClient);
                }

                foreach (DictionaryEntry dictionaryEntry in this.usersTable)
                {
                    this.Send((Socket)dictionaryEntry.Value, ClientsONLINE, Remitente.AllClientsOnline);
                }
            }


            private void Send(Socket socket, object client, Remitente remitente, bool success = true)
            {
                string sendDataClient ="";
                ClientDataModel clientDataModel = new ClientDataModel();
                List<ClientDataModel> ListOfClientsModel = new List<ClientDataModel>();
                try
                {
                    if (remitente == Remitente.AllClientsOnline)
                    {
                        sendDataClient = this.generateBytesToSend(client, remitente, true, "none");
                    }
                    else if (remitente == Remitente.NewClient_Linked || remitente == Remitente.ClientConnClosed)
                    {
                        clientDataModel = (ClientDataModel)client;
                        ListOfClientsModel.Add(clientDataModel);
                        sendDataClient = this.generateBytesToSend(ListOfClientsModel, remitente, true, "none");

                    } 
                    else if (remitente == Remitente.GeneralSvMessage || remitente == Remitente.PrivateUsMessage || remitente == Remitente.requesttToServer)
                    {
                        var toSend = (MessageModel)client;
                        sendDataClient = this.generateBytesToSend(toSend, remitente, success, "none");
                    }
                    socket.Send(Encoding.ASCII.GetBytes(sendDataClient));
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
                ListUsersModel ListClients;
                MessageModel badResponseGeneral = new MessageModel();
                try
                {
                    if (!success)
                    {
                        throw new InvalidCastException(Error);
                    }
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
                            ListClients = new ListUsersModel();
                            List<ClientDataModel> tempClientList = (List<ClientDataModel>)client;
                            ListClients._clientes = tempClientList;
                            ListClients.Succcess = success;
                            ListClients.Error = Error;
                            ListClients.ErrorDetail = "";
                            ListClients.remitente = remitente.ToString();
                            ListClients.NumberOfRecords = ListClients._clientes.Count;
                            return JsonConvert.SerializeObject(ListClients);


                        case Remitente.ClientConnClosed:
                            ListClients = new ListUsersModel();
                            var tempClientgone = (List<ClientDataModel>)client;
                            ListClients._clientes = tempClientgone;
                            ListClients.Succcess = success;
                            ListClients.Error = Error;
                            ListClients.ErrorDetail = "";
                            ListClients.remitente = remitente.ToString();
                            ListClients.NumberOfRecords = ListClients._clientes.Count;
                            return JsonConvert.SerializeObject(ListClients);


                        case Remitente.NewClient_Linked:
                            ListClients = new ListUsersModel();
                            var tempClientLists = (List<ClientDataModel>)client;
                            ListClients._clientes = tempClientLists;
                            ListClients.Succcess = success;
                            ListClients.Error = Error;
                            ListClients.ErrorDetail = "";
                            ListClients.remitente = remitente.ToString();
                            ListClients.NumberOfRecords = ListClients._clientes.Count;
                            return JsonConvert.SerializeObject(ListClients);


                        case Remitente.GeneralSvMessage:
                             var tempMessage = (MessageModel)client;
                             MessageModel clientMessage = tempMessage;
                            clientMessage.Succcess = success;
                            clientMessage.Error = Error;
                            clientMessage.ErrorDetail = "";
                            clientMessage.remitente = remitente.ToString();
                            clientMessage.NumberOfRecords = 1;
                             return JsonConvert.SerializeObject(clientMessage);

                        case Remitente.PrivateUsMessage:
                            var tempPrivateMessage = (MessageModel)client;
                            MessageModel privateMessage = tempPrivateMessage;
                            privateMessage.Succcess = success;
                            privateMessage.Error = Error;
                            privateMessage.ErrorDetail = "";
                            privateMessage.remitente = remitente.ToString();
                            privateMessage.NumberOfRecords = 1;
                            return JsonConvert.SerializeObject(privateMessage);
                    case Remitente.requesttToServer:
                            var tempRequest = (MessageModel)client;
                            MessageModel request = tempRequest;
                            request.Message = "";
                            request.Succcess = success;
                            request.Error = Error;
                            request.ErrorDetail = "None";
                            request.remitente = remitente.ToString();
                            request.NumberOfRecords = 1;
                            return JsonConvert.SerializeObject(request);


                    default:
                            client = null;
                            return null;
                    }
                }
                catch (Exception ex)
                {

                    var bad = (MessageModel)client;
                    badResponseGeneral = bad;
                    badResponseGeneral.Message = "";
                    badResponseGeneral.Succcess = false;
                    badResponseGeneral.Error = ex.Message;
                    badResponseGeneral.ErrorDetail = ex.StackTrace;
                    badResponseGeneral.remitente = remitente.ToString();
                    badResponseGeneral.NumberOfRecords = 0;


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
                    MessageModel clientMessage = JsonConvert.DeserializeObject<MessageModel>(dataClient);
                    return clientMessage;
                }

                if (FirstConnection)
                    {
                        ListUsersModel firstConnection = JsonConvert.DeserializeObject<ListUsersModel>(dataClient);
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
                        
                        MessageModel clientMessage = JsonConvert.DeserializeObject<MessageModel>(dataClient);                       
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
