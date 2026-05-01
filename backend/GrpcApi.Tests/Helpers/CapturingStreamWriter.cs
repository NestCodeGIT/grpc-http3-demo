using System.Collections.Concurrent;
using Grpc.Core;

namespace GrpcApi.Tests.Helpers;

/// <summary>
/// Captures every message written by a server-streaming RPC for assertion in tests.
/// </summary>
internal sealed class CapturingStreamWriter<T> : IServerStreamWriter<T>
{
    public ConcurrentQueue<T> Written { get; } = new();
    public WriteOptions? WriteOptions { get; set; }

    public Task WriteAsync(T message)
    {
        Written.Enqueue(message);
        return Task.CompletedTask;
    }
}
