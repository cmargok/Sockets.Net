using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Models;
using System.Net;
using System.Text.Json;
using System.Net.Sockets;
using System.Threading;

namespace ClientSocketS
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


    public class ClientSocket
    {
        private Socket clienteSocket;
        private Thread listenThread;
        private ServerData serverData;
        private GenerateClientModel ClientU;
        private List<ClientDataModel> ListConnectedClients;
        private CancellationTokenSource cts, sts, gts;
        private ClientDataModel UserPrivateChat;


        public ClientSocket(string nameClient)
        {
            try
            {
                Console.WriteLine("\n*************************************************\n            Configuring Client");
                IPHostEntry Host = Dns.GetHostEntry(Dns.GetHostName());
                IPAddress addr = Host.AddressList[Host.AddressList.Length - 1];
                IPEndPoint endPoint = new IPEndPoint(addr, 4404);
                clienteSocket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                ListConnectedClients = new List<ClientDataModel>();
                Console.WriteLine("         Successful configuration");
                StartClient(endPoint);
                Console.WriteLine(" \n    ---------------------------------------");
                Console.WriteLine("\n          Successful connection");
                Connect(nameClient);
            }
            catch (Exception ex)
            {
                Console.WriteLine("catch de creado de cliente");
                Console.WriteLine(ex.Message + "\nSOURCE\n" + ex.Source + "\nTARGET\n" + ex.TargetSite + "\nSTACKTRACE\n" + ex.StackTrace);
                clienteSocket.Close();
            }

        }

        public void StartClient(IPEndPoint endPoint)
        {
            Console.WriteLine("           Connenting To server");
            int connectAttemps = 1;
            do
            {
              try
                {                   
                    clienteSocket.Connect(endPoint);                   
                    for (int i = 0; i <= 100; i++)
                    {
                        Console.Write("\r                  {0}    ", i + "%");
                        Thread.Sleep(1);
                    }
                    connectAttemps = 10;

                }
                catch (Exception ex)
                {
                    ClearCurrentConsoleLineNotConexion(connectAttemps);
                    if (!clienteSocket.Connected) connectAttemps++;
                    else connectAttemps = 10;
                }
            } while (connectAttemps <10);
        }
        private void ClearCurrentConsoleLineNotConexion(int connectAttemps)
        {
            Console.Write("   Unnable to reach conexion to remote server...");
            Thread.Sleep(2000);
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write("       Retrying Connection... -> Attemp #" + connectAttemps);
            Thread.Sleep(2000);
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, Console.CursorTop);
        }

        private void Connect(string nameClient)
        {
            try
            {
                ClientU = new GenerateClientModel(nameClient);
                ClientU.status = true;
                string SendDataToServer = generateBytesToSend(ClientU, Remitente.SendingServerDat, true, "none");
                ListConnectedClients = null;
                clienteSocket.Send(Encoding.ASCII.GetBytes(SendDataToServer));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + "\nSOURCE\n" + ex.Source + "\nTARGET\n" + ex.TargetSite + "\nSTACKTRACE\n" + ex.StackTrace);
            }
            FirstConnection();
            Instructions();
            sts = new CancellationTokenSource();
            listenThread = new Thread(() => HearingServer(clienteSocket, sts));
             listenThread.Start();
             cts = new CancellationTokenSource();
             this.SendMessage(sts);   
        }

        public void FirstConnection()
        {
            try
            {
                byte[] bytesReceived = new Byte[2048];
                int res = clienteSocket.Receive(bytesReceived);
                string messageReceived = Encoding.ASCII.GetString(bytesReceived, 0, res);
                ServerDataResponse serverDataResponse = JsonConvert.DeserializeObject<ServerDataResponse>(messageReceived);
                serverData = serverDataResponse.ServerData;
                Console.WriteLine("         Server:  -> " + serverData.Name +
                                    "\nID: -> " + serverData.ServerId
                                    + "\n          Clients on chat -> "
                                    + serverData.ClientCount);

                Console.WriteLine("     ---------------------------------------");
                Console.WriteLine("Connected as ->    " + ClientU.ClientName);
                Console.WriteLine("Client -ID ->" + ClientU.ClientId
                                    + "\n *************************************************\n");
                
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + "\nSOURCE\n" + ex.Source + "\nTARGET\n" + ex.TargetSite + "\nSTACKTRACE\n" + ex.StackTrace);
            }
          
        }
        
       
        
        public void HearingServer(Object obj, CancellationTokenSource ctts)
        {
            Socket so = (Socket)obj;
            byte[] bytesReceivedFromServer = new Byte[2048];
            int dataFromServer;
            while (true)
            {
                try
                {                    
                    if (clienteSocket.Connected)
                    {
                        dataFromServer = clienteSocket.Receive(bytesReceivedFromServer);
                    }
                    else          
                    {
                        throw new ArgumentException("Lost connection");
                    }

                    string jsonReceived = Encoding.ASCII.GetString(bytesReceivedFromServer, 0, dataFromServer);
                    string typeClient = jsonReceived.Substring(jsonReceived.Length - 18, 16);
                    Remitente remitenteFromServer = Enum.Parse<Remitente>(typeClient);

                    switch (remitenteFromServer)
                    {

                        case Remitente.AllClientsOnline:
                            ListUsersModel allcLientsOnline = JsonConvert.DeserializeObject<ListUsersModel>(jsonReceived);                            
                            ListConnectedClients = allcLientsOnline._clientes;
                            Console.WriteLine("\n                                                            //--------------USERS ONLINE------------/");
                            foreach (var client in ListConnectedClients)
                            {
                                Console.WriteLine("                                                                       -->" + client.ClientName);
                            };
                            break;

                        case Remitente.NewClient_Linked:
                            ListUsersModel NewClientConnected = JsonConvert.DeserializeObject<ListUsersModel>(jsonReceived);
                            Console.WriteLine("\n                                                          --------------NEW USER CONNECTED----------|");
                            foreach (var client in NewClientConnected._clientes)
                            {
                                Console.WriteLine("                                                                 ->" + client.ClientName);
                             //   Console.WriteLine("                                                         ->" + client.ClientId);
                            };
                            break;

                        case Remitente.ClientConnClosed:
                            ListUsersModel ClientConnClosed = JsonConvert.DeserializeObject<ListUsersModel>(jsonReceived);
                            Console.WriteLine("\n                                                                    >-------USER DISCONNECTED-------->");
                            foreach (var client in ClientConnClosed._clientes)
                            {
                                Console.WriteLine("                                                                  >--->" + client.ClientName);
                            };
                            break;

                        case Remitente.GeneralSvMessage:
                            if (ClientU.status)
                            {
                                MessageModel messageReceived = JsonConvert.DeserializeObject<MessageModel>(jsonReceived);
                                Console.WriteLine("\n                      *-* FROM -> " + messageReceived.clientFrom.ClientName);
                                Console.WriteLine("                               - - > " + messageReceived.Message + "\n");
                            }                  
                            break;

                        case Remitente.PrivateUsMessage:
                            MessageModel privateMessage = JsonConvert.DeserializeObject<MessageModel>(jsonReceived);

                            if (privateMessage.Message.Length>1 && privateMessage.Message[0] == '*')
                            {
                                gts = new CancellationTokenSource();
                                cts.Cancel();
                                listenThread = new Thread(() => HearingServer(clienteSocket, gts));
                                listenThread.Start();

                            }

                            ClientU.status = privateMessage.clientTo.status;
                            UserPrivateChat = privateMessage.clientFrom;

                            if (privateMessage.Succcess)
                            {

                                Console.ForegroundColor = ConsoleColor.Cyan;
                                Console.WriteLine("\n                        *-* PRIVATE - FROM -> " + privateMessage.clientFrom.ClientName);
                                Console.WriteLine("                                 - - > " + privateMessage.Message + "\n");
                                Console.ForegroundColor = ConsoleColor.White;
                            }
                            else
                            {
                                Console.ForegroundColor = ConsoleColor.Cyan;
                                Console.WriteLine("                           - - > " + privateMessage.Message + "\n");
                                Console.ForegroundColor = ConsoleColor.White;

                            }
                            break;

                        case Remitente.requesttToServer:
                            MessageModel requestMessage = JsonConvert.DeserializeObject<MessageModel>(jsonReceived);
                            if (requestMessage.Succcess)
                            {
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine("Connetion Successfull");
                                Console.ForegroundColor = ConsoleColor.White;
                            }
                            else
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine("Connetion Failed");
                                Console.ForegroundColor = ConsoleColor.White;
                            }
                            break;

                        default: 
                            break;
                    }
                  
                }
                catch (ArgumentException argumentEx)
                {
                    Console.WriteLine("Desconexion exitosa");
                    break;
                }
                catch (System.Net.Sockets.SocketException SockEx)
                {
                    Console.WriteLine("Desconexion exitosa del servidor");
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message + "\nSOURCE\n" + ex.Source + "\nTARGET\n" + ex.TargetSite + "\nSTACKTRACE\n" + ex.StackTrace);
                    break;
                }
                finally
                {
                    cts.Cancel();
                }
            }        

        }      
           


        public void SendMessage(CancellationTokenSource ctts)
        {
            MessageModel _clientMessage = new MessageModel(); ;
            while (true)
            {
                try
                {
                    string messageFromConsole = "";
                    Thread.Sleep(1000);
                    if (ClientU.status == true)
                    {
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.WriteLine("New Message to general...");
                       messageFromConsole = Console.ReadLine();
                    }
                    else if (ClientU.status == false)
                    {
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine("New Message ...");
                        messageFromConsole = Console.ReadLine();
                    }
                     

                    if (ClientU.status == false)
                    {
                        if (messageFromConsole.Length > 1 && messageFromConsole.Substring(0, 2) == "-c")
                        {
                            if (UserPrivateChat != null) disconnectinPrivateChat(_clientMessage);

                            else Console.WriteLine("                  There's no private chat running....");                          

                        }
                        else if (messageFromConsole == "exit" || messageFromConsole == "EXIT" || messageFromConsole == "Exit")
                        {

                            _clientMessage.Message = "Close connection";
                            _clientMessage.clientFrom = new ClientDataModel { ClientId = ClientU.ClientId, ClientName = ClientU.ClientName };
                            var sendprueba = generateBytesToSend(_clientMessage, Remitente.ClientConnClosed, true, "none");
                            clienteSocket.Send(Encoding.ASCII.GetBytes(sendprueba));
                            clienteSocket.Shutdown(SocketShutdown.Both);
                            clienteSocket.Close();
                            break;
                        }

                        else
                        {
                            _clientMessage.Message = messageFromConsole;
                            _clientMessage.clientFrom = new ClientDataModel { ClientId = ClientU.ClientId, ClientName = ClientU.ClientName, status = ClientU.status };
                            _clientMessage.clientTo = UserPrivateChat;
                            var sendMessage = generateBytesToSend(_clientMessage, Remitente.PrivateUsMessage, true, "none");
                            clienteSocket.Send(Encoding.ASCII.GetBytes(sendMessage));
                            ClientU.status = false;
                        }

                        
                    }
                    else if (ClientU.status == true)
                    {

                        if (messageFromConsole.Length > 0 && messageFromConsole[0] == '-')
                        {

                            if (messageFromConsole.Length > 4 && messageFromConsole.Substring(0, 4) == "-n -" && ClientU.status == true)
                            {

                                var newPrivateChat = messageFromConsole.Split('-');
                                newPrivateChat[2] = newPrivateChat[2].ToUpper();
                                if(newPrivateChat[2]== ClientU.ClientName) UserPrivateChat = null;                                
                                else
                                {
                                    Console.WriteLine("\nTrying connection with -> " + newPrivateChat[2]);
                                    try
                                    {
                                        UserPrivateChat = ListConnectedClients.Find(z => z.ClientName == newPrivateChat[2]);
                                        Console.WriteLine("Waiting for server response...");
                                    }
                                    catch (Exception ex)
                                    {
                                        UserPrivateChat = null;
                                    }
                                }
                                

                                if (UserPrivateChat != null)
                                {
                                    _clientMessage.Message = "*new private chat initilized with you\n                          Press Enter to in chat";
                                    _clientMessage.clientFrom = new ClientDataModel { ClientId = ClientU.ClientId, ClientName = ClientU.ClientName, status = false };
                                    _clientMessage.clientTo = UserPrivateChat;
                                    var sendMessage = generateBytesToSend(_clientMessage, Remitente.PrivateUsMessage, true, "none");
                                    clienteSocket.Send(Encoding.ASCII.GetBytes(sendMessage));
                                    ClientU.status = false;                                    
                                }
                                else
                                {
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.WriteLine("User not Connected or not Found, try again...\n");
                                    Console.ForegroundColor = ConsoleColor.White;
                                }


                            }

                        }
                        else if (messageFromConsole == "exit" || messageFromConsole == "EXIT" || messageFromConsole == "Exit")
                        {
                            _clientMessage.Message = "Close connection";
                            _clientMessage.clientFrom = new ClientDataModel { ClientId = ClientU.ClientId, ClientName = ClientU.ClientName };
                            var sendprueba = generateBytesToSend(_clientMessage, Remitente.ClientConnClosed, true, "none");
                            clienteSocket.Send(Encoding.ASCII.GetBytes(sendprueba));
                            clienteSocket.Shutdown(SocketShutdown.Both);
                            clienteSocket.Close();
                            break;
                        }
                        else
                        {
                            _clientMessage.Message = messageFromConsole;
                            _clientMessage.clientFrom = new ClientDataModel { ClientId = ClientU.ClientId, ClientName = ClientU.ClientName };
                            var sendprueba = generateBytesToSend(_clientMessage, Remitente.GeneralSvMessage, true, "none");
                            clienteSocket.Send(Encoding.ASCII.GetBytes(sendprueba));
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message + "\nSTACKTRACE\n" + ex.StackTrace);
                }
                
            }
            sts.Cancel();
            Thread.Sleep(1000);       

        }

        private string generateBytesToSend(object client, Remitente remitente, bool success, string Error, Socket to = null)
        {
            ListUsersModel SendModelClientList;

            try
            {
                switch (remitente)
                {

                    //recibiendo informacion del cliente para configurar primera conexion
                    case Remitente.SendingServerDat:
                        List<ClientDataModel> tempClientList = new List<ClientDataModel>();
                        var castingTocClientModel = (GenerateClientModel)client;                        
                        ClientDataModel clientTosend = new ClientDataModel 
                                                                         { 
                                                                            ClientId = castingTocClientModel.ClientId, 
                                                                            ClientName = castingTocClientModel.ClientName, 
                                                                            status=ClientU.status 
                                                                         };
                        tempClientList.Add(clientTosend);
                        SendModelClientList = new ListUsersModel();
                        SendModelClientList._clientes = tempClientList;
                        SendModelClientList.Succcess = true;
                        SendModelClientList.Error = "none";
                        SendModelClientList.remitente = Remitente.SendingServerDat.ToString();
                        SendModelClientList.ErrorDetail = "";
                        SendModelClientList.NumberOfRecords = SendModelClientList._clientes.Count;
                        return JsonConvert.SerializeObject(SendModelClientList);


                    //enviando lista de clientes conectados a todos los clientes conectados
                    case Remitente.AllClientsOnline:
                        SendModelClientList = new ListUsersModel();
                        List<ClientDataModel> tempAllClientOnlineList = (List<ClientDataModel>)client;
                        SendModelClientList._clientes = tempAllClientOnlineList;
                        SendModelClientList.Succcess = success;
                        SendModelClientList.Error = Error;
                        SendModelClientList.ErrorDetail = "";
                        SendModelClientList.remitente = remitente.ToString();
                        SendModelClientList.NumberOfRecords = SendModelClientList._clientes.Count;
                        return JsonConvert.SerializeObject(SendModelClientList);


                    //Definiendo que hacer cuando llega un nuevo cliente
                    // enviando la notificacion del nuevo cliente a los clientes que estan conectados
                    case Remitente.NewClient_Linked:
                        SendModelClientList = new ListUsersModel();
                        List<ClientDataModel> tempoUniclient = new List<ClientDataModel>();
                        var tempClientLists = (List<ClientDataModel>)client;
                        SendModelClientList._clientes = tempClientLists;
                        SendModelClientList.Succcess = success;
                        SendModelClientList.Error = Error;
                        SendModelClientList.ErrorDetail = "";
                        SendModelClientList.remitente = remitente.ToString();
                        SendModelClientList.NumberOfRecords = SendModelClientList._clientes.Count;
                        return JsonConvert.SerializeObject(SendModelClientList);

                     case Remitente.GeneralSvMessage:
                        var tempMessage = (MessageModel)client;
                        tempMessage.clientTo = new ClientDataModel 
                                                                { 
                                                                    ClientId= serverData.ServerId, 
                                                                    ClientName= serverData.Name
                                                                }; 
                         tempMessage.Succcess = success;
                         tempMessage.Error = Error;
                         tempMessage.ErrorDetail = "";
                         tempMessage.remitente = remitente.ToString();
                         tempMessage.NumberOfRecords = 1;
                         return JsonConvert.SerializeObject(tempMessage);
               
                    case Remitente.ClientConnClosed:
                        var CloseConnection= (MessageModel)client;
                        CloseConnection.clientTo = new ClientDataModel { ClientId = new Guid(), ClientName = "Client-Closed" };
                        CloseConnection.Succcess = success;
                        CloseConnection.Error = Error;
                        CloseConnection.ErrorDetail = "Client has closed the connection to server";
                        CloseConnection.remitente = remitente.ToString();
                        CloseConnection.NumberOfRecords = 1;
                        return JsonConvert.SerializeObject(CloseConnection);

                    case Remitente.PrivateUsMessage:
                        var tempPrivateMessage = (MessageModel)client;
                        tempPrivateMessage.Succcess = success;
                        tempPrivateMessage.Error = Error;
                        tempPrivateMessage.ErrorDetail = "";
                        tempPrivateMessage.remitente = remitente.ToString();
                        tempPrivateMessage.NumberOfRecords = 1;
                        return JsonConvert.SerializeObject(tempPrivateMessage);
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



        private void disconnectinPrivateChat(MessageModel message)
        {
            Console.WriteLine("Disconnecting From private chat ");
            message = new MessageModel();
            message.Message = "?|"+ClientU.ClientName + "|-c |It's Free to talk in the Public chat";
            ClientU.status = true;
            message.clientFrom = new ClientDataModel { ClientId = ClientU.ClientId, ClientName = ClientU.ClientName, status = ClientU.status };
            message.clientTo = UserPrivateChat;
            var sendMessage = generateBytesToSend(message, Remitente.PrivateUsMessage, true, "none");
            clienteSocket.Send(Encoding.ASCII.GetBytes(sendMessage));
            UserPrivateChat = new ClientDataModel { ClientId = serverData.ServerId, ClientName = serverData.Name };
        }
        private void Instructions()
        {
            Console.WriteLine("*******************************************************\n" +
                "                     INSTRUCTIONS\n" +
                "*******************************************************\n" +
                "-n -username    // to init a private conversation\n" +
                "-c              // to close the actual private chat\n" +
                "exit            // to close sesion\n" +
                "*********************************************************** \n" +
                "------------------------------------------------------------------------------------------------\n");

        }

    }
}
