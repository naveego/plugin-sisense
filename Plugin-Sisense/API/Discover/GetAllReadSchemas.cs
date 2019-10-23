using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Plugin_Sisense.Helper;
using Pub;

namespace Plugin_Sisense.API.Discover
{
    public static partial class Discover
    {
        /// <summary>
        /// Gets all read schemas
        /// </summary>
        /// <param name="client"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        public static async Task<List<Schema>> GetAllReadSchemas(RequestHelper client, Settings settings)
        {
//            var fieldsUri = $"elasticubes/metadata/{settings.EncodedElasticCube}/fields";
//            var response = await client.GetAsync(fieldsUri);
//            response.EnsureSuccessStatusCode();
//
//            JArray fields = JArray.Parse(await response.Content.ReadAsStringAsync());

            var schemas = new List<Schema>();
//            foreach (dynamic field in fields)
//            { 
//                var schema = new Schema 
//                {
//                    Id = field.id,
//                    Name = field.id,
//                    DataFlowDirection = Schema.Types.DataFlowDirection.Read
//                };
//
//                schema.Properties.Add(new Property
//                {
//                    Id = field.id,
//                    Name = field.title,
//                    Type = GetPropertyType((string)field.dimtype)
//                });
//
//                schemas.Add(schema);
//            }

            return schemas;
        }
    }
}