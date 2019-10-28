using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using LiteDB;
using Newtonsoft.Json;
using Plugin_Sisense.DataContracts;
using Plugin_Sisense.Helper;
using Logger = Plugin_Sisense.Helper.Logger;


namespace Plugin_Sisense.API.Replication
{
    public static partial class Replication
    {
        /// <summary>
        /// Generates a Sisense config object
        /// </summary>
        /// <returns></returns>
        public static SisenseConfig GenerateSisenseConfig()
        {
            Logger.Info("Generating Sisense Config...");
            
            var apiBaseUri = GetBindingHostedService.ServerAddresses.Addresses.First();
            var tables = new List<SisenseTable>();
            
            using (var db = new LiteDatabase($"{Path}/SisenseReplication.db"))
            {
                var shapes = db.GetCollection<ShapeNameObject>($"shapes");

                var shapesList = shapes.FindAll().ToList();

                foreach (var shape in shapesList)
                {
                    var table = new SisenseTable
                    {
                        Name = shape.ShapeName,
                        Schema = "Http",
                        Method = "GET",
                        Base = apiBaseUri,
                        Path = $"v1/replication/goldenrecords/{HttpUtility.UrlEncode(shape.ShapeName)}",
                        Headers = new Dictionary<string, string>(),
                        PathParameters = new List<string>(),
                        DataPath = ""
                    };
                    tables.Add(table);
                }
            }
            
            Logger.Info("Generated Sisense Config");
            
            return new SisenseConfig
            {
                Settings = new SisenseSettings
                {
                    Provider = "rest.naveego.connector",
                    DisplayName = "Naveego",
                    ConnectorAssemblyFileName = "_rest.tag",
                    MaxDocs = 100,
                    FetchSize = 1000,
                },
                Tables = tables
            };
        }
    }
}