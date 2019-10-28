
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using Pub;
using RichardSzalay.MockHttp;
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
                SettingsJson = "{\"Hostname\":\"hostname\",\"Username\":\"test\",\"Password\":\"password\"}"
            };
        }

        private MockHttpMessageHandler GetMockHttpMessageHandler()
        {
            var mockHttp = new MockHttpMessageHandler();

            mockHttp.When("http://hostname/api/v1/authentication/login")
                .Respond("application/json", "{\"access_token\":\"token\"}");

            mockHttp.When("http://hostname/api/v1/connection")
                .Respond("application/json", "{}");

            
            return mockHttp;
        }
        
        [Fact]
        public async Task ConnectTest()
        {
            // setup

            Server server = new Server
            {
                Services = {Publisher.BindService(new Plugin(GetMockHttpMessageHandler().ToHttpClient()))},
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
                Services = {Publisher.BindService(new Plugin(GetMockHttpMessageHandler().ToHttpClient()))},
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
            Assert.Empty(response.Schemas);

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
                Services = {Publisher.BindService(new Plugin(GetMockHttpMessageHandler().ToHttpClient()))},
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
            Assert.Empty(records);

            // cleanup
            await channel.ShutdownAsync();
            await server.ShutdownAsync();
        }
        
        [Fact]
        public async Task PrepareWriteTest()
        {
            // setup
            Server server = new Server
            {
                Services = {Publisher.BindService(new Plugin(GetMockHttpMessageHandler().ToHttpClient()))},
                Ports = {new ServerPort("localhost", 0, ServerCredentials.Insecure)}
            };
            server.Start();

            var port = server.Ports.First().BoundPort;

            var channel = new Channel($"localhost:{port}", ChannelCredentials.Insecure);
            var client = new Publisher.PublisherClient(channel);

            var connectRequest = GetConnectSettings();

            var request = new PrepareWriteRequest()
            {
                Schema = new Schema
                {
                    Id = "Test",
                    Properties =
                    {
                        new Property
                        {
                            Id = "Id",
                            Type = PropertyType.String,
                            IsKey = true
                        },
                        new Property
                        {
                            Id = "Name",
                            Type = PropertyType.String
                        }
                    }
                },
                CommitSlaSeconds = 1
            };

            // act
            client.Connect(connectRequest);
            var response = client.PrepareWrite(request);

            // assert
            Assert.IsType<PrepareWriteResponse>(response);

            // cleanup
            await channel.ShutdownAsync();
            await server.ShutdownAsync();
        }
        
//        [Fact]
//        public async Task WriteStreamTest()
//        {
//            // setup
//            Server server = new Server
//            {
//                Services = {Publisher.BindService(new Plugin(GetMockHttpMessageHandler().ToHttpClient()))},
//                Ports = {new ServerPort("localhost", 0, ServerCredentials.Insecure)}
//            };
//            server.Start();
//
//            var port = server.Ports.First().BoundPort;
//
//            var channel = new Channel($"localhost:{port}", ChannelCredentials.Insecure);
//            var client = new Publisher.PublisherClient(channel);
//
//            var connectRequest = GetConnectSettings();
//            
//            var discoverSchemasRequest = new DiscoverSchemasRequest
//            {
//                Mode = DiscoverSchemasRequest.Types.Mode.Refresh,
//                ToRefresh = {new Schema {Id = "Test"}}
//            };
//
//            var prepareRequest = new PrepareWriteRequest()
//            {
//                Schema = new Schema
//                {
//                    Id = "Test",
//                    Properties =
//                    {
//                        new Property
//                        {
//                            Id = "Id",
//                            Type = PropertyType.String,
//                            IsKey = true
//                        },
//                        new Property
//                        {
//                            Id = "Name",
//                            Type = PropertyType.String
//                        }
//                    }
//                },
//                CommitSlaSeconds = 5,
//                Replication = new ReplicationWriteRequest
//                {
//                    SettingsJson = "{\"ShapeName\":\"test\"}",
//                    Versions = {  }
//                }
//            };
//
//            var records = new List<Record>()
//            {
//                {
//                    new Record
//                    {
//                        Action = Record.Types.Action.Upsert,
//                        CorrelationId = "test",
//                        DataJson = "{\"Id\":1,\"Name\":\"Test Company\"}"
//                    }
//                }
//            };
//
//            var recordAcks = new List<RecordAck>();
//
//            // act
//            client.Connect(connectRequest);
//            client.DiscoverSchemas(discoverSchemasRequest);
//            client.PrepareWrite(prepareRequest);
//
//            using (var call = client.WriteStream())
//            {
//                var responseReaderTask = Task.Run(async () =>
//                {
//                    while (await call.ResponseStream.MoveNext())
//                    {
//                        var ack = call.ResponseStream.Current;
//                        recordAcks.Add(ack);
//                    }
//                });
//
//                foreach (Record record in records)
//                {
//                    await call.RequestStream.WriteAsync(record);
//                }
//
//                await call.RequestStream.CompleteAsync();
//                await responseReaderTask;
//            }
//
//            // assert
//            Assert.Single(recordAcks);
//            Assert.Equal("", recordAcks[0].Error);
//            Assert.Equal("test", recordAcks[0].CorrelationId);
//
//            // cleanup
//            await channel.ShutdownAsync();
//            await server.ShutdownAsync();
//        }
        
        [Fact]
        public async Task DisconnectTest()
        {
            // setup
            Server server = new Server
            {
                Services = {Publisher.BindService(new Plugin(GetMockHttpMessageHandler().ToHttpClient()))},
                Ports = {new ServerPort("localhost", 0, ServerCredentials.Insecure)}
            };
            server.Start();

            var port = server.Ports.First().BoundPort;

            var channel = new Channel($"localhost:{port}", ChannelCredentials.Insecure);
            var client = new Publisher.PublisherClient(channel);

            var request = new DisconnectRequest();

            // act
            var response = client.Disconnect(request);

            // assert
            Assert.IsType<DisconnectResponse>(response);

            // cleanup
            await channel.ShutdownAsync();
            await server.ShutdownAsync();
        }
    }
}