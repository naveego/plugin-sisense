using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LiteDB;
using Microsoft.AspNetCore.Mvc;
using Plugin_Sisense.API.Replication;
using Plugin_Sisense.DataContracts;

namespace Plugin_Sisense.Controllers
{
    [ApiController]
    [Route("v{version:apiVersion}/[controller]")]
    public class ReplicationController: ControllerBase
    {
        private readonly string _path = Replication.Path;

        [HttpGet]
        [Route("goldenrecords/{shapename}")]
        [ProducesResponseType(typeof(List<Dictionary<string,object>>), 200)]
        public List<Dictionary<string,object>> AllGoldenRecords([FromRoute] string shapename)
        {
            if (string.IsNullOrEmpty(shapename))
            {
                throw new Exception("shapename must be provided");
            }
            
            var safeShapeName = string.Concat(shapename.Where(c => !char.IsWhiteSpace(c)));
            
            Directory.CreateDirectory(_path);
            
            using (var db = new LiteDatabase($"{_path}/SisenseReplication.db"))
            {
                var goldenRecords = db.GetCollection<ReplicationGoldenRecord>($"{safeShapeName}_golden_records");

                var records = goldenRecords.FindAll();
                
                return records.Select(r => r.Data).ToList();
            }
        }
        
        [HttpGet]
        [Route("goldenrecords/{shapename}/{id}")]
        [ProducesResponseType(typeof(Dictionary<string,object>), 200)]
        public Dictionary<string,object> GoldenRecordById([FromRoute] string shapename, [FromRoute] string id)
        {
            if (string.IsNullOrEmpty(shapename))
            {
                throw new Exception("shapename must be provided");
            }
            
            var safeShapeName = string.Concat(shapename.Where(c => !char.IsWhiteSpace(c)));
            
            Directory.CreateDirectory(_path);
            
            using (var db = new LiteDatabase($"{_path}/SisenseReplication.db"))
            {
                var goldenRecords = db.GetCollection<ReplicationGoldenRecord>($"{safeShapeName}_golden_records");

                var record = goldenRecords.FindOne(r => r.RecordId == id);
                
                return record.Data;
            }
        }
        
        [HttpGet]
        [Route("versions/{shapename}")]
        [ProducesResponseType(typeof(List<Dictionary<string,object>>), 200)]
        public List<Dictionary<string,object>> AllVersions([FromRoute] string shapename)
        {
            if (string.IsNullOrEmpty(shapename))
            {
                throw new Exception("shapename must be provided");
            }
            
            var safeShapeName = string.Concat(shapename.Where(c => !char.IsWhiteSpace(c)));
            
            Directory.CreateDirectory(_path);
            
            using (var db = new LiteDatabase($"{_path}/SisenseReplication.db"))
            {
                var versions = db.GetCollection<ReplicationVersionRecord>($"{safeShapeName}_versions");

                var records = versions.FindAll();
                
                return records.Select(r => r.Data).ToList();
            }
        }
        
        [HttpGet]
        [Route("versions/{shapename}/{id}")]
        [ProducesResponseType(typeof(Dictionary<string,object> ), 200)]
        public Dictionary<string,object> VersionById([FromRoute] string shapename, [FromRoute] string id)
        {
            if (string.IsNullOrEmpty(shapename))
            {
                throw new Exception("shapename must be provided");
            }
            
            var safeShapeName = string.Concat(shapename.Where(c => !char.IsWhiteSpace(c)));
            
            Directory.CreateDirectory(_path);
            
            using (var db = new LiteDatabase($"{_path}/SisenseReplication.db"))
            {
                var versions = db.GetCollection<ReplicationVersionRecord>($"{safeShapeName}_versions");

                var record = versions.FindOne(r => r.VersionRecordId == id);
                
                return record.Data;
            }
        }
        
        [HttpGet]
        [Route("shapes")]
        [ProducesResponseType(typeof(List<string>), 200)]
        public List<string> Shapes()
        {
            Directory.CreateDirectory(_path);
            
            using (var db = new LiteDatabase($"{_path}/SisenseReplication.db"))
            {
                var shapes = db.GetCollection<ShapeNameObject>($"shapes");

                var shapesList = shapes.FindAll().ToList();
                
                return shapesList.Select(s => s.ShapeName).ToList();
            }
        }
    }
}