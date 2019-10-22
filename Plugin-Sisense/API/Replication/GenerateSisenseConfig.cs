using System;
using System.Linq;
using Newtonsoft.Json;
using Plugin_Sisense.DataContracts;
using Plugin_Sisense.Helper;

namespace Plugin_Sisense.API.Replication
{
    public static partial class Replication
    {
        /// <summary>
        /// Generates a Sisense config object
        /// </summary>
        /// <returns></returns>
        public static SisenseConfig GenerateSisenseConfig()
        {
            var address = GetBindingHostedService.ServerAddresses.Addresses.First();
            return new SisenseConfig();
        }
    }
}