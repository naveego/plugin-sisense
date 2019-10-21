using System.Collections.Generic;
using Newtonsoft.Json;

namespace Plugin_Sisense.API.Replication
{
    public static partial class Replication
    {
        public static string GetSchemaJson()
        {
            var schemaJsonObj = new Dictionary<string, object>
            {
                {"type", "object"},
                {"properties", new Dictionary<string, object>
                {
                    {"FolderPath", new Dictionary<string, string>
                    {
                        {"type", "string"},
                        {"title", "Folder Path"},
                        {"description", "Path to a folder to store data"},
                    }},
                }},
                {"required", new []
                {
                    "FolderPath"
                }}
            };
            
            var uiJsonObj = new Dictionary<string, object>
            {
                {"ui:order", new []
                {
                    "FolderPath"
                }}
            };

            return JsonConvert.SerializeObject(schemaJsonObj);
        }
    }
}