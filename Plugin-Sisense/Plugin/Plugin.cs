using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
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
        private readonly string _authUri = "https://login.naveego.com";
        private readonly string _apiUri = "https://useast-pod-01.naveegoapi.com";
        private readonly HttpClient _injectedClient;
        
        private string _authToken = null;
        private FormSettings _formSettings;
        private string[] _convertNullToZero;
  
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

                _convertNullToZero = (_formSettings.ConvertNullToZero == null)
                    ? new string[0]
                    : _formSettings.ConvertNullToZero.Split(',', StringSplitOptions.RemoveEmptyEntries);
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
                var whoAmIUrl = $"{_apiUri}/v3/whoami";


                var response = await _injectedClient.GetAsync(whoAmIUrl);
                response.EnsureSuccessStatusCode();
                Logger.Info("Connected to Naveego Legacy API");
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
                var rulesUrl = $"{_apiUri}/v3/mdm/merging/rules";
                var rulesResp = await _injectedClient.GetAsync(rulesUrl);
                rulesResp.EnsureSuccessStatusCode();

                dynamic rulesJson = JObject.Parse(await rulesResp.Content.ReadAsStringAsync());

                foreach (dynamic rule in rulesJson.data)
                {
                    var schema = new Schema
                    {
                        Id = rule.@object,
                        Name = rule.@object,
                        DataFlowDirection = Schema.Types.DataFlowDirection.Read
                    };
                    
                    // Add ID property
                    schema.Properties.Add(new Property
                    {
                        Id = "ID",
                        Name = "ID",
                        Type = PropertyType.String,
                        Description = "The global identifier",
                        IsKey = true
                    });

                    foreach (dynamic prop in rule.properties)
                    {
                        schema.Properties.Add(new Property
                        {
                            Id = prop.name,
                            Name = prop.name,
                            Type = GetPropertyType((string)prop.type)
                        });
                    }

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
                var metaUrl = $"{_apiUri}/v3/metadata/objects/{schema.Name}";
                var metaResp = await _injectedClient.GetAsync(metaUrl);
                LegacyMetadata metaJson = JsonConvert.DeserializeObject<LegacyMetadata>(await metaResp.Content.ReadAsStringAsync());
                
                var recordCount = 0;
                var page = 1;
                var pageSize = 100;

                do
                {
                    var dataUrl = $"{_apiUri}/v3/data/objects/{schema.Name}?page={page}&pagesize={pageSize}";
                    var dataResp = await _injectedClient.GetAsync(dataUrl);
                    dataResp.EnsureSuccessStatusCode();

                    dynamic dataJson = JObject.Parse(await dataResp.Content.ReadAsStringAsync());
                    int total = dataJson.meta.count;
                    JArray items = (JArray) dataJson.data;
                    
                    foreach (dynamic item in items)
                    {
                        var data = new Dictionary<string, object>();
                        
                        foreach (var prop in schema.Properties)
                        {

                            var propMeta = metaJson.LegacyProperties.FirstOrDefault(p => p.Name == prop.Id);
                            var scale = (propMeta != null) ? propMeta.Scale : 0;
                            
                            if (prop.Id == "ID")
                            {
                                data.Add(prop.Id, item._id.ToString());
                                continue;
                            }

                            if (item.ContainsKey(prop.Id))
                            {
                                object value = item[prop.Id];
                                if (value != null)
                                {
                                    switch (prop.Type)
                                    {
                                        case PropertyType.String:
                                            // This is a number as s string as it has preceding zeros
                                            if (value.ToString().StartsWith("0") && value.ToString().Length > 1)
                                            {
                                                value = value.ToString().Replace("\n", "\r\n");
                                            }
                                            else if (DateTime.TryParseExact(value.ToString(), "MM/dd/yyyy hh:mm:ss",
                                                new CultureInfo("en-US"), DateTimeStyles.None, out var dr))
                                            {
                                                value = dr.ToString("yyyy-MM-ddTHH:mm:ss");
                                            }
                                            else if (decimal.TryParse(value.ToString(), out var d))
                                            {
                                                var suffix = (value.ToString().Contains("\n")) ? "\r\n" : "";
                                                value = (!ConvertNullToZero(prop.Id) && d == 0.0M) ? null : PrepareDecimal(scale, d);

                                                if (suffix != "")
                                                {
                                                    value = value.ToString() + suffix;
                                                }
                                            }
                                            else
                                            {
                                                value = (value.ToString()).Replace("\n", "\r\n");
                                            }
                                            break;
                                        case PropertyType.Datetime:
                                            if (value is DateTime time)
                                            {
                                                value = time.ToString("yyyy-MM-ddTHH:mm:ss");
                                            }
                                            else if (DateTime.TryParse(value.ToString(), out var rd))
                                            {
                                                value = rd.ToString("yyyy-MM-ddTHH:mm:ss");
                                            }
                                            break;
                                        case PropertyType.Float:
                                        case PropertyType.Decimal:
                                            value = (Convert.ToDecimal(value) == 0.0M) ? null : PrepareDecimal(scale, Convert.ToDecimal(value));
                                            break;
                                        case PropertyType.Integer:
                                            value = (Convert.ToInt64(value) == 0) ? null : value;
                                            break;
                                    }

                                    data.Add(prop.Id, value);
                                    continue;
                                }
                            }

                            data.Add(prop.Id, null);
                        }

                        var record = new Record
                        {
                            Action = Record.Types.Action.Upsert,
                            DataJson = JsonConvert.SerializeObject(data)
                        };

                        if (limitFlag && recordCount == limit)
                        {
                            break;
                        }

                        await responseStream.WriteAsync(record);
                        recordCount++;
                    }
                    
                    if (limitFlag && recordCount == limit)
                    {
                        break;
                    }

                    if (recordCount == total || items.Count == 0)
                    {
                        break;
                    }

                    page++;
                } while (true);
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
                    return PropertyType.Decimal;
                default:
                    return PropertyType.String;
            }
        }

        private string PrepareDecimal(int scale, decimal value)
        {
         
            
            var s = value.ToString();
            var numOfZeros = scale;
            
            if (scale == 0)
            {
                return s;
            }
            
            var idx = s.IndexOf('.');
            if (idx <= 0)
            {
                s += ".";
            }
            else
            {
                numOfZeros = scale - (s.Length - (idx + 1));
            }

            return s + new string('0', numOfZeros);
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

        private bool ConvertNullToZero(string fieldName)
        {
            return _convertNullToZero.Contains(fieldName, StringComparer.OrdinalIgnoreCase);
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
                    new KeyValuePair<string, string>("grant_type", "password"),
                    new KeyValuePair<string, string>("username", _formSettings.Username),
                    new KeyValuePair<string, string>("password", _formSettings.Password),
                    new KeyValuePair<string, string>("client_id", _formSettings.OAuthClientId),
                    new KeyValuePair<string, string>("client_secret", _formSettings.OAuthClientSecret)
                };
                var formContent = new FormUrlEncodedContent(keyValues);

                var authUrl = $"{_authUri}/oauth2/token";

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
    }
}