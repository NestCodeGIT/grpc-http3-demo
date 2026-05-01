# grpc-http3-demo

<div align="center">

  <img src="docs/roadmap/roadmap.svg" alt="Project roadmap" width="100%">

  <h3>A production-grade full-stack application demonstrating <strong>gRPC server-streaming</strong> and <strong>HTTP/3 (QUIC)</strong> transport — built with ASP.NET Core 8 and Angular 17.</h3>

  <p>
    <a href="https://github.com/yourusername/grpc-http3-demo/actions/workflows/ci.yml">
      <img src="https://github.com/yourusername/grpc-http3-demo/actions/workflows/ci.yml/badge.svg" alt="CI">
    </a>
    <img src="https://img.shields.io/badge/.NET-8.0-512bd4?logo=dotnet&logoColor=white" alt=".NET 8">
    <img src="https://img.shields.io/badge/Angular-17-dd0031?logo=angular&logoColor=white" alt="Angular 17">
    <img src="https://img.shields.io/badge/gRPC-protocol-00BCD4" alt="gRPC">
    <img src="https://img.shields.io/badge/HTTP%2F3-QUIC-4CAF50" alt="HTTP/3">
    <img src="https://img.shields.io/badge/Tests-25%2F25_passing-success" alt="Tests">
    <img src="https://img.shields.io/badge/Service_coverage-100%25-success" alt="Coverage">
  </p>

  <p>
    📖 <a href="docs/article.md"><strong>Read the article</strong></a>
    &nbsp;·&nbsp;
    🇩🇪 <a href="docs/roadmap/tutorial.md"><strong>Tutorial (Deutsch)</strong></a>
    &nbsp;·&nbsp;
    ✅ <a href="docs/test-results.md"><strong>Test report</strong></a>
  </p>

</div>

---

## Why This Project?

Most full-stack demos use REST over HTTP/1.1. This project demonstrates the **next generation** of web transport:

| Feature | REST / HTTP/1.1 | This project |
|---|---|---|
| Protocol | Text-based, verbose | Binary Protobuf — 3-10× smaller |
| Streaming | Polling or SSE | Native gRPC server-streaming |
| Transport | TCP (HTTP/1.1 or /2) | **QUIC (HTTP/3)** — 0-RTT, no head-of-line blocking |
| Type safety | OpenAPI / manual | Protobuf contract — code generated |
| Multiplexing | HTTP/2 only | HTTP/3 + QUIC at the transport layer |

---

## Architecture

```
┌─────────────────────────────────────────────────────────┐
│                      Browser                            │
│  Angular 17 — gRPC-Web client (TypeScript, generated)   │
└──────────────────────┬──────────────────────────────────┘
                       │  gRPC-Web (HTTP/1.1 or HTTP/2)
                       ▼
┌──────────────────────────────────────────────────────────┐
│              Envoy Proxy  :8080                          │
│  Translates gRPC-Web ↔ native gRPC (HTTP/2)             │
└──────────────────────┬───────────────────────────────────┘
                       │  gRPC over HTTP/2
                       ▼
┌──────────────────────────────────────────────────────────┐
│         ASP.NET Core 8 API  :5001                        │
│  Kestrel: HTTP/1.1 + HTTP/2 + HTTP/3 (QUIC)             │
│  Alt-Svc header upgrades clients to HTTP/3               │
│  gRPC service: Unary + Server-Streaming RPCs             │
└──────────────────────┬───────────────────────────────────┘
                       │  EF Core (Npgsql)
                       ▼
              ┌─────────────────┐
              │   PostgreSQL    │
              └─────────────────┘
```

**Flow:**
1. Angular calls gRPC methods via the generated `TaskServiceClient`
2. Envoy receives gRPC-Web requests and forwards them as native gRPC to the API
3. ASP.NET Core serves all traffic over HTTP/3 — browsers upgrade automatically via `Alt-Svc`
4. Server-streaming RPCs push task updates to Angular in real time

---

## Features

- **5 gRPC RPC methods**: `GetTask`, `ListTasks`, `CreateTask`, `UpdateTask`, `DeleteTask`
- **Server-streaming**: `StreamTasks` — pushes live updates to Angular every 2 seconds
- **HTTP/3 / QUIC**: Kestrel configured with `Http1AndHttp2AndHttp3` — verifiable in Chrome DevTools
- **Contract-first API**: single `.proto` file shared between backend and frontend
- **Angular Signals**: reactive state without RxJS boilerplate
- **Docker Compose**: one command to run the full stack
- **CI/CD**: GitHub Actions — build, test, and Docker build on every push

---

## Quick Start

### Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (for Docker Compose)
- [mkcert](https://github.com/FiloSottile/mkcert) (for local TLS certs)
- [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download) (optional, for local dev)
- [Node.js 20](https://nodejs.org/) (optional, for local dev)

### 1. Clone

```bash
git clone https://github.com/yourusername/grpc-http3-demo.git
cd grpc-http3-demo
```

### 2. Generate TLS Certificates

HTTP/3 requires HTTPS. Generate trusted local certs:

```bash
bash certs/generate.sh
```

### 3. Start Everything

```bash
docker-compose up --build
```

| Service | URL |
|---|---|
| Angular frontend | http://localhost:4200 |
| gRPC API (HTTP/3) | https://localhost:5001 |
| Envoy (gRPC-Web) | http://localhost:8080 |

---

## Verify HTTP/3

1. Open Chrome → `https://localhost:5001/healthz`
2. Open DevTools → **Network** tab
3. Look at the **Protocol** column — you should see **h3**

> **Note**: The `Alt-Svc: h3=":5001"` response header triggers the browser upgrade from HTTP/2 to HTTP/3 on subsequent requests.

---

## Verify gRPC Streaming

1. Navigate to http://localhost:4200/stream
2. Click **Start Stream**
3. The page updates in real time — new tasks appear at the top as the server pushes data
4. Open DevTools → Network → filter `Fetch/XHR` → see the long-lived streaming request

---

## Local Development (without Docker)

### Backend

```bash
cd backend/GrpcApi

# Start PostgreSQL (Docker)
docker run -d -e POSTGRES_PASSWORD=postgres -e POSTGRES_DB=grpc_tasks -p 5432:5432 postgres:16-alpine

# Run migrations and start
dotnet run
```

API available at `https://localhost:5001`

### Frontend

```bash
cd frontend

# Generate gRPC-Web client from .proto
npm run proto:gen   # requires protoc + protoc-gen-grpc-web

# Install and serve
npm install
npm start           # https://localhost:4200
```

---

## API Reference

All communication is via **gRPC** (no REST endpoints).

| RPC | Type | Request | Response |
|---|---|---|---|
| `GetTask` | Unary | `GetTaskRequest { id }` | `TaskResponse` |
| `ListTasks` | Unary | `ListTasksRequest { page, page_size }` | `ListTasksResponse { tasks[] }` |
| `CreateTask` | Unary | `CreateTaskRequest { title, description }` | `TaskResponse` |
| `UpdateTask` | Unary | `UpdateTaskRequest { id, title, status }` | `TaskResponse` |
| `DeleteTask` | Unary | `DeleteTaskRequest { id }` | `DeleteTaskResponse { success, message }` |
| `StreamTasks` | **Server-streaming** | `StreamTasksRequest { filter_status }` | `stream TaskResponse` |

**Task statuses**: `pending` · `in_progress` · `done` · `cancelled`

---

## Project Structure

```
grpc-http3-demo/
├── backend/
│   ├── GrpcApi/
│   │   ├── Protos/task.proto       ← single source of truth for API contract
│   │   ├── Services/TaskGrpcService.cs
│   │   ├── Data/
│   │   │   ├── AppDbContext.cs
│   │   │   └── TaskEntity.cs
│   │   └── Program.cs             ← HTTP/3 Kestrel config here
│   ├── GrpcApi.Tests/             ← xUnit + FluentAssertions, 25 tests
│   │   ├── Helpers/                 (TestServerCallContext, CapturingStreamWriter, TestDb)
│   │   ├── TaskGrpcServiceUnaryTests.cs
│   │   ├── TaskGrpcServiceStreamingTests.cs
│   │   └── TaskEntityTests.cs
│   └── Dockerfile
├── frontend/
│   ├── src/app/
│   │   ├── core/grpc/
│   │   │   ├── generated/          ← protoc-generated TypeScript client
│   │   │   └── task.service.ts     ← wraps gRPC-Web in Observables + Signals
│   │   └── features/tasks/
│   │       ├── task-list/          ← unary RPC demo
│   │       └── task-stream/        ← server-streaming demo
│   └── Dockerfile
├── proxy/envoy.yaml               ← gRPC-Web ↔ gRPC bridge
├── certs/generate.sh              ← mkcert TLS setup
├── docs/
│   ├── article.md                 ← deep-dive write-up (md + HTML)
│   ├── test-results.md            ← test &amp; coverage report
│   └── roadmap/
│       ├── roadmap.svg            ← visual project roadmap (German)
│       └── tutorial.md            ← step-by-step tutorial (German)
├── docker-compose.yml
└── .github/workflows/ci.yml
```

---

## Key Implementation Details

### HTTP/3 Configuration (ASP.NET Core)

```csharp
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5001, listenOptions =>
    {
        listenOptions.UseHttps();
        listenOptions.Protocols = HttpProtocols.Http1AndHttp2AndHttp3; // ← HTTP/3 enabled
    });
});
```

Kestrel automatically adds `Alt-Svc: h3=":5001"` to responses, triggering QUIC upgrade.

### Server-Streaming RPC

```csharp
public override async Task StreamTasks(
    StreamTasksRequest request,
    IServerStreamWriter<TaskResponse> responseStream,
    ServerCallContext context)
{
    while (!context.CancellationToken.IsCancellationRequested)
    {
        var tasks = await db.Tasks.OrderByDescending(t => t.UpdatedAt).Take(10).ToListAsync();
        foreach (var task in tasks)
            await responseStream.WriteAsync(MapToResponse(task));
        await Task.Delay(TimeSpan.FromSeconds(2));
    }
}
```

### Angular Signal-Based Reactive State

```typescript
readonly streamedTasks = signal<Task[]>([]);

stream.on('data', (response) => {
  this.streamedTasks.update(tasks => [this.mapTask(response), ...tasks].slice(0, 50));
});
```

---

## Testing

The backend has a dedicated xUnit test project at `backend/GrpcApi.Tests/` covering **all 6 RPCs** including the server-streaming one.

| Metric | Value |
|---|---|
| Tests | **25 / 25 passing** ✅ |
| Duration | ~0.8 s |
| `TaskGrpcService` line coverage | **100 %** |
| `TaskGrpcService` branch coverage | **100 %** |

```bash
cd backend
DOTNET_SYSTEM_NET_DISABLEIPV6=1 \
  dotnet test GrpcApi.Tests/GrpcApi.Tests.csproj -c Release \
    --collect:"XPlat Code Coverage"
```

Highlights of the test design:

- **`TestServerCallContext`** — minimal `ServerCallContext` implementation, no Kestrel needed.
- **`CapturingStreamWriter<T>`** — captures every message a server-streaming RPC writes for assertion.
- **`TestDb.Create()`** — fresh isolated EF Core InMemory database per test (zero cross-test pollution).
- **Streaming tests** verify both happy-path push, server-side filter, and prompt cancellation.

Full per-test breakdown and coverage table: [`docs/test-results.md`](docs/test-results.md).

---

## Tech Stack

| Layer | Technology |
|---|---|
| Backend runtime | .NET 8 / ASP.NET Core |
| gRPC framework | `Grpc.AspNetCore` |
| HTTP/3 transport | Kestrel + `System.Net.Quic` (libmsquic) |
| Database | PostgreSQL 16 + EF Core 8 |
| Proxy | Envoy 1.29 |
| Frontend | Angular 17 (standalone, signals) |
| gRPC-Web client | `grpc-web` npm package + `protoc-gen-grpc-web` |
| UI | Angular Material 17 |
| Containerization | Docker + Docker Compose |
| CI/CD | GitHub Actions |

---

## Resume Talking Points

- Configured **HTTP/3 / QUIC** in ASP.NET Core Kestrel — reducing connection establishment latency via 0-RTT handshake and eliminating TCP head-of-line blocking
- Implemented **gRPC server-streaming** with real-time Angular client using `grpc-web` and Angular Signals
- Defined **contract-first API** with Protocol Buffers — shared `.proto` generates both C# server stubs and TypeScript client code
- Bridged browser gRPC limitations via **Envoy gRPC-Web proxy** for transparent protocol translation
- Containerized full-stack with **Docker Compose** including health checks and dependency ordering
- Built **GitHub Actions CI** pipeline with PostgreSQL service container for integration tests

---

## License

MIT — see [LICENSE](LICENSE)
