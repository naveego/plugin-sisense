using System;
using System.IO;
using System.Threading;
using Newtonsoft.Json;
using Plugin_Sisense.DataContracts;
using Plugin_Sisense.Helper;

namespace Plugin_Sisense.API.Replication
{
    public static partial class Replication
    {
        /// <summary>
        /// Updates the Sisense config files 
        /// </summary>
        /// <returns>An error string</returns>
        public static void AddSisenseService(SisenseConfig config)
        {
            Logger.Info("Adding Sisense Config...");
            
            var configDirectory = @"C:/Program Files/Sisense/DataConnectors/DotNetContainer/Connectors/REST.Naveego.Connector";
            var configFileName = "config.json";
            var dllFileName = "_rest.tag";
            
            // delete existing config
            try
            {
                if (Directory.Exists(configDirectory))
                {
                    Logger.Info("Deleting config directory");
                    Directory.Delete(configDirectory, true);
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, e.Message);
            }

            // create the config files
            try
            {
                Logger.Info("Creating config directory");
                while (Directory.Exists(configDirectory))
                {
                    Logger.Info("Waiting for delete to complete...");
                    Thread.Sleep(100);
                }
                Directory.CreateDirectory(configDirectory);
                File.WriteAllText($"{configDirectory}/{configFileName}", JsonConvert.SerializeObject(config, Formatting.Indented));
                File.Create($"{configDirectory}/{dllFileName}").Dispose();
            }
            catch (Exception e)
            {
                Logger.Error(e, e.Message);
            }
            
            Logger.Info("Added Sisense Config");
        }
    }
}