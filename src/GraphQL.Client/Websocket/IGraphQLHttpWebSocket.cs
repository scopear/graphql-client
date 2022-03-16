using System;
using GraphQL.Client.Abstractions.Websocket;

namespace GraphQL.Client.Http.Websocket
{
    public interface IGraphQLHttpWebSocket: IDisposable
    {
        public UniRx.IObservable<GraphQLWebsocketConnectionState> ConnectionState { get; }
        public UniRx.IObservable<Exception> ReceiveErrors { get; }
    }
}
