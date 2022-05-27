using Newtonsoft.Json;

namespace Models
{
    public class ServerModels
    {

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
    public class ClientsSendListModel : GenericResponse
    {
        [JsonProperty(PropertyName = "Clients")]
        public List<ClientModel> _clientes { get; set; }
    }
    public class ClientModel
    {
        [JsonProperty(PropertyName = "ClientId")]
        public Guid ClientId { get; set; }

        [JsonProperty(PropertyName = "ClientName")]
        public string ClientName { get; set; }
    }
    public class ClientMessage : GenericResponse
    {
        [JsonProperty(PropertyName = "from")]
        public ClientModel clientFrom { get; set; }

        [JsonProperty(PropertyName = "to")]
        public ClientModel clientTo { get; set; }

        [JsonProperty(PropertyName = "Message")]
        public string Message { get; set; }
        public ClientMessage()
        {
            Message = "";

        }
    }
    public class GenericResponse
    {
        public bool Succcess { get; set; }
        public string Error { get; set; }
        public string ErrorDetail { get; set; }
        public int NumberOfRecords { get; set; }
        public string remitente { get; set; }
    }




}