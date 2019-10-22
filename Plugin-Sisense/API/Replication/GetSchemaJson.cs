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
            };

            return JsonConvert.SerializeObject(schemaJsonObj);
        }
    }
}