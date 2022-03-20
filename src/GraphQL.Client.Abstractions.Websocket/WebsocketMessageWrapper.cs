using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace GraphQL.Client.Abstractions.Websocket
{
    public class WebsocketMessageWrapper : GraphQLWebSocketResponse
    {

        [IgnoreDataMember]
        public byte[] MessageBytes { get; set; }

        [JsonConstructor]
        public WebsocketMessageWrapper()
        {}
    }
}
