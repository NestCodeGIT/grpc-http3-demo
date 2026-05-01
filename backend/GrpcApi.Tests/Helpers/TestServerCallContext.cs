using System.Threading;
using Grpc.Core;

namespace GrpcApi.Tests.Helpers;

/// <summary>
/// Minimal in-memory <see cref="ServerCallContext"/> for unit-testing gRPC service methods
/// without spinning up a full Kestrel host.
/// </summary>
internal sealed class TestServerCallContext : ServerCallContext
{
    private readonly CancellationToken _ct;
    private readonly Metadata _responseTrailers = new();
    private readonly Metadata _requestHeaders = new();
    private Status _status;
    private WriteOptions? _writeOptions;

    public TestServerCallContext(CancellationToken ct = default) => _ct = ct;

    public static TestServerCallContext Create(CancellationToken ct = default) => new(ct);

    protected override string MethodCore => "TestMethod";
    protected override string HostCore => "localhost";
    protected override string PeerCore => "test";
    protected override DateTime DeadlineCore => DateTime.MaxValue;
    protected override Metadata RequestHeadersCore => _requestHeaders;
    protected override CancellationToken CancellationTokenCore => _ct;
    protected override Metadata ResponseTrailersCore => _responseTrailers;
    protected override Status StatusCore { get => _status; set => _status = value; }
    protected override WriteOptions? WriteOptionsCore { get => _writeOptions; set => _writeOptions = value; }
    protected override AuthContext AuthContextCore => new("anonymous", new Dictionary<string, List<AuthProperty>>());

    protected override Task WriteResponseHeadersAsyncCore(Metadata responseHeaders) => Task.CompletedTask;

    protected override ContextPropagationToken CreatePropagationTokenCore(ContextPropagationOptions? options) =>
        throw new NotSupportedException();
}
