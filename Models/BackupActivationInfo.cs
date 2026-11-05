using System;
using System.Text.Json.Serialization;

namespace HyperLisUI.Models
{
    public class BackupActivationInfo
    {
        [JsonPropertyName("ProductName")]
        public string ProductName { get; set; }

        [JsonPropertyName("Time")]
        public string Time { get; set; } // Can store ToFileTimeUtc().ToString("X") or ISO 8601 string

        [JsonPropertyName("Description")]
        public string Description { get; set; }

        [JsonPropertyName("EditionID")]
        public string EditionID { get; set; }

        [JsonPropertyName("CurrentVersion")]
        public string CurrentVersion { get; set; }

        [JsonPropertyName("DigitalProductId")]
        public string DigitalProductId { get; set; } // Hex string representation of byte array

        [JsonPropertyName("DigitalProductId4")]
        public string DigitalProductId4 { get; set; } // Hex string representation of byte array

        [JsonPropertyName("ProductId")]
        public string ProductId { get; set; }
    }
}
