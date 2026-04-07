# dotnet-microservice

![CI](https://github.com/Shaisolaris/dotnet-microservice/actions/workflows/ci.yml/badge.svg)



.NET 8 microservice with RabbitMQ messaging, health checks, Entity Framework Core InMemory database, and Swagger API docs.

## Quick Start

```bash
git clone https://github.com/Shaisolaris/dotnet-microservice.git
cd dotnet-microservice
dotnet restore
dotnet run --project ./src/ProductService
# Open http://localhost:5000/swagger
```

ASP.NET Core 8 microservice template with Docker containerization, RabbitMQ event publishing, structured health checks (liveness/readiness), EF Core with seed data, and Swagger documentation. Includes a complete Product CRUD API with stock management and event-driven notifications.

## Stack

- **Framework:** ASP.NET Core 8, .NET 8
- **Messaging:** RabbitMQ (topic exchange, persistent messages)
- **Data:** EF Core 8 (InMemory for demo, SQL Server ready)
- **Container:** Docker with multi-stage build
- **Health:** ASP.NET Core Health Checks with custom checks
- **Docs:** Swagger/OpenAPI

## Architecture

```
┌──────────────┐     ┌──────────────┐     ┌──────────┐
│   API        │────▶│  Service     │────▶│ EF Core  │
│  Controllers │     │  Layer       │     │ DbContext │
└──────┬───────┘     └──────────────┘     └──────────┘
       │
       ▼
┌──────────────┐
│  Message Bus │────▶ RabbitMQ (topic exchange)
│  (events)    │     product.created / product.stock_updated / product.low_stock
└──────────────┘
```

## API Endpoints

| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/products` | List products (filter by category, active status) |
| GET | `/api/products/{id}` | Get product by ID |
| GET | `/api/products/sku/{sku}` | Get product by SKU |
| POST | `/api/products` | Create product (publishes `product.created`) |
| PUT | `/api/products/{id}` | Update product (publishes `product.updated`) |
| POST | `/api/products/{id}/stock` | Update stock (publishes `product.stock_updated`, `product.low_stock`) |
| DELETE | `/api/products/{id}` | Soft delete (publishes `product.deactivated`) |
| GET | `/health` | Full health check (all checks) |
| GET | `/health/live` | Liveness probe (startup check) |
| GET | `/health/ready` | Readiness probe (database check) |
| GET | `/info` | Service metadata |

## Events Published

| Event | Routing Key | Trigger |
|---|---|---|
| `product.created` | `product.created` | New product created |
| `product.updated` | `product.updated` | Product details changed |
| `product.stock_updated` | `product.stock_updated` | Stock quantity changed |
| `product.low_stock` | `product.low_stock` | Stock drops to 10 or below |
| `product.deactivated` | `product.deactivated` | Product soft deleted |

## Health Checks

Three health endpoints for Kubernetes/Docker orchestration:

- `/health` — All checks with detailed JSON response (status, duration per check)
- `/health/live` — Liveness: is the service process running and responsive?
- `/health/ready` — Readiness: can the service handle requests? (database connectivity)

## Docker

```bash
# Build and run with Docker Compose
docker-compose up -d

# Services:
# - Product API: http://localhost:8080/swagger
# - RabbitMQ Management: http://localhost:15672 (guest/guest)
# - Redis: localhost:6379
```

Multi-stage Dockerfile: SDK image for build, runtime image for production (smaller footprint).

## File Structure

```
dotnet-microservice/
├── docker-compose.yml                           # Product service + RabbitMQ + Redis
├── docker/Dockerfile                            # Multi-stage .NET 8 build
├── src/ProductService/
│   ├── Controllers/ProductsController.cs        # CRUD + stock + event publishing
│   ├── Models/Models.cs                         # Product entity, request/response records
│   ├── Services/ProductService.cs               # Business logic + EF Core DbContext
│   ├── Messaging/MessageBus.cs                  # RabbitMQ publisher + InMemory fallback
│   ├── HealthChecks/HealthChecks.cs             # Database + Startup health checks
│   ├── Program.cs                               # DI, pipeline, health endpoints, CORS
│   ├── ProductService.csproj
│   └── appsettings.json
└── dotnet-webapi-clean.sln
```

## Key Design Decisions

**InMemory fallback for messaging.** If RabbitMQ is unavailable, the service falls back to `InMemoryMessageBus`. This enables local development without Docker while maintaining the same `IMessageBus` interface. Events are logged instead of published.

**Seed data in DbContext.** Three products are seeded via `HasData()` in `OnModelCreating`. The InMemory database ensures the service starts with data for immediate testing. Production would use migrations.

**Separate liveness and readiness probes.** Liveness (`/health/live`) only checks if the process is running. Readiness (`/health/ready`) checks database connectivity. Kubernetes uses liveness to decide restart, readiness to decide traffic routing.

**Event publishing in controller, not service.** The controller publishes events after the service completes the operation. This keeps the service layer focused on business logic and gives the controller full context for event construction (including HTTP-level concerns).

**Low stock alert as separate event.** When stock drops to 10 or below, a `product.low_stock` event fires alongside the `product.stock_updated` event. Consumers can subscribe to just low-stock alerts without processing all stock changes.

## License

MIT
