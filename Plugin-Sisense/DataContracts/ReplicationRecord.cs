using System.Collections.Generic;
using LiteDB;

namespace Plugin_Sisense.DataContracts
{
    public class ReplicationRecord
    {
        [BsonId]
        public string Id { get; set; }
        
        [BsonField]
        public Dictionary<string, object> Data { get; set; }
    }
}