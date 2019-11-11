using System.Collections.Generic;
using Newtonsoft.Json;

namespace Plugin_Sisense.API.Replication
{
    public class ReadResult
    {
        [JsonProperty("data")]
        public List<Dictionary<string,object>> Data { get; set; }
    }
}