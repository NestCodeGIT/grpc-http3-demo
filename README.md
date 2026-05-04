# grpc-http3-demo

<div align="center">

  <img src="docs/roadmap/roadmap.svg" alt="Project roadmap" width="100%">

  <p><em>A small full-stack playground I built to actually <strong>use</strong> gRPC server-streaming and HTTP/3 in anger — not just read about them on a slide deck.</em></p>

  <p>
    <a href="https://github.com/yourusername/grpc-http3-demo/actions/workflows/ci.yml">
      <img src="https://github.com/yourusername/grpc-http3-demo/actions/workflows/ci.yml/badge.svg" alt="CI">
    </a>
    <img src="https://img.shields.io/badge/.NET-10-512bd4?logo=dotnet&logoColor=white" alt=".NET 10">
    <img src="https://img.shields.io/badge/Angular-21-dd0031?logo=angular&logoColor=white" alt="Angular 21">
    <img src="https://img.shields.io/badge/gRPC-Web-00BCD4" alt="gRPC">
    <img src="https://img.shields.io/badge/HTTP%2F3-QUIC-4CAF50" alt="HTTP/3">
    <img src="https://img.shields.io/badge/tests-25%2F25-success" alt="Tests">
    <img src="https://img.shields.io/badge/license-MIT-blue" alt="MIT License">
  </p>

  <p>
    <a href="docs/roadmap/tutorial.md"><strong>Tutorial (DE)</strong></a>
    &nbsp;·&nbsp;
    <a href="docs/test-results.md"><strong>Test report</strong></a>
  </p>

</div>

---

## What is this?

I kept seeing job ads ask for "experience with gRPC and HTTP/3" but most public examples were either toy snippets or 2,000-line enterprise things. So I built something in between: a real, working full-stack app — Angular on the front, ASP.NET Core on the back, PostgreSQL underneath — that actually exercises the parts people care about:

- A **single `.proto` file** that generates both the C# server stub and the TypeScript client
- A **server-streaming RPC** that pushes live updates to the browser, no WebSockets, no polling
- **HTTP/3 over QUIC** end-to-end on Kestrel, verifiable in DevTools
- **Envoy** in the middle so the browser can speak gRPC-Web while the API speaks native gRPC
- Everything wired up so `docker compose up --build` is the only command you need to run

It's small enough to read in an afternoon and complete enough that I'd be comfortable showing it in an interview.

---

## Why bother with gRPC + HTTP/3?

REST over HTTP/1.1 is fine for a lot of things. It stops being fine the moment you need any of these:

| You want… | REST / HTTP/1.1 | This stack |
|---|---|---|
| A typed contract both sides agree on | OpenAPI, manually kept in sync | `.proto` → compiler errors on drift |
| Real-time push from the server | SSE or polling, both clunky | Native server-streaming RPC |
| Smaller payloads on mobile | JSON | Protobuf — usually 3–10× smaller |
| Faster reconnects, no head-of-line blocking | TCP | QUIC (UDP) with 0-RTT |
| Multiplexed requests | HTTP/2 only | HTTP/3 + QUIC |

If your API is just a few CRUD endpoints, REST is still the right answer. But the moment you reach for "we'll just poll every 2 seconds" or "let's add a WebSocket layer", you're already in territory where this stack pays for itself.

---

## How it fits together

```
┌───────────────────────────────┐
│  Browser — Angular 21         │
│  grpc-web client + Signals    │
└──────────────┬────────────────┘
               │  gRPC-Web (HTTP/1.1 or HTTP/2)
               ▼
┌───────────────────────────────┐
│  Envoy 1.29  :8080            │
│  gRPC-Web ↔ native gRPC       │
└──────────────┬────────────────┘
               │  gRPC over HTTP/2
               ▼
┌───────────────────────────────┐
│  ASP.NET Core 10 API  :5001   │
│  Kestrel: HTTP/1 + 2 + 3      │
│  Alt-Svc upgrades to QUIC     │
└──────────────┬────────────────┘
               │  EF Core (Npgsql)
               ▼
┌───────────────────────────────┐
│  PostgreSQL 16                │
└───────────────────────────────┘
```

The browser part is the awkward bit. Browsers can't speak native gRPC because JavaScript has no access to HTTP/2 trailers, so Envoy translates between gRPC-Web (what the browser can do) and gRPC (what the backend speaks). It's one config file. No code.

---

## Getting started

You need Docker, mkcert (for local TLS), and that's it. Everything else lives inside containers.

```bash
git clone https://github.com/yourusername/grpc-http3-demo.git
cd grpc-http3-demo

# Generate a locally-trusted cert — HTTP/3 won't work without HTTPS
bash certs/generate.sh

# Build and run the whole stack
docker compose up --build
```

Once it's up:

| Service | URL |
|---|---|
| Angular app | <http://localhost:4200> |
| gRPC API (HTTP/3) | <https://localhost:5001> |
| Envoy gRPC-Web bridge | <http://localhost:8080> |

### See HTTP/3 in action

1. Open <https://localhost:5001/healthz> in Chrome
2. DevTools → Network → enable the **Protocol** column
3. The first request shows `h2`, the next ones show **`h3`** — that's the `Alt-Svc` header doing its job

### See server-streaming in action

1. Open <http://localhost:4200/stream> and click **Start Stream**
2. In another tab, create a task on the list page
3. Within ~2 seconds it shows up at the top of the live feed

---

## Running things without Docker

### Backend

```bash
cd backend/GrpcApi
docker run -d -e POSTGRES_PASSWORD=postgres -e POSTGRES_DB=grpc_tasks \
  -p 5432:5432 postgres:16-alpine
dotnet run
```

> Tip: prefix `dotnet` commands with `DOTNET_SYSTEM_NET_DISABLEIPV6=1` if `restore` hangs — it's a known issue on some Linux setups.

### Frontend

```bash
cd frontend
npm ci             # the prebuild hook generates the gRPC-Web client
npm start          # https://localhost:4200
```

If you don't have `protoc` and `protoc-gen-grpc-web` on your machine, just use the Docker path — the frontend image installs them for you.

---

## The API at a glance

A single `.proto` file is the source of truth. All six methods live there:

| RPC | Type | What it does |
|---|---|---|
| `GetTask` | unary | fetch one task by id |
| `ListTasks` | unary | paginated list |
| `CreateTask` | unary | adds a task |
| `UpdateTask` | unary | mutates title/status |
| `DeleteTask` | unary | idempotent delete |
| `StreamTasks` | **server-streaming** | pushes the latest tasks every 2 s |

Statuses are a small enum: `pending`, `in_progress`, `done`, `cancelled`.

---

## A couple of things I learned the hard way

These are the bugs that actually happened to me. They're worth knowing about because the docs don't shout about them.

**`responseStream.WriteAsync(message, cancellationToken)` throws at runtime.** The two-argument overload isn't supported in the in-process gRPC server. Use the single-arg version and check `context.CancellationToken.IsCancellationRequested` yourself. The tests in this repo catch this regression now.

**Casting `IQueryable` to `IOrderedQueryable` after `.Where(...)` blows up.** Order matters: filter first, then sort. Looks fine in the IDE, throws at runtime. Again — caught by a test.

**Don't forget to expose UDP in `docker-compose.yml`.** Port `5001/tcp` *and* `5001/udp`. Without UDP, QUIC silently falls back to HTTP/2 and you'll wonder why DevTools never shows `h3`.

**Fresh `dotnet restore` can hang for minutes on Linux.** Set `DOTNET_SYSTEM_NET_DISABLEIPV6=1`. That's a Microsoft NuGet/IPv6 issue, not a code issue.

---

## How the project is laid out

```
grpc-http3-demo/
├── backend/
│   ├── GrpcApi/
│   │   ├── Protos/task.proto         ← single source of truth
│   │   ├── Services/TaskGrpcService.cs
│   │   ├── Data/{AppDbContext,TaskEntity}.cs
│   │   └── Program.cs                ← HTTP/3 Kestrel config lives here
│   ├── GrpcApi.Tests/                ← xUnit, 25 tests, ~0.8 s
│   └── Dockerfile
├── frontend/
│   ├── src/app/
│   │   ├── core/grpc/                ← generated client + service wrapper
│   │   └── features/tasks/           ← list (unary) + stream (streaming)
│   └── Dockerfile
├── proxy/envoy.yaml                   ← gRPC-Web ↔ gRPC bridge
├── certs/generate.sh                  ← mkcert helper
├── docs/
│   ├── test-results.md
│   └── roadmap/{roadmap.svg,tutorial.md}
├── docker-compose.yml
└── .github/workflows/ci.yml
```

---

## A taste of the code

**Turning on HTTP/3** in `Program.cs` is genuinely a one-liner:

```csharp
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5001, listen =>
    {
        listen.UseHttps();
        listen.Protocols = HttpProtocols.Http1AndHttp2AndHttp3;
    });
});
```

**The streaming RPC.** No heartbeats, no reconnect logic — gRPC handles all of it for you:

```csharp
public override async Task StreamTasks(
    StreamTasksRequest request,
    IServerStreamWriter<TaskResponse> stream,
    ServerCallContext context)
{
    while (!context.CancellationToken.IsCancellationRequested)
    {
        var tasks = await _db.Tasks
            .Where(t => string.IsNullOrEmpty(request.FilterStatus) || t.Status == request.FilterStatus)
            .OrderByDescending(t => t.UpdatedAt)
            .Take(10)
            .ToListAsync();

        foreach (var t in tasks)
            await stream.WriteAsync(Map(t));

        await Task.Delay(TimeSpan.FromSeconds(2), context.CancellationToken);
    }
}
```

**Consuming the stream in Angular** with Signals — no RxJS gymnastics needed:

```typescript
readonly streamedTasks = signal<Task[]>([]);

startStream(filter = ''): void {
  const stream = this.client.streamTasks({ filterStatus: filter }, {});
  stream.on('data', resp => {
    this.streamedTasks.update(curr => [this.toTask(resp), ...curr].slice(0, 50));
  });
}
```

---

## Tests

There's a dedicated test project at `backend/GrpcApi.Tests/`. It covers all six RPCs (yes, including the streaming one) without booting Kestrel.

| | |
|---|---|
| Test framework | xUnit + FluentAssertions |
| Database | EF Core InMemory, fresh per test |
| Tests | **25 / 25 passing** |
| Duration | ~0.8 s |
| `TaskGrpcService` line + branch coverage | **100 %** |

```bash
cd backend
DOTNET_SYSTEM_NET_DISABLEIPV6=1 \
  dotnet test GrpcApi.Tests/GrpcApi.Tests.csproj -c Release \
    --collect:"XPlat Code Coverage"
```

The trick to testing gRPC without a real server is three small helpers:

- `TestServerCallContext` — a stand-in `ServerCallContext` so you can call RPC methods directly
- `CapturingStreamWriter<T>` — collects every message a streaming RPC writes, so you can assert against it
- `TestDb.Create()` — a factory for isolated InMemory databases, one per test

Full breakdown in [`docs/test-results.md`](docs/test-results.md).

---

## Tech stack

| Layer | What I used |
|---|---|
| Backend | .NET 10, ASP.NET Core, `Grpc.AspNetCore` 2.70 |
| Transport | Kestrel + `System.Net.Quic` (libmsquic) |
| Database | PostgreSQL 16, EF Core 10, Npgsql |
| Proxy | Envoy 1.29 (gRPC-Web filter) |
| Frontend | Angular 21 (standalone components + Signals), TypeScript 5.9 |
| gRPC client | `grpc-web` 1.5 + `protoc-gen-grpc-web` |
| Containers | Docker + Docker Compose |
| CI | GitHub Actions (build, test, coverage, Docker images) |

---

## Suggested GitHub topics

If you fork or star this repo, these are the topics I'd recommend tagging it with so it shows up in the right searches:

`dotnet` · `dotnet10` · `aspnetcore` · `grpc` · `grpc-web` · `http3` · `quic` · `angular` · `angular21` · `typescript` · `protobuf` · `envoy` · `postgresql` · `docker` · `streaming` · `realtime`

---

## License

[MIT](LICENSE) — do whatever you want with it. If it helps you land an interview or ship something cool, that's already the best outcome I could hope for.
