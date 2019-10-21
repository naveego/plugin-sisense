using Newtonsoft.Json;

namespace Plugin_Sisense.DataContracts
{
    public class ReadRecordObject
    {
        [JsonProperty("data")]
        public object Data { get; set; }
    }
}