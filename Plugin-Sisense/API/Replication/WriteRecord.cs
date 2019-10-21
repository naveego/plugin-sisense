using System;
using System.Collections.Generic;
using LiteDB;
using Newtonsoft.Json;
using Plugin_Sisense.DataContracts;
using Pub;
using Logger = Plugin_Sisense.Helper.Logger;

namespace Plugin_Sisense.API.Replication
{
    public static partial class Replication
    {
        public static string WriteRecord(Record record)
        {
            try
            {
                using (var db = new LiteDatabase(@"SisenseReplication.db"))
                {
                    var records = db.GetCollection<ReplicationRecord>("records");

                    var replicationRecord = new ReplicationRecord
                    {
                        Id = record.RecordId,
                        Data = JsonConvert.DeserializeObject<Dictionary<string, object>>(record.DataJson)
                    };

                    records.Upsert(replicationRecord);
                }

                return "";
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
                throw;
            }
        }
    }
}