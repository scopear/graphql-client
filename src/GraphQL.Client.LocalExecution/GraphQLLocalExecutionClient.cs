using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using GraphQL.Client.Abstractions;
using GraphQL.Client.Serializer.Newtonsoft;
using GraphQL.NewtonsoftJson;
using GraphQL.Subscription;
using GraphQL.Types;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace GraphQL.Client.LocalExecution
{
    public static class GraphQLLocalExecutionClient
    {
        public static GraphQLLocalExecutionClient<TSchema> New<TSchema>(TSchema schema, IGraphQLJsonSerializer serializer) where TSchema : ISchema
            => new GraphQLLocalExecutionClient<TSchema>(schema, serializer, new SubscriptionDocumentExecuter(), new DocumentWriter());
    }

    public class GraphQLLocalExecutionClient<TSchema> : IGraphQLClient where TSchema : ISchema
    {
        private static readonly JsonSerializerSettings _variablesSerializerSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            DateTimeZoneHandling = DateTimeZoneHandling.Local,
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            Converters = new List<JsonConverter>
            {
                new ConstantCaseEnumConverter()
            }
        };

        public TSchema Schema { get; }

        public IGraphQLJsonSerializer Serializer { get; }

        private readonly IDocumentExecuter _documentExecuter;
        private readonly IDocumentWriter _documentWriter;

        public GraphQLLocalExecutionClient(TSchema schema, IGraphQLJsonSerializer serializer, IDocumentExecuter documentExecuter, IDocumentWriter documentWriter)
        {
            Schema = schema ?? throw new ArgumentNullException(nameof(schema), "no schema configured");
            Serializer = serializer ?? throw new ArgumentNullException(nameof(serializer), "please configure the JSON serializer you want to use");

            if (!Schema.Initialized)
                Schema.Initialize();
            _documentExecuter = documentExecuter;
            _documentWriter = documentWriter;
        }

        public void Dispose() { }

        public Task<GraphQLResponse<TResponse>> SendQueryAsync<TResponse>(GraphQLRequest request, CancellationToken cancellationToken = default)
            => ExecuteQueryAsync<TResponse>(request, cancellationToken);

        public Task<GraphQLResponse<TResponse>> SendMutationAsync<TResponse>(GraphQLRequest request, CancellationToken cancellationToken = default)
            => ExecuteQueryAsync<TResponse>(request, cancellationToken);

        public UniRx.IObservable<GraphQLResponse<TResponse>> CreateSubscriptionStream<TResponse>(GraphQLRequest request) =>
            UniRx.Observable.Defer(() => ExecuteSubscriptionAsync<TResponse>(request).ToObservable())
                .Concat()
                .Publish()
                .RefCount();

        public UniRx.IObservable<GraphQLResponse<TResponse>> CreateSubscriptionStream<TResponse>(GraphQLRequest request,
            Action<Exception> exceptionHandler)
            => CreateSubscriptionStream<TResponse>(request);

        #region Private Methods

        private async Task<GraphQLResponse<TResponse>> ExecuteQueryAsync<TResponse>(GraphQLRequest request, CancellationToken cancellationToken)
        {
            var executionResult = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);
            return await ExecutionResultToGraphQLResponseAsync<TResponse>(executionResult, cancellationToken).ConfigureAwait(false);
        }
        private async Task<IObservable<GraphQLResponse<TResponse>>> ExecuteSubscriptionAsync<TResponse>(GraphQLRequest request, CancellationToken cancellationToken = default)
        {
            var result = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);
            var stream = ((SubscriptionExecutionResult)result).Streams?.Values.SingleOrDefault();

            return stream == null
                ? Observable.Throw<GraphQLResponse<TResponse>>(new InvalidOperationException("the GraphQL execution did not return an observable"))
                : stream.SelectMany(executionResult => Observable.FromAsync(token => ExecutionResultToGraphQLResponseAsync<TResponse>(executionResult, token)));
        }

        private async Task<ExecutionResult> ExecuteAsync(GraphQLRequest request, CancellationToken cancellationToken = default)
        {
            string serializedRequest = Serializer.SerializeToString(request);

            var deserializedRequest = JsonConvert.DeserializeObject<GraphQLRequest>(serializedRequest);
            var inputs = deserializedRequest.Variables != null
                ? (JObject.FromObject(request.Variables, JsonSerializer.Create(_variablesSerializerSettings)) as JObject)
                .ToInputs()
                : null;

            var result = await _documentExecuter.ExecuteAsync(options =>
            {
                options.Schema = Schema;
                options.OperationName = deserializedRequest?.OperationName;
                options.Query = deserializedRequest?.Query;
                options.Inputs = inputs;
                options.CancellationToken = cancellationToken;
            }).ConfigureAwait(false);

            return result;
        }

        private async Task<GraphQLResponse<TResponse>> ExecutionResultToGraphQLResponseAsync<TResponse>(ExecutionResult executionResult, CancellationToken cancellationToken = default)
        {
            using var stream = new MemoryStream();
            await _documentWriter.WriteAsync(stream, executionResult, cancellationToken).ConfigureAwait(false);
            stream.Seek(0, SeekOrigin.Begin);
            return await Serializer.DeserializeFromUtf8StreamAsync<TResponse>(stream, cancellationToken).ConfigureAwait(false);
        }

        #endregion
    }
}
