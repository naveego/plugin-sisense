using System.IO;
using Plugin_Sisense.DataContracts;

namespace Plugin_Sisense.API.Replication
{
    public static partial class Replication
    {
        /// <summary>
        /// Updates the config files and restarts the Sisense service
        /// </summary>
        /// <returns>An error string</returns>
        public static void RestartSisenseService(SisenseConfig config)
        {
            var configDirectory = @"C:/Program Files/Sisense/DataConnectors/DotNetContainer/Connectors/REST.Naveego.Connector";
            var configFileName = "config.json";
            var dllFileName = "_rest.tag";

            Directory.CreateDirectory(configDirectory);
            
            
            return;
        }
    }
}