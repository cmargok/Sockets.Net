using Newtonsoft.Json;

namespace Models
{
    public class Modelos
    {

    }
    public class GenerateClientModel
    {
        public Guid ClientId { get; set; }
        public string ClientName { get; set; }

        public bool status { get; set; }

        public GenerateClientModel(string name)
        {
            ClientName = name.ToUpper();
            ClientId = Guid.NewGuid();
        }
    }
    
    public class ListUsersModel : GenericResponse
    {
        [JsonProperty(PropertyName = "Clients")]
        public List<ClientDataModel> _clientes { get; set; } 
    }
    public class ClientDataModel
    {
        [JsonProperty(PropertyName = "ClientId")]
        public Guid ClientId { get; set; }

        [JsonProperty(PropertyName = "ClientName")]
        public string ClientName { get; set; }

        [JsonProperty(PropertyName = "status")]
        public bool status { get; set; }

    }


    public class MessageModel : GenericResponse
    {
        [JsonProperty(PropertyName = "from")]
        public ClientDataModel clientFrom { get; set; }

        [JsonProperty(PropertyName = "to")]
        public ClientDataModel clientTo { get; set; }

        [JsonProperty(PropertyName = "Message")]
        public string Message { get; set; }
        public MessageModel()
        {
            Message = "";

        }
    }

    //respuseta generica
    public class GenericResponse
    {
        public bool Succcess { get; set; }
        public string Error { get; set; }
        public string ErrorDetail { get; set; }
        public int NumberOfRecords { get; set; }
        public string remitente { get; set; }
    }
    public class ServerData 
    {
        public string Name { get; set; }
        public Guid ServerId { get; set; }
        public int ClientCount { get; set; }
    }
    public class ServerDataResponse : GenericResponse
    {
        public ServerData ServerData { get; set; }
    }
}