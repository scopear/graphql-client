using GraphQL.Client.Abstractions.Websocket;

namespace GraphQL.Client.Http.Websocket;

public interface IWebsocketProtocolHandler
{
    string WebsocketProtocol { get; }

    UniRx.IObservable<GraphQLResponse<TResponse>> CreateSubscriptionObservable<TResponse>(GraphQLRequest request);

    UniRx.IObservable<GraphQLResponse<TResponse>> CreateGraphQLRequestObservable<TResponse>(GraphQLRequest request);

    UniRx.IObservable<object?> CreatePongObservable();

    Task InitializeConnectionAsync(UniRx.IObservable<WebsocketMessageWrapper> incomingMessages, UniRx.CompositeDisposable closeConnectionDisposable);

    Task SendCloseConnectionRequestAsync();

    Task SendPingAsync(object? payload);

    Task SendPongAsync(object? payload);
}
