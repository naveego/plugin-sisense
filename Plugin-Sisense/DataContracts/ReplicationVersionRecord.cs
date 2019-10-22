using System.Collections.Generic;
using LiteDB;

namespace Plugin_Sisense.DataContracts
{
    public class ReplicationVersionRecord
    {
        [BsonId]
        public string VersionRecordId { get; set; }
        
        [BsonField]
        public string GoldenRecordId { get; set; }
        
        [BsonField]
        public Dictionary<string, object> Data { get; set; }
    }
}