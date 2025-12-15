using Newtonsoft.Json;
using System;

namespace SOMIOD.Models
{
    public class ContentInstance
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
        public string ResType { get; set; } = "content-instance";

        // Novos campos obrigatórios para dados
        [JsonProperty("content")]
        public string Content { get; set; }

        [JsonProperty("content-type")]
        public string ContentType { get; set; }

        public ContentInstance() { }
    }
}