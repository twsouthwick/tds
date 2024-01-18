using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Protocols.Tds.Features;
using System.Buffers;
using System.Data;

namespace Microsoft.Protocols.Tds;

public sealed class TdsConnection : IAsyncDisposable, IDisposable
{
    private readonly TdsConnectionContext _context;

    public TdsConnection(TdsConnectionContext context)
    {
        _context = context;
    }

    public ValueTask DisposeAsync() => _context.Features.GetRequiredFeature<IConnectionOpenFeature>().DisposeAsync();

    public Task<IDataReader> ExecuteAsync(string v) => throw new NotImplementedException();

    void IDisposable.Dispose()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
    }
}

public sealed class TdsConnectionPool
{
    private readonly TdsConnectionDelegate _next;

    private TdsConnectionPool(TdsConnectionDelegate next)
    {
        _next = next;
    }

    public static TdsConnectionPool Create(Action<ITdsConnectionBuilder> builder)
    {
        var b = TdsConnectionBuilder.Create();

        builder(b);

        return new(b.Build());
    }

    public static TdsConnectionPool Create(IServiceProvider services, Action<ITdsConnectionBuilder> builder)
    {
        var b = TdsConnectionBuilder.Create(services);

        builder(b);

        return new(b.Build());
    }

    public async ValueTask<TdsConnection> OpenAsync(string connectionString)
    {
        var context = new OpenContext(_next, connectionString);

        await context.StartAsync();

        return new(context);
    }

    private sealed class OpenContext : DefaultTdsConnectionContext
    {
        private readonly TdsConnectionDelegate _next;
        private Task? _task;

        public OpenContext(TdsConnectionDelegate tdsConnection, string connectionString)
            : base(tdsConnection, connectionString)
        {
            _next = tdsConnection;
        }

        public ValueTask StartAsync()
        {
            // todo handle dispose
            var opened = new ConnectionOpenFeature();

            Features.Set<IConnectionOpenFeature>(opened);
            Features.Set<IAbortFeature>(opened);

            _task = _next(this).AsTask();

            opened.Middleware = _task;

            return opened.WaitForInitializedAsync();
        }

        private sealed class ConnectionOpenFeature : IConnectionOpenFeature, IAbortFeature, IDisposable
        {
            private readonly TaskCompletionSource<bool> _tcs = new();
            private readonly CancellationTokenSource _cts = new();

            public Task? Middleware { get; set; }

            CancellationToken IAbortFeature.Token => _cts.Token;

            bool IConnectionOpenFeature.IsOpened => _tcs.Task.IsCompleted;

            public void Dispose()
            {
                _cts.Dispose();
            }

            void IAbortFeature.Abort()
            {
                _tcs.SetCanceled();
                _cts.Cancel();
            }

            void IConnectionOpenFeature.Initialized() => _tcs.SetResult(true);

            public async ValueTask WaitForInitializedAsync()
            {
                await _tcs.Task;
            }

            async ValueTask IConnectionOpenFeature.DisposeAsync()
            {
                if (Middleware is { } m)
                {
                    await m;
                }
            }
        }
    }

    private sealed class TdsConnectionBuilder : ITdsConnectionBuilder
    {
        private readonly List<Func<TdsConnectionDelegate, TdsConnectionDelegate>> _components = new();

        private TdsConnectionBuilder(IServiceProvider? provider)
        {
            Properties = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                { nameof(Services),  new WrappedProvider(provider) },
            };
        }

        private TdsConnectionBuilder(IDictionary<string, object?> properties)
        {
            Properties = new CopyOnWriteDictionary<string, object?>(properties, StringComparer.Ordinal);
        }

        public IServiceProvider Services => Properties.TryGetValue(nameof(Services), out var result) && result is IServiceProvider services ? services : throw new InvalidOperationException();

        public IDictionary<string, object?> Properties { get; }

        public static ITdsConnectionBuilder Create(IServiceProvider? provider = null) => new TdsConnectionBuilder(provider);

        private static readonly TdsConnectionDelegate _connection = context =>
        {
            context.Features.GetRequiredFeature<IConnectionOpenFeature>().Initialized();
            return default;
        };

        public TdsConnectionDelegate Build()
        {
            var connection = _connection;

            for (var c = _components.Count - 1; c >= 0; c--)
            {
                connection = _components[c](connection);
            }

            return connection;
        }

        public ITdsConnectionBuilder New() => new TdsConnectionBuilder(Properties);

        public ITdsConnectionBuilder Use(Func<TdsConnectionDelegate, TdsConnectionDelegate> middleware)
        {
            _components.Add(middleware);
            return this;
        }

        private sealed class WrappedProvider : IServiceProvider, IPooledObjectPolicy<ArrayBufferWriter<byte>>
        {
            private readonly IServiceProvider? _other;
            private readonly DefaultObjectPool<ArrayBufferWriter<byte>> _pool;

            public WrappedProvider(IServiceProvider? other)
            {
                _other = other;
                _pool = new DefaultObjectPool<ArrayBufferWriter<byte>>(this);
            }

            object? IServiceProvider.GetService(Type serviceType)
            {
                if (_other?.GetService(serviceType) is { } existing)
                {
                    return existing;
                }

                if (serviceType == typeof(ObjectPool<ArrayBufferWriter<byte>>))
                {
                    return _pool;
                }

                return null;
            }

            ArrayBufferWriter<byte> IPooledObjectPolicy<ArrayBufferWriter<byte>>.Create()
                => new ArrayBufferWriter<byte>();

            bool IPooledObjectPolicy<ArrayBufferWriter<byte>>.Return(ArrayBufferWriter<byte> obj)
            {
                obj.ResetWrittenCount();
                return true;
            }
        }
    }
}
