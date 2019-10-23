namespace Plugin_Sisense.API.Read
{
    public static partial class Read
    {
        //            var schema = request.Schema;
//            var limit = request.Limit;
//            var limitFlag = request.Limit != 0;
//            
//            // get to get a schema for each module found
//            try
//            {
//                
//                // get additional metadata about properties for formatting
//                var jaqlUri = $"elasticubes/{_server.Settings.EncodedElasticCube}/jaql";
//
//                var jaql = $@"{{ ""datasource"": ""{_server.Settings.ElastiCube}"",
//                    ""metadata"": [
//                        {{
//                            ""dim"": ""{schema.Id}""
//                        }}
//                    ]
//                }}";
//                
//                var prop = schema.Properties.First();
//
//                var body = new StringContent(jaql, Encoding.UTF8, MediaTypeNames.Application.Json);
//                var response = await _client.PostAsync(jaqlUri, body);
//                response.EnsureSuccessStatusCode();
//
//                dynamic dataJson = JObject.Parse(await response.Content.ReadAsStringAsync());
//                JArray items = (JArray) dataJson.values;
//
//                foreach (dynamic item in items)
//                {
//                    var itemValues = (JArray) item;
//
//                    foreach (dynamic iv in itemValues)
//                    {
//                        var d = new Dictionary<string, object>
//                        {
//                            { prop.Id, iv.data }
//                        };
//
//                        var record = new Record
//                        {
//                            Action = Record.Types.Action.Upsert,
//                            DataJson = JsonConvert.SerializeObject(d)
//                        };
//                        
//                        await responseStream.WriteAsync(record);
//                    }
//                }
//            }
//            catch (Exception e)
//            {
//                Logger.Error(e.Message);
//                throw;
//            }
    }
}