namespace Plugin_Sisense.Helper
{
    public class ServerStatus
    {
        public Settings Settings { get; set; }
        public bool Connected { get; set; }
        public bool WriteConfigured { get; set; }
        public WriteSettings WriteSettings { get; set; }
    }
}