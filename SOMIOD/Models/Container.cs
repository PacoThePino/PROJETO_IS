using Newtonsoft.Json;
using System;

namespace SOMIOD.Models
{
    public class Container
    {
        [JsonIgnore]
        public int Id { get; set; }

        [JsonProperty("resource-name")]
        public string Name { get; set; }

        [JsonProperty("creation-datetime")]
        public DateTime CreationDate { get; set; }

        // O pai deste container (não aparece no JSON, serve para a BD)
        [JsonIgnore]
        public int ParentAppId { get; set; }

        [JsonProperty("res-type")]
        public string ResType { get; set; } = "container";

        public Container() { }
    }
}