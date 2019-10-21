using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Plugin_Sisense.DataContracts
{
    public class TokenResponse
    {
        [JsonProperty("access_token")]
        public string AccessToken   { get; set; }
        
        [JsonProperty("message")]
        public string Message   { get; set; }
        
        [JsonProperty("success")]
        public bool Success   { get; set; }
        
        [JsonProperty("profile")]
        public JObject Profile   { get; set; }
        
        [JsonProperty("userId")]
        public string UserId   { get; set; }
    }
}