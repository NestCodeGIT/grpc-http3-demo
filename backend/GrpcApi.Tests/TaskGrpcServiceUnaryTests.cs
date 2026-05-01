using FluentAssertions;
using Grpc.Core;
using GrpcApi;
using GrpcApi.Data;
using GrpcApi.Services;
using GrpcApi.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;

namespace GrpcApi.Tests;

public class TaskGrpcServiceUnaryTests
{
    private static TaskGrpcService NewService(out AppDbContext db)
    {
        db = TestDb.Create();
        return new TaskGrpcService(db, NullLogger<TaskGrpcService>.Instance);
    }

    // ---------- CreateTask ----------

    [Fact]
    public async Task CreateTask_persists_entity_and_returns_it_with_pending_status()
    {
        var svc = NewService(out var db);

        var resp = await svc.CreateTask(
            new CreateTaskRequest { Title = "  Buy milk  ", Description = "  2L  " },
            TestServerCallContext.Create());

        resp.Should().NotBeNull();
        resp.Title.Should().Be("Buy milk", "title should be trimmed");
        resp.Description.Should().Be("2L", "description should be trimmed");
        resp.Status.Should().Be("pending");
        Guid.TryParse(resp.Id, out _).Should().BeTrue();

        db.Tasks.Should().ContainSingle(t => t.Title == "Buy milk");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task CreateTask_throws_InvalidArgument_when_title_is_blank(string? title)
    {
        var svc = NewService(out _);

        var act = async () => await svc.CreateTask(
            new CreateTaskRequest { Title = title ?? "", Description = "x" },
            TestServerCallContext.Create());

        var ex = await act.Should().ThrowAsync<RpcException>();
        ex.Which.StatusCode.Should().Be(StatusCode.InvalidArgument);
    }

    // ---------- GetTask ----------

    [Fact]
    public async Task GetTask_returns_existing_task()
    {
        var svc = NewService(out var db);
        var entity = new TaskEntity { Title = "Read", Description = "Book", Status = "pending" };
        db.Tasks.Add(entity);
        await db.SaveChangesAsync();

        var resp = await svc.GetTask(
            new GetTaskRequest { Id = entity.Id.ToString() },
            TestServerCallContext.Create());

        resp.Id.Should().Be(entity.Id.ToString());
        resp.Title.Should().Be("Read");
    }

    [Fact]
    public async Task GetTask_throws_NotFound_for_missing_id()
    {
        var svc = NewService(out _);
        var missing = Guid.NewGuid().ToString();

        var act = async () => await svc.GetTask(
            new GetTaskRequest { Id = missing },
            TestServerCallContext.Create());

        var ex = await act.Should().ThrowAsync<RpcException>();
        ex.Which.StatusCode.Should().Be(StatusCode.NotFound);
    }

    [Fact]
    public async Task GetTask_throws_InvalidArgument_for_malformed_id()
    {
        var svc = NewService(out _);

        var act = async () => await svc.GetTask(
            new GetTaskRequest { Id = "not-a-guid" },
            TestServerCallContext.Create());

        (await act.Should().ThrowAsync<RpcException>()).Which.StatusCode.Should().Be(StatusCode.InvalidArgument);
    }

    // ---------- ListTasks ----------

    [Fact]
    public async Task ListTasks_returns_newest_first_and_respects_paging()
    {
        var svc = NewService(out var db);
        var now = DateTime.UtcNow;
        for (var i = 0; i < 25; i++)
        {
            db.Tasks.Add(new TaskEntity
            {
                Title = $"Task {i:D2}",
                Status = "pending",
                CreatedAt = now.AddMinutes(i),
                UpdatedAt = now.AddMinutes(i)
            });
        }
        await db.SaveChangesAsync();

        var page1 = await svc.ListTasks(
            new ListTasksRequest { Page = 1, PageSize = 10 },
            TestServerCallContext.Create());
        var page2 = await svc.ListTasks(
            new ListTasksRequest { Page = 2, PageSize = 10 },
            TestServerCallContext.Create());

        page1.Tasks.Should().HaveCount(10);
        page1.Tasks[0].Title.Should().Be("Task 24", "newest must come first");
        page2.Tasks.Should().HaveCount(10);
        page2.Tasks[0].Title.Should().Be("Task 14");
    }

    [Fact]
    public async Task ListTasks_clamps_page_size_to_100()
    {
        var svc = NewService(out var db);
        for (var i = 0; i < 150; i++)
            db.Tasks.Add(new TaskEntity { Title = $"T{i}", Status = "pending" });
        await db.SaveChangesAsync();

        var resp = await svc.ListTasks(
            new ListTasksRequest { Page = 1, PageSize = 500 },
            TestServerCallContext.Create());

        resp.Tasks.Should().HaveCount(100);
    }

    [Fact]
    public async Task ListTasks_uses_default_page_size_when_unspecified()
    {
        var svc = NewService(out var db);
        for (var i = 0; i < 30; i++)
            db.Tasks.Add(new TaskEntity { Title = $"T{i}", Status = "pending" });
        await db.SaveChangesAsync();

        var resp = await svc.ListTasks(new ListTasksRequest(), TestServerCallContext.Create());

        resp.Tasks.Should().HaveCount(20);
    }

    // ---------- UpdateTask ----------

    [Theory]
    [InlineData("pending")]
    [InlineData("in_progress")]
    [InlineData("done")]
    [InlineData("cancelled")]
    public async Task UpdateTask_accepts_all_valid_statuses(string status)
    {
        var svc = NewService(out var db);
        var entity = new TaskEntity { Title = "x", Status = "pending" };
        db.Tasks.Add(entity);
        await db.SaveChangesAsync();

        var resp = await svc.UpdateTask(
            new UpdateTaskRequest { Id = entity.Id.ToString(), Status = status },
            TestServerCallContext.Create());

        resp.Status.Should().Be(status);
    }

    [Fact]
    public async Task UpdateTask_rejects_unknown_status()
    {
        var svc = NewService(out var db);
        var entity = new TaskEntity { Title = "x", Status = "pending" };
        db.Tasks.Add(entity);
        await db.SaveChangesAsync();

        var act = async () => await svc.UpdateTask(
            new UpdateTaskRequest { Id = entity.Id.ToString(), Status = "exploded" },
            TestServerCallContext.Create());

        (await act.Should().ThrowAsync<RpcException>()).Which.StatusCode.Should().Be(StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task UpdateTask_advances_UpdatedAt_timestamp()
    {
        var svc = NewService(out var db);
        var earlier = DateTime.UtcNow.AddMinutes(-10);
        var entity = new TaskEntity { Title = "x", Status = "pending", CreatedAt = earlier, UpdatedAt = earlier };
        db.Tasks.Add(entity);
        await db.SaveChangesAsync();

        await svc.UpdateTask(
            new UpdateTaskRequest { Id = entity.Id.ToString(), Title = "renamed" },
            TestServerCallContext.Create());

        var reloaded = await db.Tasks.FindAsync(entity.Id);
        reloaded!.Title.Should().Be("renamed");
        reloaded.UpdatedAt.Should().BeAfter(earlier);
    }

    [Fact]
    public async Task UpdateTask_throws_NotFound_for_missing_id()
    {
        var svc = NewService(out _);

        var act = async () => await svc.UpdateTask(
            new UpdateTaskRequest { Id = Guid.NewGuid().ToString(), Status = "done" },
            TestServerCallContext.Create());

        (await act.Should().ThrowAsync<RpcException>()).Which.StatusCode.Should().Be(StatusCode.NotFound);
    }

    // ---------- DeleteTask ----------

    [Fact]
    public async Task DeleteTask_removes_entity_and_returns_success()
    {
        var svc = NewService(out var db);
        var entity = new TaskEntity { Title = "x", Status = "pending" };
        db.Tasks.Add(entity);
        await db.SaveChangesAsync();

        var resp = await svc.DeleteTask(
            new DeleteTaskRequest { Id = entity.Id.ToString() },
            TestServerCallContext.Create());

        resp.Success.Should().BeTrue();
        db.Tasks.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteTask_returns_success_false_when_missing_instead_of_throwing()
    {
        var svc = NewService(out _);

        var resp = await svc.DeleteTask(
            new DeleteTaskRequest { Id = Guid.NewGuid().ToString() },
            TestServerCallContext.Create());

        resp.Success.Should().BeFalse();
        resp.Message.Should().Contain("not found");
    }

    [Fact]
    public async Task DeleteTask_throws_InvalidArgument_for_malformed_id()
    {
        var svc = NewService(out _);

        var act = async () => await svc.DeleteTask(
            new DeleteTaskRequest { Id = "not-a-guid" },
            TestServerCallContext.Create());

        (await act.Should().ThrowAsync<RpcException>()).Which.StatusCode.Should().Be(StatusCode.InvalidArgument);
    }
}
