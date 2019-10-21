using Newtonsoft.Json;

namespace Plugin_Sisense.DataContracts
{
    public class LookupObject
    {
        [JsonProperty("id")]
        public string Id { get; set; }
    }
}