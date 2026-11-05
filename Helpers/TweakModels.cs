
using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;

namespace HyperLisUI.Helpers
{
    public class TweakData
    {
        public Dictionary<string, Tweak> Tweaks { get; set; } = new();
        public Dictionary<string, List<string>> Presets { get; set; } = new();
    }

    public class Tweak
    {
        [JsonPropertyName("Content")]
        public string? Content { get; set; }
        [JsonPropertyName("Description")]
        public string? Description { get; set; }
        [JsonPropertyName("registry")]
        public List<RegistryEntry>? Registry { get; set; }
        [JsonPropertyName("service")]
        public List<ServiceEntry>? Service { get; set; }
        [JsonPropertyName("InvokeScript")]
        public List<string>? InvokeScript { get; set; }
    }

    public class RegistryEntry
    {
        [JsonPropertyName("Path")]
        public string? Path { get; set; }
        [JsonPropertyName("Name")]
        public string? Name { get; set; }
        [JsonPropertyName("Value")]
        public object? Value { get; set; }
        [JsonPropertyName("Type")]
        public string? Type { get; set; }
        [JsonPropertyName("OriginalValue")]
        public object? OriginalValue { get; set; }
    }

    public class ServiceEntry
    {
        [JsonPropertyName("Name")]
        public string? Name { get; set; }
        [JsonPropertyName("StartupType")]
        public string? StartupType { get; set; }
        [JsonPropertyName("OriginalType")]
        public string? OriginalType { get; set; }
    }
}
