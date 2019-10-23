using System.Collections.Generic;

namespace Plugin_Sisense.DataContracts
{
    public class SisenseConfig
    {
        public SisenseSettings Settings { get; set; }
//        public SisenseCredentials Credentials { get; set; }
        public List<SisenseTable> Tables { get; set; }
    }

    public class SisenseSettings
    {
        public string Provider { get; set; }
        public string DisplayName { get; set; }
        public string ConnectorAssemblyFileName { get; set; }
        public List<string> FileList { get; set; }
        public int MaxDocs { get; set; }
        public int FetchSize { get; set; }
    }

    public class SisenseCredentials
    {
        
    }

    public class SisenseTable
    {
        public string Name { get; set; }
        public string Schema { get; set; }
        public string Method { get; set; }
        public string Base { get; set; }
        public string Path { get; set; }
        public Dictionary<string, string> Headers { get; set; }
        public List<string> PathParameters { get; set; }
        public string DataPath { get; set; }
    }
}