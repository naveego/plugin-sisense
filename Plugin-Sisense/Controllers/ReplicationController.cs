using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LiteDB;
using Microsoft.AspNetCore.Mvc;
using Plugin_Sisense.DataContracts;

namespace Plugin_Sisense.Controllers
{
    [ApiController]
    [Route("v{version:apiVersion}/[controller]")]
    public class ReplicationController: ControllerBase
    {
        private readonly string _path = "localdb";

        [HttpGet]
        [Route("goldenrecords/{shapename}")]
        [ProducesResponseType(typeof(List<ReplicationGoldenRecord>), 200)]
        public List<ReplicationGoldenRecord> AllGoldenRecords([FromRoute] string shapename)
        {
            if (string.IsNullOrEmpty(shapename))
            {
                throw new Exception("shapename must be provided");
            }
            
            Directory.CreateDirectory(_path);
            
            using (var db = new LiteDatabase($"{_path}/SisenseReplication.db"))
            {
                var goldenRecords = db.GetCollection<ReplicationGoldenRecord>($"{shapename}_golden_records");

                var records = goldenRecords.FindAll();
                
                return records.ToList();
            }
        }
        
        [HttpGet]
        [Route("goldenrecords/{shapename}/{id}")]
        [ProducesResponseType(typeof(ReplicationGoldenRecord), 200)]
        public ReplicationGoldenRecord GoldenRecordById([FromRoute] string shapename, [FromRoute] string id)
        {
            if (string.IsNullOrEmpty(shapename))
            {
                throw new Exception("shapename must be provided");
            }
            
            Directory.CreateDirectory(_path);
            
            using (var db = new LiteDatabase($"{_path}/SisenseReplication.db"))
            {
                var goldenRecords = db.GetCollection<ReplicationGoldenRecord>($"{shapename}_golden_records");

                var record = goldenRecords.FindOne(r => r.RecordId == id);
                
                return record;
            }
        }
        
        [HttpGet]
        [Route("versions/{shapename}")]
        [ProducesResponseType(typeof(List<ReplicationVersionRecord>), 200)]
        public List<ReplicationVersionRecord> AllVersions([FromRoute] string shapename)
        {
            if (string.IsNullOrEmpty(shapename))
            {
                throw new Exception("shapename must be provided");
            }
            
            Directory.CreateDirectory(_path);
            
            using (var db = new LiteDatabase($"{_path}/SisenseReplication.db"))
            {
                var versions = db.GetCollection<ReplicationVersionRecord>($"{shapename}_versions");

                var records = versions.FindAll();
                
                return records.ToList();
            }
        }
        
        [HttpGet]
        [Route("versions/{shapename}/{id}")]
        [ProducesResponseType(typeof(ReplicationVersionRecord), 200)]
        public ReplicationVersionRecord VersionById([FromRoute] string shapename, [FromRoute] string id)
        {
            if (string.IsNullOrEmpty(shapename))
            {
                throw new Exception("shapename must be provided");
            }
            
            Directory.CreateDirectory(_path);
            
            using (var db = new LiteDatabase($"{_path}/SisenseReplication.db"))
            {
                var versions = db.GetCollection<ReplicationVersionRecord>($"{shapename}_versions");

                var record = versions.FindOne(r => r.VersionRecordId == id);
                
                return record;
            }
        }
    }
}