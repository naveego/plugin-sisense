using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;
using Google.Protobuf.Collections;
using Grpc.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Plugin_Naveego_Legacy.DataContracts;
using Plugin_Naveego_Legacy.Helper;
using Pub;

namespace Plugin_Naveego_Legacy.Plugin
{
    public class Plugin : Publisher.PublisherBase
    {
        private readonly HttpClient _injectedClient;
        
        private string _authToken = null;
        private FormSettings _formSettings;

        private TaskCompletionSource<bool> _tcs;
        
        public Plugin(HttpClient client = null)
        {
            _injectedClient = client != null ? client : new HttpClient();
        }

        /// <summary>
        /// Establishes a connection with Naveego Legacy CRM. Creates an authenticated http client and tests it.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="context"></param>
        /// <returns>A message indicating connection success</returns>
        public override async Task<ConnectResponse> Connect(ConnectRequest request, ServerCallContext context)
        {
            try
            {
                _formSettings = JsonConvert.DeserializeObject<FormSettings>(request.SettingsJson);
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
                return new ConnectResponse
                {
                    ConnectionError = "",
                    OauthError = "",
                    SettingsError = e.Message
                };
            }

            // create new authenticated request helper with validated settings
            var authSuccess = await AuthorizeHttpClient();

            if (!authSuccess)
            {
                return new ConnectResponse
                {
                    ConnectionError = "Could not authenticate to API",
                    OauthError = "",
                    SettingsError = ""
                };
            }

            // attempt to call the Legacy API api
            try
            {
                var testUri = ToResourceUri("api/elasticubes/metadata");


                var response = await _injectedClient.GetAsync(testUri);
                response.EnsureSuccessStatusCode();
                Logger.Info("Connected to Sisense API");
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);

                return new ConnectResponse
                {
                    OauthStateJson = request.OauthStateJson,
                    ConnectionError = e.Message,
                    OauthError = "",
                    SettingsError = ""
                };
            }

            return new ConnectResponse
            {
                ConnectionError = "",
                OauthError = "",
                SettingsError = ""
            };
        }

        public override async Task ConnectSession(ConnectRequest request,
            IServerStreamWriter<ConnectResponse> responseStream, ServerCallContext context)
        {
            Logger.Info("Connecting session...");

            // create task to wait for disconnect to be called
            _tcs?.SetResult(true);
            _tcs = new TaskCompletionSource<bool>();

            // call connect method
            var response = await Connect(request, context);

            await responseStream.WriteAsync(response);

            Logger.Info("Session connected.");

            // wait for disconnect to be called
            await _tcs.Task;
        }


        /// <summary>
        /// Discovers schemas located in the users Zoho CRM instance
        /// </summary>
        /// <param name="request"></param>
        /// <param name="context"></param>
        /// <returns>Discovered schemas</returns>
        public override async Task<DiscoverSchemasResponse> DiscoverSchemas(DiscoverSchemasRequest request,
            ServerCallContext context)
        {
            Logger.Info("Discovering Schemas...");

            DiscoverSchemasResponse discoverSchemasResponse = new DiscoverSchemasResponse();

            var isAuthed = await AuthorizeHttpClient();
            if (!isAuthed)
            {
                return discoverSchemasResponse;
            }

            // get to get a schema for each module found
            try
            {
                var fieldsUri = ToResourceUri($"elasticubes/metadata/{_formSettings.ElastiCube}/fields");
                var fieldsResp = await _injectedClient.GetAsync(fieldsUri);
                fieldsResp.EnsureSuccessStatusCode();

                JArray fields = JArray.Parse(await fieldsResp.Content.ReadAsStringAsync());

                foreach (dynamic field in fields)
                { 
                    var schema = new Schema 
                    {
                        Id = field.id,
                        Name = field.id,
                        DataFlowDirection = Schema.Types.DataFlowDirection.Read
                    };

                   schema.Properties.Add(new Property
                    {
                        Id = field.id,
                        Name = field.title,
                        Type = GetPropertyType((string)field.dimtype)
                    });

                   discoverSchemasResponse.Schemas.Add(schema);
                }
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
                throw;
            }

            Logger.Info($"Schemas found: {discoverSchemasResponse.Schemas.Count}");

            // only return requested schemas if refresh mode selected
            if (request.Mode == DiscoverSchemasRequest.Types.Mode.Refresh)
            {
                var refreshSchemaIds = request.ToRefresh.Select(x => x.Id);
                var schemas =
                    JsonConvert.DeserializeObject<Schema[]>(
                        JsonConvert.SerializeObject(discoverSchemasResponse.Schemas));
                discoverSchemasResponse.Schemas.Clear();
                discoverSchemasResponse.Schemas.AddRange(schemas.Where(x => refreshSchemaIds.Contains(x.Id)));
                

                Logger.Debug($"Schemas found: {JsonConvert.SerializeObject(schemas)}");
                Logger.Debug($"Refresh requested on schemas: {refreshSchemaIds}");

                Logger.Info($"Schemas returned: {discoverSchemasResponse.Schemas.Count}");
                return discoverSchemasResponse;
            }

            // return all schemas otherwise
            Logger.Info($"Schemas returned: {discoverSchemasResponse.Schemas.Count}");
            return discoverSchemasResponse;
        }

        /// <summary>
        /// Publishes a stream of data for a given schema
        /// </summary>
        /// <param name="request"></param>
        /// <param name="responseStream"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public override async Task ReadStream(ReadRequest request, IServerStreamWriter<Record> responseStream,
            ServerCallContext context)
        {
            var schema = request.Schema;
            var limit = request.Limit;
            var limitFlag = request.Limit != 0;
            
            // get to get a schema for each module found
            try
            {
                
                // get additional metadata about properties for formatting
                var jaqlUri = ToResourceUri($"elasticubes/{_formSettings.ElastiCube}/jaql");

                var jaql = $@"{{ ""datasource"": ""{_formSettings.ElastiCube}"",
                    ""metadata"": [
                        {{
                            ""dim"": ""{schema.Id}""
                        }}
                    ]
                }}";
                
                var prop = schema.Properties.First();

                var dataPostContent = new StringContent(jaql, Encoding.UTF8, MediaTypeNames.Application.Json);
                var dataResp = await _injectedClient.PostAsync(jaqlUri, dataPostContent);
                dataResp.EnsureSuccessStatusCode();

                dynamic dataJson = JObject.Parse(await dataResp.Content.ReadAsStringAsync());
                JArray items = (JArray) dataJson.values;

                foreach (dynamic item in items)
                {
                    var itemValues = (JArray) item;

                    foreach (dynamic iv in itemValues)
                    {
                        var d = new Dictionary<string, object>
                        {
                            { prop.Id, iv.data }
                        };

                        var record = new Record
                        {
                            Action = Record.Types.Action.Upsert,
                            DataJson = JsonConvert.SerializeObject(d)
                        };
                        
                        await responseStream.WriteAsync(record);
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
                throw;
            }
        }

        
        /// <summary>
        /// Gets the Naveego type from the provided Zoho information
        /// </summary>
        /// <param name="field"></param>
        /// <returns>The property type</returns>
        private PropertyType GetPropertyType(string type)
        {
            switch (type)
            {
                case "boolean":
                    return PropertyType.Bool;
                case "double":
                    return PropertyType.Float;
                case "number":
                case "integer":
                    return PropertyType.Integer;
                case "jsonarray":
                case "jsonobject":
                    return PropertyType.Json;
                case "date":
                case "datetime":
                    return PropertyType.Datetime;
                case "time":
                    return PropertyType.Text;
                case "float":
                    return PropertyType.Float;
                case "decimal": 
                case "numeric":
                    return PropertyType.Decimal;
                default:
                    return PropertyType.String;
            }
        }

        /// <summary>
        /// Checks if a http response message is not empty and did not fail
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        private bool IsSuccessAndNotEmpty(HttpResponseMessage response)
        {
            return response.StatusCode != HttpStatusCode.NoContent && response.IsSuccessStatusCode;
        }

        private async Task<bool> AuthorizeHttpClient()
        {
            if (_authToken != null)
            {
                _injectedClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", _authToken);

                return true;
            }
            
            try
            {
                var keyValues = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("username", _formSettings.Username),
                    new KeyValuePair<string, string>("password", _formSettings.Password)
                };
                var formContent = new FormUrlEncodedContent(keyValues);

                var authUrl = ToResourceUri("v1/authentication/login");

                var resp = await _injectedClient.PostAsync(authUrl, formContent);

                if (resp.IsSuccessStatusCode)
                {
                    var respJson = JObject.Parse(await resp.Content.ReadAsStringAsync());
                    _authToken = (string) respJson["access_token"];

                    _injectedClient.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", _authToken);
                    
                    return true;
                }
            }
            catch (Exception e)
            {
               Logger.Error($"Could not authenticate plugin: ${e.Message}");
            }

            return false;
        }

        private string ToResourceUri(string resource)
        {
            return WebUtility.UrlEncode($"http://{_formSettings.APIUrl}/api/{resource}");
        }
    }
}