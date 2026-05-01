using GrpcApi.Data;
using Microsoft.EntityFrameworkCore;

namespace GrpcApi.Tests.Helpers;

internal static class TestDb
{
    /// <summary>
    /// Creates a fresh in-memory <see cref="AppDbContext"/> with a unique database name so
    /// tests cannot leak state into each other.
    /// </summary>
    public static AppDbContext Create()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"grpc-tests-{Guid.NewGuid()}")
            .ConfigureWarnings(b => b.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new AppDbContext(options);
    }
}
