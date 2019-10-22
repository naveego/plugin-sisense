
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using Pub;
using Xunit;
using Record = Pub.Record;

namespace Plugin_Sisense.Plugin
{
    public class PluginTest
    {
        
        private ConnectRequest GetConnectSettings()
        {
            return new ConnectRequest
            {
                SettingsJson = "{\"APIUrl\":\"http://localhost:8081\",\"Username\":\"derek.smith@naveego.com\",\"Password\":\"\", \"ElastiCube\": \"Naveego\" }"
            };
        }
        [Fact]
        public async Task ConnectTest()
        {
            // setup

            Server server = new Server
            {
                Services = {Publisher.BindService(new Plugin())},
                Ports = {new ServerPort("localhost", 0, ServerCredentials.Insecure)}
            };
            server.Start();

            var port = server.Ports.First().BoundPort;

            var channel = new Channel($"localhost:{port}", ChannelCredentials.Insecure);
            var client = new Publisher.PublisherClient(channel);

            var request = GetConnectSettings();

            // act
            var response = client.Connect(request);

            // assert
            Assert.IsType<ConnectResponse>(response);

            // cleanup
            await channel.ShutdownAsync();
            await server.ShutdownAsync();
        }
        
        [Fact]
        public async Task DiscoverSchemasAllTest()
        {
            // setup
            Server server = new Server
            {
                Services = {Publisher.BindService(new Plugin())},
                Ports = {new ServerPort("localhost", 0, ServerCredentials.Insecure)}
            };
            server.Start();

            var port = server.Ports.First().BoundPort;

            var channel = new Channel($"localhost:{port}", ChannelCredentials.Insecure);
            var client = new Publisher.PublisherClient(channel);

            var connectRequest = GetConnectSettings();

            var request = new DiscoverSchemasRequest
            {
                Mode = DiscoverSchemasRequest.Types.Mode.All,
            };

            // act
            client.Connect(connectRequest);
            var response = client.DiscoverSchemas(request);

            // assert
            Assert.IsType<DiscoverSchemasResponse>(response);
            Assert.Equal(12, response.Schemas.Count);

            // cleanup
            await channel.ShutdownAsync();
            await server.ShutdownAsync();
        }
        
        [Fact]
        public async Task ReadStreamTest()
        {
            // setup

            Server server = new Server
            {
                Services = {Publisher.BindService(new Plugin())},
                Ports = {new ServerPort("localhost", 0, ServerCredentials.Insecure)}
            };
            server.Start();

            var port = server.Ports.First().BoundPort;

            var channel = new Channel($"localhost:{port}", ChannelCredentials.Insecure);
            var client = new Publisher.PublisherClient(channel);

            var connectRequest = GetConnectSettings();

            var request = new ReadRequest()
            {
                Schema = new Schema
                {
                    Id = "[Customers.address]",
                    Properties =
                    {
                        new Property{ Id = "[Customers.address]", Type=PropertyType.String} 
                    }
                }
            };

            // act
            client.Connect(connectRequest);
            var response = client.ReadStream(request);
            var responseStream = response.ResponseStream;
            var records = new List<Record>();

            while (await responseStream.MoveNext())
            {
                records.Add(responseStream.Current);
            }

            // assert
            Assert.Equal(775, records.Count);

            // cleanup
            await channel.ShutdownAsync();
            await server.ShutdownAsync();
        }
    }
}