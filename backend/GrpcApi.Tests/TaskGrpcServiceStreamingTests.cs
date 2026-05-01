using FluentAssertions;
using GrpcApi;
using GrpcApi.Data;
using GrpcApi.Services;
using GrpcApi.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;

namespace GrpcApi.Tests;

public class TaskGrpcServiceStreamingTests
{
    [Fact]
    public async Task StreamTasks_writes_existing_tasks_then_stops_on_cancellation()
    {
        var db = TestDb.Create();
        for (var i = 0; i < 3; i++)
            db.Tasks.Add(new TaskEntity { Title = $"T{i}", Status = "pending" });
        await db.SaveChangesAsync();

        var svc = new TaskGrpcService(db, NullLogger<TaskGrpcService>.Instance);
        var writer = new CapturingStreamWriter<TaskResponse>();
        using var cts = new CancellationTokenSource();
        var ctx = TestServerCallContext.Create(cts.Token);

        var streamTask = svc.StreamTasks(new StreamTasksRequest(), writer, ctx);

        // Wait until the first batch is written (server delays 2s between batches)
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (writer.Written.Count < 3 && DateTime.UtcNow < deadline)
            await Task.Delay(50);

        cts.Cancel();
        await streamTask; // service swallows OCE and returns

        writer.Written.Should().HaveCountGreaterThanOrEqualTo(3);
        writer.Written.Select(t => t.Title).Should().Contain(new[] { "T0", "T1", "T2" });
    }

    [Fact]
    public async Task StreamTasks_filters_by_status()
    {
        var db = TestDb.Create();
        db.Tasks.Add(new TaskEntity { Title = "A", Status = "pending" });
        db.Tasks.Add(new TaskEntity { Title = "B", Status = "done" });
        db.Tasks.Add(new TaskEntity { Title = "C", Status = "done" });
        await db.SaveChangesAsync();

        var svc = new TaskGrpcService(db, NullLogger<TaskGrpcService>.Instance);
        var writer = new CapturingStreamWriter<TaskResponse>();
        using var cts = new CancellationTokenSource();
        var ctx = TestServerCallContext.Create(cts.Token);

        var streamTask = svc.StreamTasks(
            new StreamTasksRequest { FilterStatus = "done" }, writer, ctx);

        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (writer.Written.Count < 2 && DateTime.UtcNow < deadline)
            await Task.Delay(50);

        cts.Cancel();
        await streamTask;

        writer.Written.Select(t => t.Title).Should().OnlyContain(t => t == "B" || t == "C");
        writer.Written.Should().NotContain(t => t.Title == "A");
    }

    [Fact]
    public async Task StreamTasks_returns_immediately_if_already_cancelled()
    {
        var db = TestDb.Create();
        var svc = new TaskGrpcService(db, NullLogger<TaskGrpcService>.Instance);
        var writer = new CapturingStreamWriter<TaskResponse>();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var ctx = TestServerCallContext.Create(cts.Token);

        // Should complete promptly (well under the 2s inter-batch delay)
        var sw = System.Diagnostics.Stopwatch.StartNew();
        await svc.StreamTasks(new StreamTasksRequest(), writer, ctx);
        sw.Stop();

        sw.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(1));
    }
}
