# AspireProject

Modern .NET Aspire sample that wires together an API gateway, an invoice producer, and a payment consumer via RabbitMQ, complete with reusable messaging abstractions, tests, and helper scripts for local and containerized runs.

## Repository Layout

- `AppHost/AppHost` – Aspire distributed app host that orchestrates the Web API plus invoice and payment microservices.
- `AppHost/WebApi.Service` (+ `tests/WebApi.Service.Tests`) – ASP.NET Core 10 API that exposes the sample `/WeatherForecast` endpoint and ships with NUnit integration tests.
- `AppHost/InvoiceMicroservice` – Console worker that creates fake invoices interactively and publishes `InvoiceCreated` events via MassTransit and RabbitMQ.
- `AppHost/PaymentMicroservice` – Worker that subscribes to the invoices exchange/queue and logs (or processes) each invoice.
- `AppHost/MessageContracts` – Shared POCO contracts for invoices plus the `Message` base type.
- `AppHost/Messaging` – Thin abstractions (`IMessageProducer<T>`, `IMessageHandler<T>`) shared by producers/consumers.
- `AppHost/tests/...` - NUnit suites for both microservices, covering unit- and RabbitMQ-backed integration cases.
- `webapp/` - Next.js 14 storefront that sells three sample products, drives the cart/order UX, and calls `WebApi.Service`.
- `run-services.sh|.ps1`, `start-docker-instances.sh` - Developer tooling to build/run the solution or its containers.

## Solution Topology

![System Architecture](System Architecture.png)

```mermaid
flowchart LR
    subgraph Experiences
        W[Next.js Webapp<br/>SPA]
    end

    subgraph Services
        A[WebApi.Service<br/>HTTP API]
        B[InvoiceMicroservice<br/>Publisher]
        C[PaymentMicroservice<br/>Consumer]
    end

    W -- REST/JSON --> A
    B -- InvoiceCreated --> Q[(RabbitMQ<br/>Exchange: invoice-service)]
    Q -- fanout/queue --> C
    A -. OrderSubmission .-> O[(RabbitMQ<br/>Exchange: order-service)]
    O -. fanout/queue .-> B

    subgraph Shared Libraries
        M[MessageContracts<br/>+ Messaging]
    end

    M --> B
    M --> C
    M --> A
```

### Context Diagram

```mermaid
flowchart LR
    user([Web operator])
    spa[/Next.js SPA/]
    subgraph AspireHost
        api[(WebApi.Service)]
        invoice[(InvoiceMicroservice)]
        payment[(PaymentMicroservice)]
    end
    bus[[RabbitMQ<br/>invoice-service & order-service]]

    user --> spa
    spa --> api
    api --> user
    invoice -. async .-> bus
    bus -. async .-> payment
    api -. OrderSubmission .-> bus
```

### Container Diagram

```mermaid
flowchart TB
    subgraph AspireHost
        direction LR
        api[WebApi.Service<br/>ASP.NET Core]
        shared[MessageContracts + Messaging]
        invoice[InvoiceMicroservice<br/>Publisher]
        payment[PaymentMicroservice<br/>Consumer]
    end
    bus[(RabbitMQ<br/>invoice-service exchange)]
    webapp[Storefront<br/>Next.js 14]

    webapp -- REST + Swagger --> api
    api -- references --> shared
    invoice -- references --> shared
    payment -- references --> shared
    invoice -. async .-> bus
    bus -. async .-> payment
    api -. OrderSubmission .-> bus
```

Editable Excalidraw source for these diagrams lives at `C:\Git\Untitled-2025-11-13-2006.excalidraw`.

## Core Scenarios (“Cases”)

1. **Invoice creation & publishing** - The `InvoiceMicroservice` (`AppHost/InvoiceMicroservice/Program.cs`) reads RabbitMQ settings (appsettings or `RABBIT_HOST`) and waits for keyboard input. Each keystroke (except `q`) generates deterministic-but-random invoices and publishes them via `IMessageProducer<InvoiceCreated>`, ensuring traceable IDs and sample line items for downstream consumers.
2. **Payment ingestion & handling** - `PaymentMicroservice` (`AppHost/PaymentMicroservice/Program.cs`) configures MassTransit with an `InvoiceCreatedConsumer`. It binds the `payment-microservice` queue to the `invoice-service` exchange (default `fanout`), delivering events to the reusable `IMessageHandler<InvoiceCreated>` which currently logs but can be swapped for real payment logic or orchestrated retry workflows.
3. **Order submission pipeline** - The Next.js storefront calls `WebApi.Service/api/orders` (default `http://localhost:5088`). The controller validates the payload, publishes an `OrderSubmission` message via MassTransit, and replies with an acknowledgement so the UI can display the “latest receipt”. `InvoiceMicroservice` now also consumes `OrderSubmission` messages via a dedicated queue/exchange and routes them through `IMessageHandler<OrderSubmission>` for downstream processing.
4. **Web API experience & observability** - `WebApi.Service` exposes `/WeatherForecast` and `/api/orders`, hosts Swagger UI at `/swagger`, publishes to RabbitMQ, and now enables CORS for the storefront. Aspire-provided telemetry, discovery, and resilience features are ready once the API references the `Project.Aspire` defaults.

Each scenario has supporting tests:
- `AppHost/tests/InvoiceMicroservice.Tests` validates producer behavior plus RabbitMQ publishing.
- `AppHost/tests/PaymentMicroservice.Tests` checks consumer delegation and end-to-end queue handling.
- `AppHost/tests/WebApi.Service.Tests` verifies the API returns five forecasts with valid data.

## Running the Stack Locally

1. **Start RabbitMQ (Unix example)**  
   ```bash
   docker run -d --name rabbitmq \
     -p 5672:5672 \
     -p 15672:15672 \
     rabbitmq:3-management
   ```
   Management UI becomes available at `http://localhost:15672` (`guest/guest`).
   > Tip: keep the container name `rabbitmq`; the Web API resolves that hostname when running in Docker.

2. **Build everything**  
   ```bash
   cd AppHost
   dotnet build AppHost.slnx
   ```

3. **Run services quickly**  
   - **Aspire host**: `dotnet run --project AppHost/AppHost/AppHost.csproj` (launches all three services with Aspire dashboards when enabled).
   - **Windows**: `./run-services.ps1 -InvoiceInstances 1 -PaymentInstances 3 -RabbitHost localhost`
   - **Unix/macOS**: `./run-services.sh --paymentInstances 3 --rabbitHost localhost`

   Scripts compile the solution, then launch invoice & payment workers (multiple instances supported). Alternatively, run each project manually:
   ```bash
   cd AppHost/InvoiceMicroservice && dotnet run
   # separate terminal
   cd AppHost/PaymentMicroservice && dotnet run
   # API
   cd AppHost/WebApi.Service && dotnet run
   ```

4. **Run the storefront (optional)**  
   ```bash
   cd webapp
   npm install
   NEXT_PUBLIC_WEBAPI_BASE_URL=http://localhost:5088 npm run dev
   ```
   The app runs at `http://localhost:3000`, calls the API via the `NEXT_PUBLIC_WEBAPI_BASE_URL` env var, and expects the API to have CORS enabled (defaults already permit `http://localhost:3000`).

5. **Dockerized services helper**  
   ```bash
   ./start-docker-instances.sh InvoiceCount 1 PaymentCount 2
   ```
   Requirements: the RabbitMQ container named `rabbitmq` must already be running and the `invoice-microservice`, `payment-microservice`, and `aspire-webapi` images must exist. The script (a) cleans up old invoice/payment containers, (b) launches the requested counts with the `host.docker.internal` gateway mapping, and (c) ensures the `aspire-net` bridge network exists so it can attach both `rabbitmq` and a single `aspire-webapi` container (bound to `http://localhost:5088/swagger`).

6. **Dockerized Web API**  
   Build from the `AppHost` directory (so the Dockerfile can locate shared projects):
   ```bash
   cd AppHost
   docker build -t aspire-webapi -f WebApi.Service/Dockerfile .
   ```
   The API container needs to reach RabbitMQ via Docker DNS. Create a bridge network once, then attach both containers and run the API:
   ```bash
   docker network create aspire-net                           # no-op if it already exists
   docker network connect aspire-net rabbitmq                # only needed the first time
   docker run -d --rm --name aspire-webapi \
     --network aspire-net \
     -p 5088:8080 \
     -e ASPNETCORE_ENVIRONMENT=Development \
     aspire-webapi
   ```
   `MassTransit` now resolves the broker at `rabbitmq:5672`, while the host accesses Swagger at `http://localhost:5088/swagger`. Use `docker logs aspire-webapi` to confirm `Bus started: rabbitmq://rabbitmq/`.

## Configuration Notes

- **Web API CORS**: `AppHost/WebApi.Service/appsettings.json` exposes `AllowedOrigins`. Override (or use user secrets/environment variables) to permit whichever hosts the storefront runs under (`http://localhost:3000` by default).
- **Storefront API base URL**: The Next.js client reads `NEXT_PUBLIC_WEBAPI_BASE_URL`; fallback is `http://localhost:5088`. Update in `.env.local` when deploying elsewhere.
- **RabbitMQ bindings**: The order pipeline uses the `order-service` exchange with the `invoice-order-submission` queue (configurable in `AppHost/InvoiceMicroservice/appsettings.json`). The payment/invoice flow still relies on the existing `invoice-service` exchange.

## Testing

From `AppHost`:
```bash
dotnet test AppHost.slnx
```
This runs the API, producer, and consumer test suites. Integration tests communicate with whatever RabbitMQ endpoint is configured in `tests/*/appsettings.json` (defaults to `localhost`), so ensure the broker is running first. `AppHost/WebApi.Service.Tests` focuses on DTO-to-message mapping and producer behavior so the controller logic stays thin.

## Extending the Architecture

- **Deepen Aspire integration**: configure per-service resources (RabbitMQ container, dashboards, secrets) or add health checks so Aspire’s environment view reflects production topology.
- **Enhance message handling**: replace the logging-only `MessageHandler<T>` with richer business logic, validation, and error handling policies.
- **Add API endpoints**: expose invoice/payment status via `WebApi.Service`, using the shared contracts to keep REST + messaging synchronized.

These improvements can build directly on the existing contracts, MassTransit setup, and testing harness already in place.
