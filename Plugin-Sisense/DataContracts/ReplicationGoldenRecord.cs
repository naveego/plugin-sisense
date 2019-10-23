using System.Collections.Generic;
using LiteDB;

namespace Plugin_Sisense.DataContracts
{
    public class ReplicationGoldenRecord
    {
        [BsonId]
        public string RecordId { get; set; }
        
        [BsonField]
        public List<string> VersionRecordIds { get; set; }
        
        [BsonField]
        public Dictionary<string, object> Data { get; set; }
    }
}