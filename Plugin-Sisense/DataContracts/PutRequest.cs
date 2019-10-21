using System.Collections.Generic;

namespace Plugin_Sisense.DataContracts
{
    public class PutRequest
    {
        public object[] data { get; set; }
        public List<string> trigger { get; set; }
    }
}