using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Plugin_Sisense.DataContracts;

namespace Plugin_Sisense.Helper
{
    public class Authenticator
    {
        private readonly HttpClient _client;
        private readonly Settings _settings;
        private string _token;

        public Authenticator(Settings settings, HttpClient client)
        {
            _client = client;
            _settings = settings;
            _token = string.Empty;
        }

        /// <summary>
        /// Get a token for the Salesforce API
        /// </summary>
        /// <returns></returns>
        public async Task<string> GetToken()
        {
            // check if token is expired or will expire in 5 minutes or less
            if (_token == string.Empty)
            {
                try
                {
                    var keyValues = new List<KeyValuePair<string, string>>
                    {
                        new KeyValuePair<string, string>("username", _settings.Username),
                        new KeyValuePair<string, string>("password", _settings.Password)
                    };
                    var formContent = new FormUrlEncodedContent(keyValues);

                    var authUrl = _settings.ToResourceUri("v1/authentication/login");

                    var response = await _client.PostAsync(authUrl, formContent);
                    response.EnsureSuccessStatusCode();
                    
                    var content = JsonConvert.DeserializeObject<TokenResponse>(await response.Content.ReadAsStringAsync());

                    _token = content.AccessToken;

                    return _token;
                }
                catch (Exception e)
                {
                    Logger.Error(e.Message);
                    throw;
                }
            }
            
            // return saved token
            return _token;
        }
    }
}