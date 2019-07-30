using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Plugin_Naveego_Legacy.Plugin
{
    public class LegacyMetadata
    {
        [JsonProperty("flags")]
        public int Flags { get; set; }
        
        
        [JsonProperty("properties")]
        public LegacyProperty[] LegacyProperties { get; set; }
    }

    public class LegacyProperty
    {
        [JsonProperty("length")]
        public int Length { get; set; }
        
        [JsonProperty("type")]
        public string Type { get; set; }
        
        [JsonProperty("flags")]
        public string Flags { get; set; }
        
        [JsonProperty("id")]
        public string Id { get; set; }
        
        [JsonProperty("name")]
        public string Name { get; set; }
        
        [JsonProperty("sequenceNumber")]
        public int Sequence { get; set; }
        
        [JsonProperty("scale")]
        public int Scale { get; set; }
        
        [JsonProperty("precision")]
        public int Precision { get; set; }
    }
}