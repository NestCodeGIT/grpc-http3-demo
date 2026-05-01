using Grpc.Core;
using GrpcApi.Data;
using Microsoft.EntityFrameworkCore;

namespace GrpcApi.Services;

public class TaskGrpcService(AppDbContext db, ILogger<TaskGrpcService> logger) : TaskService.TaskServiceBase
{
    public override async Task<TaskResponse> GetTask(GetTaskRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.Id, out var id))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid task ID format"));

        var task = await db.Tasks.FindAsync(id)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Task {request.Id} not found"));

        return MapToResponse(task);
    }

    public override async Task<ListTasksResponse> ListTasks(ListTasksRequest request, ServerCallContext context)
    {
        var pageSize = request.PageSize > 0 ? Math.Min(request.PageSize, 100) : 20;
        var page = request.Page > 0 ? request.Page - 1 : 0;

        var tasks = await db.Tasks
            .OrderByDescending(t => t.CreatedAt)
            .Skip(page * pageSize)
            .Take(pageSize)
            .ToListAsync(context.CancellationToken);

        var response = new ListTasksResponse();
        response.Tasks.AddRange(tasks.Select(MapToResponse));
        return response;
    }

    public override async Task<TaskResponse> CreateTask(CreateTaskRequest request, ServerCallContext context)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Title is required"));

        var entity = new TaskEntity
        {
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            Status = "pending"
        };

        db.Tasks.Add(entity);
        await db.SaveChangesAsync(context.CancellationToken);

        logger.LogInformation("Created task {Id}: {Title}", entity.Id, entity.Title);
        return MapToResponse(entity);
    }

    public override async Task<TaskResponse> UpdateTask(UpdateTaskRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.Id, out var id))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid task ID format"));

        var task = await db.Tasks.FindAsync(id)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Task {request.Id} not found"));

        var validStatuses = new[] { "pending", "in_progress", "done", "cancelled" };
        if (!string.IsNullOrEmpty(request.Status) && !validStatuses.Contains(request.Status))
            throw new RpcException(new Status(StatusCode.InvalidArgument, $"Invalid status. Must be one of: {string.Join(", ", validStatuses)}"));

        if (!string.IsNullOrEmpty(request.Title))
            task.Title = request.Title.Trim();
        if (!string.IsNullOrEmpty(request.Status))
            task.Status = request.Status;
        task.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(context.CancellationToken);

        logger.LogInformation("Updated task {Id}", task.Id);
        return MapToResponse(task);
    }

    public override async Task<DeleteTaskResponse> DeleteTask(DeleteTaskRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.Id, out var id))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid task ID format"));

        var task = await db.Tasks.FindAsync(id);
        if (task is null)
            return new DeleteTaskResponse { Success = false, Message = "Task not found" };

        db.Tasks.Remove(task);
        await db.SaveChangesAsync(context.CancellationToken);

        logger.LogInformation("Deleted task {Id}", id);
        return new DeleteTaskResponse { Success = true, Message = "Task deleted successfully" };
    }

    // Server-streaming RPC — pushes live task updates every 2 seconds
    public override async Task StreamTasks(StreamTasksRequest request, IServerStreamWriter<TaskResponse> responseStream, ServerCallContext context)
    {
        logger.LogInformation("Client started streaming tasks (filter: '{Filter}')", request.FilterStatus);

        while (!context.CancellationToken.IsCancellationRequested)
        {
            try
            {
                IQueryable<TaskEntity> query = db.Tasks.AsNoTracking();

                if (!string.IsNullOrEmpty(request.FilterStatus))
                    query = query.Where(t => t.Status == request.FilterStatus);

                var tasks = await query
                    .OrderByDescending(t => t.UpdatedAt)
                    .Take(10)
                    .ToListAsync(context.CancellationToken);

                foreach (var task in tasks)
                {
                    context.CancellationToken.ThrowIfCancellationRequested();
                    await responseStream.WriteAsync(MapToResponse(task));
                }

                await Task.Delay(TimeSpan.FromSeconds(2), context.CancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        logger.LogInformation("Client disconnected from task stream");
    }

    private static TaskResponse MapToResponse(TaskEntity e) => new()
    {
        Id = e.Id.ToString(),
        Title = e.Title,
        Description = e.Description,
        Status = e.Status,
        CreatedAt = e.CreatedAt.ToString("O"),
        UpdatedAt = e.UpdatedAt.ToString("O")
    };
}
