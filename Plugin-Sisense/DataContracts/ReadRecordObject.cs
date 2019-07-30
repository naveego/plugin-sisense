using Newtonsoft.Json;

namespace Plugin_Naveego_Legacy.DataContracts
{
    public class ReadRecordObject
    {
        [JsonProperty("data")]
        public object Data { get; set; }
    }
}