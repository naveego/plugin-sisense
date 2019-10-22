using System;
using System.Collections.Generic;
using System.IO;
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
                var path = "localdb";
                Directory.CreateDirectory(path);
                
                using (var db = new LiteDatabase($"{path}/SisenseGoldenRecordReplication.db"))
                {
                    var goldenRecords = db.GetCollection<ReplicationRecord>("records");

                    if (record.Action == Record.Types.Action.Delete)
                    {
                        goldenRecords.Delete(record.RecordId);
                    }
                    else
                    {
                        var replicationRecord = new ReplicationRecord
                        {
                            Id = record.RecordId,
                            Data = JsonConvert.DeserializeObject<Dictionary<string, object>>(record.DataJson)
                        };

                        goldenRecords.Upsert(replicationRecord);
                    }
                }
                
                using (var db = new LiteDatabase($"{path}/SisenseVersionReplication.db"))
                {
                    var versions = db.GetCollection<ReplicationRecord>("records");
                    
                    foreach (var version in record.Versions)
                    {
                        if (record.Action == Record.Types.Action.Delete)
                        {
                            versions.Delete(version.RecordId);
                        }
                        else
                        {
                            var versionRecord = new ReplicationRecord
                            {
                                Id = version.RecordId,
                                Data = JsonConvert.DeserializeObject<Dictionary<string, object>>(version.DataJson)
                            };

                            versions.Upsert(versionRecord);
                        }
                    }
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