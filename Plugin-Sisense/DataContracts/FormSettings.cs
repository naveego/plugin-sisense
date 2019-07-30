namespace Plugin_Naveego_Legacy.DataContracts
{
    public class FormSettings
    {
        public string OAuthClientId { get; set; }
        
        public string OAuthClientSecret { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        
        public string ConvertNullToZero { get; set; }

    }
}