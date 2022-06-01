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
        public class ServerSocket
        {
            private Socket socketServer;
            private Thread ListeningThread;
            private Hashtable usersTable;
            private Dictionary<Guid, Guid> ListUsersInPrivateChat;
            private Guid Server_Id;
            private ServerData serverData;
        private readonly string serverIP;
            /*
             * generando informacion para uso del servidor
             * isntanciando los objetos necesarios para su funcionamiento*/
            public ServerSocket()
                {
                try
                    {
                    IPAddress addr = IPAddress.Parse("0.0.0.0");
                    serverIP = SerachServerIPInternal();
                    IPEndPoint endPoint = new IPEndPoint(addr, 4404);
                    socketServer = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    Console.WriteLine("         MADE BY  ENGR. KEVIN CAMARGO\n\n********************************************\n            Configuring Server");                    
                    usersTable = new Hashtable(); //client / socket
                    ListUsersInPrivateChat = new Dictionary<Guid, Guid>(); //id cliente, id llega
                    Server_Id = Guid.NewGuid();
                    serverData = new ServerData { ServerId = Server_Id,  Name = ".NET SERVER", };
                    socketServer.Bind(endPoint);
                    socketServer.Listen(10);
                    Console.WriteLine("             Starting Server");
                    ListeningThread = new Thread(Listen); 
                    ListeningThread.Start();  
                    for (int i = 0; i <= 100; i++)
                    {
                        Console.Write("\r                  {0}    ", i + "%"); Thread.Sleep(3);
                    }
                    Thread.Sleep(80);
                    Console.WriteLine("\n       SERVER IP -> " + serverIP+"\n         Success Configuration\n             Server Running\n********************************************\n");                

                }
                catch (Exception ex)
                {
                    Console.WriteLine("CONFIGURATING SERVER ERROR\n"+ex.Message +  "\nSTACKTRACE\n" + ex.StackTrace);
                }
            }
                  //poniendo el servidor a escuchar cualquier conexion entrantes           
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
                    //envia la lista 
                    this.SendAllUsersToClient(NewClientConnected);

                    while (true)
                    {
                        received = this.Receive(NewClientConnected);
                        if (received is null)
                        {
                            Console.WriteLine("ERROR");
                            break;
                        }
                        else if (received is MessageModel)
                        {
                            if (!IsMessageModel(received, client, NewClientConnected))
                            {
                                break;
                            }

                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message + "\nSOURCE\n" + ex.Source + "\nTARGET\n" + ex.TargetSite + "\nSTACKTRACE\n" + ex.StackTrace);
                }
            }
            private object Receive(Socket socket, bool FirstConnection = false)
            {

                byte[] buffer = new byte[2048];
                string dataClient = "";
                try
                {
                    if (socket.Connected)
                    {
                        int BytesAmountReceived = socket.Receive(buffer);
                        dataClient = Encoding.ASCII.GetString(buffer, 0, BytesAmountReceived);
                    }
                    else
                    {
                        MessageModel clientMessage = JsonConvert.DeserializeObject<MessageModel>(dataClient);
                        return clientMessage;
                    }

                    if (FirstConnection)
                    {
                        return NewUserConnectionSucces(dataClient);
                    }
                    else
                    {
                        MessageModel clientMessage = JsonConvert.DeserializeObject<MessageModel>(dataClient);
                        return clientMessage;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message + "\nSTACKTRACE\n" + ex.StackTrace);
                    return null;
                }

            }
            private ListUsersModel NewUserConnectionSucces(string dataClient)
            {
                ListUsersModel firstConnection = JsonConvert.DeserializeObject<ListUsersModel>(dataClient);

                Console.WriteLine("          Connection Succes : " + firstConnection.Succcess);
                foreach (var client in firstConnection._clientes)
                {
                    Console.WriteLine("             || New Client ||\n          Name: --> " + client.ClientName + " <--\nID: -> " + client.ClientId
                                    + "\n********************************************\n");
                }
                return firstConnection;
            }
            private bool IsMessageModel(object received, ListUsersModel client, Socket NewClientConnected)
                {
                    var message = (MessageModel)received;
                    if (message.remitente == Remitente.ClientConnClosed.ToString())
                    {
                        UserDisconnected(message, client, NewClientConnected);
                         return false;
                    }
                    else if (message.remitente == Remitente.PrivateUsMessage.ToString())
                    {

                        if (message.Message.Length > 0 && message.Message[0] == '?')
                        {
                            var requestDisposeMessage = message.Message.Split('|');
                            if (requestDisposeMessage[2].Substring(0, 2) == "-c")
                            {
                                killPrivateChat(message, requestDisposeMessage);
                            }

                        }
                        else if (message.Message.Length > 0 && message.Message[0] == '*')
                        {

                            PrintItInColor("New Private Chat Request", FontColors.blue);
                            CheckUserAvailability(message, NewClientConnected);
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
                        SendMessage(message, true);                    
                    }
                    return true;

                }            
            private void CheckUserAvailability(MessageModel message, Socket NewClientConnected)
                {
                    if (!ListUsersInPrivateChat.ContainsKey(message.clientTo.ClientId) && !ListUsersInPrivateChat.ContainsValue(message.clientTo.ClientId))
                    {
                        var lockincomingMessage = message.Message;
                        message.Message = lockincomingMessage;
                        ListUsersInPrivateChat.Add(message.clientTo.ClientId, message.clientFrom.ClientId);
                        message.clientTo.status = false;
                        ResponsePrivateChatRequest(message, NewClientConnected, true);
                        message.Message = lockincomingMessage;
                        SendMessage(message, false);
                        PrintItInColor("Guaranteed", FontColors.green);
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
                        ResponsePrivateChatRequest(message, NewClientConnected, false);
                        PrintItInColor("Denied", FontColors.red);
                    }

                }
            private ListUsersModel ResponseToNewClientConnection(Socket NewClientConnected)
            {
                object received;
                try
                {
                    do
                    {
                        Console.WriteLine("********************************************\n           New Connection Request");
                        received = this.Receive(NewClientConnected, true);
                        string SendFirstData = ServerDataResponse(serverData, Remitente.SendingServerDat, true, "Null");
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
            private void ResponsePrivateChatRequest(MessageModel response, Socket socket, bool success)
            {
                this.Send(socket, response, Remitente.requesttToServer, success);
            }
            private void killPrivateChat(MessageModel message, string[] requestDisposeMessage)
            {
                PrintItInColor("Private Chat Dispose Request", FontColors.blue);
                message.Message = "Private Chat Disposed";
                message.clientFrom.status = true;
                message.clientTo.status = true;
                message.remitente = Remitente.GeneralSvMessage.ToString();
                SendMessage(message, false, true);
                message.Message = requestDisposeMessage[1] + " " + requestDisposeMessage[3];
                SendMessage(message, false);
                ListUsersInPrivateChat.Remove(message.clientTo.ClientId);
                PrintItInColor("Private Chat Disposed", FontColors.green);

            }
            private void UserDisconnected(MessageModel message, ListUsersModel client, Socket NewClientConnected)
            {
                Console.WriteLine("\n**** -> USER DISCONNECTED -> " + message.clientFrom.ClientName);
                var clientT = message.clientFrom;
                this.BroadCast(client._clientes[0], true);
                usersTable.Remove(client._clientes[0]);
                this.SendAllUsersToClient(NewClientConnected);
                NewClientConnected.Close();
            }
            private void SendMessage(MessageModel _clienteSendModel, bool sendToALL, bool disposeChat = false)
            {
                //aqui debo especificar si el mensaje es all
                ClientDataModel tmpUser;
                if (sendToALL)
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
                else if (disposeChat)
                {
                    AlertPrivateChatDisposeToUsers(_clienteSendModel);
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
                    List<ClientDataModel> ClientsONLINE = new();
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
            private void AlertPrivateChatDisposeToUsers(MessageModel _clienteSendModel)
            {
                ClientDataModel tmpUser;
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

                    if (from && to)
                    {
                        break;
                    }
                }
            }
            private void Send(Socket socket, object client, Remitente remitente, bool success = true)
                {
                    string sendDataClient ="";
                    try
                    {
                        if (remitente == Remitente.AllClientsOnline)
                        {
                            sendDataClient = this.GenerateBytesToSend(client, remitente, true);
                        }
                        else if (remitente == Remitente.NewClient_Linked || remitente == Remitente.ClientConnClosed)
                        {                        
                            ClientDataModel  clientDataModel = (ClientDataModel)client;
                            List<ClientDataModel> ListOfClientsModel = new List<ClientDataModel>() { clientDataModel };
                            sendDataClient = this.GenerateBytesToSend(ListOfClientsModel, remitente, true);
                        } 
                        else if (remitente == Remitente.GeneralSvMessage || remitente == Remitente.PrivateUsMessage || remitente == Remitente.requesttToServer)
                        {                       
                            sendDataClient = this.GenerateBytesToSend(client, remitente, success);
                        }
                        socket.Send(Encoding.ASCII.GetBytes(sendDataClient));
                }
                    catch (Exception ex)
                    {
                        sendDataClient = this.GenerateBytesToSend(client, remitente, false, "wrong-data");
                        socket.Send(Encoding.ASCII.GetBytes(sendDataClient));
                        Console.WriteLine(ex.Message + "\nSTACKTRACE\n" + ex.StackTrace);
                    }
                }           
            private string GenerateBytesToSend(object client, Remitente remitente, bool success, string Error = "Null")
                {
                    string generalResponse = "";
                    try
                    {
                        if (!success)
                        {
                            throw new InvalidCastException(Error);
                        
                        }
                        else
                        {
                            switch (remitente)
                            {
                                case Remitente.AllClientsOnline:
                                    //  return AddListResponse(client, remitente, success, Error);                            
                                case Remitente.ClientConnClosed:
                                    //  return AddListResponse(client, remitente, success, Error);                           
                                case Remitente.NewClient_Linked:
                                    generalResponse = AddListResponse(client, remitente, success, Error);
                                    break;
                                case Remitente.GeneralSvMessage:
                                     //     return MessageResponse(client, remitente, success, Error);                           
                                case Remitente.PrivateUsMessage:
                                    generalResponse = MessageResponse(client, remitente, success, Error);
                                    break;
                                case Remitente.requesttToServer:
                                    generalResponse = MessageResponse(client, remitente, success, Error, true); 
                                    break;

                                default:
                                    break;
                            }                   
                        }
                        return generalResponse;
                      }
                    catch (InvalidCastException inex)
                    {
                        var bad = (MessageModel)client;
                        MessageModel badResponseGeneral = bad;
                        badResponseGeneral.Message = "";
                        badResponseGeneral.Succcess = false;
                        badResponseGeneral.Error = inex.Message;
                        badResponseGeneral.ErrorDetail = inex.StackTrace;
                        badResponseGeneral.remitente = remitente.ToString();
                        badResponseGeneral.NumberOfRecords = 0;                    
                        return JsonConvert.SerializeObject(badResponseGeneral);
                    }
                    catch (Exception ex)
                    {
                         Console.WriteLine(ex.Message + "\nSTACKTRACE\n" + ex.StackTrace);
                          return generalResponse;
                    }
                }
            /// <summary>
            /// Receive all the serialized object
            /// </summary>
            /// <param name="socket">Socket that receive the object</param>
            /// <returns>Object received from client</returns>
            private string AddListResponse(object client, Remitente remitente, bool success, string Error)
            {
                ListUsersModel ListClients = new();
                List<ClientDataModel> castingToList = (List<ClientDataModel>)client;
                ListClients._clientes = castingToList;
                ListClients.Succcess = success;
                ListClients.Error = Error;
                ListClients.ErrorDetail = " ";
                ListClients.remitente = remitente.ToString();
                ListClients.NumberOfRecords = ListClients._clientes.Count;
                return JsonConvert.SerializeObject(ListClients);
            }
            private string ServerDataResponse(object client, Remitente remitente, bool success, string Error)
            {
                ServerDataResponse serverDataResponse = new();
                ServerData tempServerData = (ServerData)client;
                tempServerData.ClientCount = usersTable.Count + 1;
                serverDataResponse.ServerData = tempServerData;
                serverDataResponse.Succcess = success;
                serverDataResponse.Error = Error;
                serverDataResponse.ErrorDetail = " ";
                serverDataResponse.remitente = remitente.ToString();
                serverDataResponse.NumberOfRecords = 1;
                return JsonConvert.SerializeObject(serverDataResponse);

            }
            private string MessageResponse(object client, Remitente remitente, bool success, string Error, bool request = false)
            {
                var message = (MessageModel)client;
                MessageModel sendMessage = message;
                if (request)
                {
                    sendMessage.Message = "";
                }
                sendMessage.Succcess = success;
                sendMessage.Error = Error;
                sendMessage.ErrorDetail = "";
                sendMessage.remitente = remitente.ToString();
                sendMessage.NumberOfRecords = 1;
                return JsonConvert.SerializeObject(sendMessage);
            }
            private void PrintItInColor(string print, FontColors color)
            {
                switch (color)
                {
                    case FontColors.white:
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.WriteLine(print);
                        break;
                    case FontColors.cyan:
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine(print);
                        break;
                    case FontColors.blue:
                        Console.ForegroundColor = ConsoleColor.Blue;
                        Console.WriteLine(print);
                        break;
                    case FontColors.red:
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine(print);
                        break;
                    case FontColors.green:
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine(print);
                        break;
                    default:
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.WriteLine(print);
                        break;
                }
                Console.ForegroundColor = ConsoleColor.White;

            }
            private string SerachServerIPInternal()
            {
            IPHostEntry Host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var i in Host.AddressList)
                    {                    
                    var temp = i.ToString();
                    if (temp[0] == '1')
                    {
                        return temp;
                    }                   
                }
                return null;
            }
    }    
}
