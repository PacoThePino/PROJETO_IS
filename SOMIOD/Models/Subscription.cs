using Newtonsoft.Json;
using System;

namespace SOMIOD.Models
{
    public class Subscription
    {
        [JsonIgnore]
        public int Id { get; set; }

        [JsonProperty("resource-name")]
        public string Name { get; set; }

        [JsonProperty("creation-datetime")]
        public DateTime CreationDate { get; set; }

        [JsonIgnore]
        public int ParentContainerId { get; set; }

        [JsonProperty("res-type")]
        public string ResType { get; set; } = "subscription";

        // Campos específicos da subscrição
        [JsonProperty("endpoint")]
        public string Endpoint { get; set; } // Onde vamos mandar o aviso (MQTT ou URL)

        [JsonProperty("evt")]
        public string Event { get; set; } // "1" (Criação) ou "2" (Apagar)

        public Subscription() { }
    }
}