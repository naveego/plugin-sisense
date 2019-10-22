using System.Collections.Generic;

namespace Plugin_Sisense.DataContracts
{
    public class SisenseConfig
    {
        public SisenseSettings Settings { get; set; }
        public SisenseCredentials Credentials { get; set; }
        public SisenseTables Tables { get; set; }
    }

    public class SisenseSettings
    {
        public string Provider { get; set; }
        public string ConnectorAssemblyFileName { get; set; }
        public string DisplayName { get; set; }
        public List<string> FileName { get; set; }
    }

    public class SisenseCredentials
    {
        
    }

    public class SisenseTables
    {
        
    }
}