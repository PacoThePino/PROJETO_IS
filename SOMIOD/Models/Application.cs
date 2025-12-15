using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Newtonsoft.Json;

namespace SOMIOD.Models
{
    public class Application
    {
        // 1. ID Interno (Escondido do JSON)
        [JsonIgnore]
        public int Id { get; set; }

        // 2. Nome do Recurso (Mapeado para 'resource-name')
        [JsonProperty("resource-name")]
        public string Name { get; set; }

        // 3. Data de Criação (Mapeado para 'creation-datetime')
        [JsonProperty("creation-datetime")]
        public DateTime CreationDate { get; set; }

        // 4. Tipo de Recurso (Sempre 'application')
        [JsonProperty("res-type")]
        public string ResType { get; set; } = "application";

        // Construtor vazio
        public Application() { }
    }
}