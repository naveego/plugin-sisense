using System.Net;

namespace Plugin_Naveego_Legacy.DataContracts
{
    public class FormSettings
    {
        public string APIUrl { get; set; }
        
        public string ServerName { get; set; }
        
        public string Username { get; set; }
        public string Password { get; set; }
        
        public string ElastiCube { get; set; }

        public string EncodedElasticCube => WebUtility.UrlEncode(ElastiCube);
    }
}