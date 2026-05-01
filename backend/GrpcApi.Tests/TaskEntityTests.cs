using FluentAssertions;
using GrpcApi.Data;
using GrpcApi.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace GrpcApi.Tests;

public class TaskEntityTests
{
    [Fact]
    public void TaskEntity_has_sane_defaults()
    {
        var t = new TaskEntity();

        t.Id.Should().NotBeEmpty();
        t.Status.Should().Be("pending");
        t.Title.Should().BeEmpty();
        t.Description.Should().BeEmpty();
        t.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        t.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task AppDbContext_round_trips_a_task()
    {
        await using var db = TestDb.Create();
        var entity = new TaskEntity { Title = "Hello", Description = "World", Status = "in_progress" };
        db.Tasks.Add(entity);
        await db.SaveChangesAsync();

        var loaded = await db.Tasks.AsNoTracking().FirstAsync(t => t.Id == entity.Id);
        loaded.Title.Should().Be("Hello");
        loaded.Description.Should().Be("World");
        loaded.Status.Should().Be("in_progress");
    }
}
