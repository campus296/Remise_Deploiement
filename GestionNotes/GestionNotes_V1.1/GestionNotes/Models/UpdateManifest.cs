using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace GestionNotes.Models
{
    public class UpdateManifest
    {
        [JsonPropertyName("version")]
        public string Version { get; set; } = "";

        [JsonPropertyName("fichier")]
        public string Fichier { get; set; } = "";
    }
}
