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
        Incoming_Message,
        SendingServerDat,
        ClientConnClosed,
    }

    public class ClientSocket
    {
        private Socket clienteSocket;
        private Thread listenThread;
        private ServerData serverData;
        private GenerateClientModel ClientU;
        CancellationTokenSource cts;

        public ClientSocket(string nameClient)
        {
            try
            {
                Console.WriteLine("\n*************************************************\n            Configuring Client");              
                IPHostEntry host = Dns.GetHostEntry("localhost");
                IPAddress addr = host.AddressList[0];
                IPEndPoint endPoint = new IPEndPoint(addr, 4404);
                clienteSocket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                Console.WriteLine("         Successful configuration");
                StartClient(endPoint);
                Console.WriteLine("     ---------------------------------------");
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
                }
                catch (Exception ex)
                {
                    Console.WriteLine();
                    Console.Write("Unnable to reach conexion to remote server...");            
                    ClearCurrentConsoleLine(500);
                    Console.Write("Retrying Connection... -> Attemp #"+connectAttemps );
                    
                    ClearCurrentConsoleLine(500);
                    if (!clienteSocket.Connected) connectAttemps++;
                    else connectAttemps = 10;
                }
            } while (connectAttemps <10);
            ClearCurrentConsoleLine(10);    
        }   


        private void Connect(string nameClient)
        {
            try
            {
                ClientU = new GenerateClientModel(nameClient);
                string SendDataToServer = generateBytesToSend(ClientU, Remitente.SendingServerDat, true, "none");
                clienteSocket.Send(Encoding.ASCII.GetBytes(SendDataToServer));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + "\nSOURCE\n" + ex.Source + "\nTARGET\n" + ex.TargetSite + "\nSTACKTRACE\n" + ex.StackTrace);
            }
            FirstConnection();          
             listenThread = new Thread(() => HearingServer(clienteSocket, cts));
             listenThread.Start();
             cts = new CancellationTokenSource();
             this.SendMessage(cts);   
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
                        throw new ArgumentException("Conexxion Perdida");
                    }

                    string jsonReceived = Encoding.ASCII.GetString(bytesReceivedFromServer, 0, dataFromServer);
                    string typeClient = jsonReceived.Substring(jsonReceived.Length - 18, 16);
                    Remitente remitenteFromServer = Enum.Parse<Remitente>(typeClient);

                    switch (remitenteFromServer)
                    {
                        case Remitente.AllClientsOnline:
                            ClientsSendListModel allcLientsOnline = JsonConvert.DeserializeObject<ClientsSendListModel>(jsonReceived);
                            Console.WriteLine("\n                                                            //--------------USERS ONLINE------------/");
                            foreach (var client in allcLientsOnline._clientes)
                            {
                                Console.WriteLine("                                                                       -->" + client.ClientName);
                            };
                            break;
                        case Remitente.NewClient_Linked:
                            ClientsSendListModel NewClientConnected = JsonConvert.DeserializeObject<ClientsSendListModel>(jsonReceived);
                            Console.WriteLine("\n                                                          --------------NEW USER CONNECTED----------|");
                            foreach (var client in NewClientConnected._clientes)
                            {
                                Console.WriteLine("                                                                 ->" + client.ClientName);
                             //   Console.WriteLine("                                                         ->" + client.ClientId);
                            };
                            break;
                        case Remitente.ClientConnClosed:
                            ClientsSendListModel ClientConnClosed = JsonConvert.DeserializeObject<ClientsSendListModel>(jsonReceived);
                            Console.WriteLine("\n                                                                    >-------USER DISCONNECTED-------->");
                            foreach (var client in ClientConnClosed._clientes)
                            {
                                Console.WriteLine("                                                                  >--->" + client.ClientName);
                            };
                            break;
                        case Remitente.Incoming_Message:
                            ClientMessage messageReceived = JsonConvert.DeserializeObject<ClientMessage>(jsonReceived);
                            Console.WriteLine("\n                        *-* FROM -> " + messageReceived.clientFrom.ClientName);
                            Console.WriteLine("                                 - - > " + messageReceived.Message+"\n");

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
                    Console.WriteLine("Desconexion exitosa");
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
            ClientMessage _clientMessage;
            while (true)
            {
                try
                {
                    Thread.Sleep(1000);
                    Console.WriteLine("Nuevo mensaje...");
                    var meme = Console.ReadLine();
                    if (meme == "exit" || meme == "EXIT" || meme == "Exit")
                    {
                        _clientMessage = new ClientMessage();
                        _clientMessage.Message = "Close connection";
                        _clientMessage.clientFrom = new ClientModel { ClientId = ClientU.ClientId, ClientName = ClientU.ClientName };
                        var sendprueba = generateBytesToSend(_clientMessage, Remitente.ClientConnClosed, true, "none");
                        clienteSocket.Send(Encoding.ASCII.GetBytes(sendprueba));
                        clienteSocket.Shutdown(SocketShutdown.Both);
                        clienteSocket.Close();
                        break;
                    }
                    else
                    {
                        _clientMessage = new ClientMessage();
                        _clientMessage.Message = meme;
                        _clientMessage.clientFrom = new ClientModel { ClientId = ClientU.ClientId, ClientName = ClientU.ClientName };
                        var sendprueba = generateBytesToSend(_clientMessage, Remitente.Incoming_Message, true, "none");
                        clienteSocket.Send(Encoding.ASCII.GetBytes(sendprueba));
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message + "\nSOURCE\n" + ex.Source + "\nTARGET\n" + ex.TargetSite + "\nSTACKTRACE\n" + ex.StackTrace);
                }
                
            }
            cts.Cancel();
            Thread.Sleep(1000);       

        }

        private string generateBytesToSend(object client, Remitente remitente, bool success, string Error, Socket to = null)
        {
            ClientsSendListModel SendModelClientList;

            try
            {
                switch (remitente)
                {

                    //recibiendo informacion del cliente para configurar primera conexion
                    case Remitente.SendingServerDat:
                        List<ClientModel> tempClientList = new List<ClientModel>();
                        var castingTocClientModel = (GenerateClientModel)client;
                        ClientModel clientTosend = new ClientModel { ClientId = castingTocClientModel.ClientId, ClientName = castingTocClientModel.ClientName };
                        tempClientList.Add(clientTosend);
                        SendModelClientList = new ClientsSendListModel();
                        SendModelClientList._clientes = tempClientList;
                        SendModelClientList.Succcess = true;
                        SendModelClientList.Error = "none";
                        SendModelClientList.remitente = Remitente.SendingServerDat.ToString();
                        SendModelClientList.ErrorDetail = "";
                        SendModelClientList.NumberOfRecords = SendModelClientList._clientes.Count;
                        return JsonConvert.SerializeObject(SendModelClientList);


                    //enviando lista de clientes conectados a todos los clientes conectados
                    case Remitente.AllClientsOnline:
                        SendModelClientList = new ClientsSendListModel();
                        List<ClientModel> tempAllClientOnlineList = (List<ClientModel>)client;
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
                        SendModelClientList = new ClientsSendListModel();
                        List<ClientModel> tempoUniclient = new List<ClientModel>();
                        var tempClientLists = (List<ClientModel>)client;
                        SendModelClientList._clientes = tempClientLists;
                        SendModelClientList.Succcess = success;
                        SendModelClientList.Error = Error;
                        SendModelClientList.ErrorDetail = "";
                        SendModelClientList.remitente = remitente.ToString();
                        SendModelClientList.NumberOfRecords = SendModelClientList._clientes.Count;
                        return JsonConvert.SerializeObject(SendModelClientList);

                     case Remitente.Incoming_Message:
                         var tempMessage = (ClientMessage)client;
                        tempMessage.clientTo = new ClientModel { ClientId= serverData.ServerId, ClientName= serverData.Name}; 
                         tempMessage.Succcess = success;
                        tempMessage.Error = Error;
                        tempMessage.ErrorDetail = "";
                        tempMessage.remitente = remitente.ToString();
                        tempMessage.NumberOfRecords = 1;
                         return JsonConvert.SerializeObject(tempMessage);
               
                    case Remitente.ClientConnClosed:
                        var CloseConnection= (ClientMessage)client;
                        CloseConnection.clientTo = new ClientModel { ClientId = new Guid(), ClientName = "Client-Closed" };
                        CloseConnection.Succcess = success;
                        CloseConnection.Error = Error;
                        CloseConnection.ErrorDetail = "Client has closed the connection to server";
                        CloseConnection.remitente = remitente.ToString();
                        CloseConnection.NumberOfRecords = 1;
                        return JsonConvert.SerializeObject(CloseConnection);
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
        public void ClearCurrentConsoleLine(int time)
        {
            Thread.Sleep(time);
            int currentLineCursor = Console.CursorTop;
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, currentLineCursor);
        }
    }
}
