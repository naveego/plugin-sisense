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
        /// Gets all write schemas
        /// </summary>
        /// <param name="settings"></param>
        /// <returns></returns>
        public static async Task<List<Schema>> GetAllWriteSchema(Settings settings)
        {
            var schemas = new List<Schema>();

            return schemas;
        }
    }
}