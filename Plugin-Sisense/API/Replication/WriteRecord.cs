using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LiteDB;
using Newtonsoft.Json;
using Plugin_Sisense.DataContracts;
using Pub;
using Logger = Plugin_Sisense.Helper.Logger;

namespace Plugin_Sisense.API.Replication
{
    public static partial class Replication
    {
        public static string WriteRecord(Record record, ConfigureReplicationFormData config)
        {
            try
            {
                var path = "localdb";
                Directory.CreateDirectory(path);

                var recordData = JsonConvert.DeserializeObject<Dictionary<string, object>>(record.DataJson);

                using (var db = new LiteDatabase($"{path}/SisenseReplication.db"))
                {
                    var goldenRecords = db.GetCollection<ReplicationGoldenRecord>($"{config.ShapeName}_golden_records");
                    var versions = db.GetCollection<ReplicationVersionRecord>($"{config.ShapeName}_versions");

                    // get and check previous record
                    var previousRecord = goldenRecords.FindOne(r => r.RecordId == record.RecordId);
                    if (previousRecord == null)
                    {
                        // set previous record to current record
                        previousRecord = new ReplicationGoldenRecord
                        {
                            RecordId = record.RecordId,
                            VersionRecordIds = record.Versions.Select(v => v.RecordId).ToList(),
                            Data = recordData
                        };
                    }
                    
                    if (recordData.Count == 0)
                    {
                        // delete everything for this record
                        Logger.Info($"shapeId: {config.ShapeName} recordId: {record.RecordId} - DELETE");
                        goldenRecords.Delete(r => r.RecordId == record.RecordId);
                        
                        foreach (var versionId in previousRecord.VersionRecordIds)
                        {
                            Logger.Info($"shapeId: {config.ShapeName} recordId: {record.RecordId} | versionId: {versionId} - DELETE");
                            versions.Delete(r => r.VersionRecordId == versionId);
                        }
                    }
                    else
                    {
                        // update record and remove/add versions
                        Logger.Info($"shapeId: {config.ShapeName} recordId: {record.RecordId} - UPSERT");
                        var replicationRecord = new ReplicationGoldenRecord
                        {
                            RecordId = record.RecordId,
                            VersionRecordIds = record.Versions.Select(v => v.RecordId).ToList(),
                            Data = recordData
                        };

                        goldenRecords.Upsert(replicationRecord);
                        
                        // delete missing versions
                        var missingVersions = previousRecord.VersionRecordIds.Except(replicationRecord.VersionRecordIds);
                        foreach (var versionId in missingVersions)
                        {
                            Logger.Info($"shapeId: {config.ShapeName} recordId: {record.RecordId} | versionId: {versionId} - DELETE");
                            versions.Delete(r => r.VersionRecordId == versionId);
                        }
                        
                        // upsert other versions
                        foreach (var version in record.Versions)
                        {
                            Logger.Info($"shapeId: {config.ShapeName} recordId: {record.RecordId} | versionId: {version.RecordId} - UPSERT");
                            var versionRecord = new ReplicationVersionRecord
                            {
                                VersionRecordId =  version.RecordId,
                                GoldenRecordId = record.RecordId,
                                Data = JsonConvert.DeserializeObject<Dictionary<string, object>>(version.DataJson)
                            };

                            versions.Upsert(versionRecord);
                        }
                    }
                    
                    // update shapes
                    var shapes = db.GetCollection<ShapeNameObject>("shapes");

                    var existingShape = shapes.FindOne(s => s.ShapeName == config.ShapeName);

                    if (existingShape == null)
                    {
                        var shapeNameObject = new ShapeNameObject
                        {
                            ShapeName = config.ShapeName
                        };
                        shapes.Upsert(shapeNameObject);

                        // update the Sisense service
                        var sisenseConfig = GenerateSisenseConfig();
                        RestartSisenseService(sisenseConfig);
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