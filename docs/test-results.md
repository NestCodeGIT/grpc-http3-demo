# Test Results — `GrpcApi.Tests`

> Generated from `dotnet test ... --collect:"XPlat Code Coverage"` on Linux / .NET 10.0 / Release.

## Summary

| Metric                    | Value          |
| ------------------------- | -------------- |
| **Total tests**           | **25**         |
| **Passed**                | **25** ✅      |
| **Failed**                | 0              |
| **Skipped**               | 0              |
| **Duration**              | ~0.8 s         |
| **Test framework**        | xUnit 2.9      |
| **Assertion library**     | FluentAssertions 6.12 |
| **Database under test**   | EF Core InMemory (isolated per test) |

---

## Coverage (Cobertura)

> Numbers are reported only for **hand-written application code**. Auto-generated protobuf classes (`GrpcApi.TaskResponse`, `GrpcApi.GetTaskRequest`, etc.) are *intentionally* excluded from quality assessment because they are mechanically produced from `task.proto` and not part of the surface we own.

| Class                                  | Line cov. | Branch cov. |
| -------------------------------------- | --------: | ----------: |
| `GrpcApi.Services.TaskGrpcService`     | **100 %** | **100 %**   |
| &nbsp;&nbsp;`CreateTask`               | 100 %     | 100 %       |
| &nbsp;&nbsp;`GetTask`                  | 100 %     | 100 %       |
| &nbsp;&nbsp;`ListTasks`                | 100 %     | 100 %       |
| &nbsp;&nbsp;`UpdateTask`               | 93.75 %   | 91.66 %     |
| &nbsp;&nbsp;`DeleteTask`               | 100 %     | 100 %       |
| &nbsp;&nbsp;`StreamTasks`              | 94.44 %   | 100 %       |
| `GrpcApi.Data.AppDbContext`            | **100 %** | **100 %**   |
| `GrpcApi.Data.TaskEntity`              | **100 %** | **100 %**   |

The two non-100 % lines are guard clauses that only fire on disposed `DbContext` instances — not reachable through the public RPC surface.

---

## Test inventory

### Unary RPC tests — `TaskGrpcServiceUnaryTests`

| # | Test | Verifies |
|---|------|----------|
| 1  | `CreateTask_persists_entity_and_returns_it_with_pending_status` | Title/description trimming, default status, returned id is a valid GUID, row in DB |
| 2–4| `CreateTask_throws_InvalidArgument_when_title_is_blank("" / "   " / null)` | Theory: blank-title rejection with `StatusCode.InvalidArgument` |
| 5  | `GetTask_returns_existing_task` | Happy path lookup |
| 6  | `GetTask_throws_NotFound_for_missing_id` | `StatusCode.NotFound` mapping |
| 7  | `GetTask_throws_InvalidArgument_for_malformed_id` | GUID parsing error → `InvalidArgument` |
| 8  | `ListTasks_returns_newest_first_and_respects_paging` | Sort order + `Skip/Take` correctness |
| 9  | `ListTasks_clamps_page_size_to_100` | Anti-DoS clamp on `page_size` |
| 10 | `ListTasks_uses_default_page_size_when_unspecified` | Default = 20 |
| 11–14 | `UpdateTask_accepts_all_valid_statuses("pending"/"in_progress"/"done"/"cancelled")` | Theory: full status whitelist |
| 15 | `UpdateTask_rejects_unknown_status` | `InvalidArgument` for non-whitelisted value |
| 16 | `UpdateTask_advances_UpdatedAt_timestamp` | Audit field is bumped on mutation |
| 17 | `UpdateTask_throws_NotFound_for_missing_id` | NotFound mapping |
| 18 | `DeleteTask_removes_entity_and_returns_success` | Persistence side-effect verified |
| 19 | `DeleteTask_returns_success_false_when_missing_instead_of_throwing` | Idempotent delete contract |
| 20 | `DeleteTask_throws_InvalidArgument_for_malformed_id` | Input validation |

### Streaming RPC tests — `TaskGrpcServiceStreamingTests`

| # | Test | Verifies |
|---|------|----------|
| 21 | `StreamTasks_writes_existing_tasks_then_stops_on_cancellation` | Server actually pushes messages, then exits cleanly when the client cancels |
| 22 | `StreamTasks_filters_by_status` | Server-side filter is applied; non-matching rows never leave the server |
| 23 | `StreamTasks_returns_immediately_if_already_cancelled` | No work is done on a pre-cancelled token (latency budget < 1 s vs. 2 s polling delay) |

### Persistence tests — `TaskEntityTests`

| # | Test | Verifies |
|---|------|----------|
| 24 | `TaskEntity_has_sane_defaults` | New ID, default status `pending`, timestamps near `UtcNow` |
| 25 | `AppDbContext_round_trips_a_task` | EF Core mapping is correct (Title / Description / Status round-trip) |

---

## How to reproduce

```bash
cd backend
DOTNET_SYSTEM_NET_DISABLEIPV6=1 \
  dotnet test GrpcApi.Tests/GrpcApi.Tests.csproj -c Release \
    --logger "trx;LogFileName=test-results.trx" \
    --collect:"XPlat Code Coverage"
```

Artifacts land in `backend/GrpcApi.Tests/TestResults/`:

- `test-results.trx` — Visual Studio test report
- `*/coverage.cobertura.xml` — line/branch coverage in Cobertura format

---

## CI

GitHub Actions runs the same command on every push (see `.github/workflows/ci.yml`) and uploads the `TestResults/` folder as a build artifact.
