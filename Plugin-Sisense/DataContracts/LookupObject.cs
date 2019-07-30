using Newtonsoft.Json;

namespace Plugin_Naveego_Legacy.DataContracts
{
    public class LookupObject
    {
        [JsonProperty("id")]
        public string Id { get; set; }
    }
}