# AspireProject

Modern .NET Aspire sample that wires together an API gateway, an invoice producer, and a payment consumer via RabbitMQ, complete with reusable messaging abstractions, tests, and helper scripts for local and containerized runs.

## Repository Layout

- `AppHost/AppHost` – Aspire distributed app host that orchestrates the Web API plus invoice and payment microservices.
- `AppHost/WebApi.Service` (+ `tests/WebApi.Service.Tests`) – ASP.NET Core 10 API that exposes the sample `/WeatherForecast` endpoint and ships with NUnit integration tests.
- `AppHost/InvoiceMicroservice` – Console worker that creates fake invoices interactively and publishes `InvoiceCreated` events via MassTransit and RabbitMQ.
- `AppHost/PaymentMicroservice` – Worker that subscribes to the invoices exchange/queue and logs (or processes) each invoice.
- `AppHost/TestConsumer` – Kafka-only worker that subscribes to `InvoiceCreated` on `messagecontracts.messages.invoice.invoicecreated`.
- `AppHost/MessageContracts` – Shared POCO contracts for invoices plus the `Message` base type.
- `AppHost/Messaging` – Thin abstractions (`IMessageProducer<T>`, `IMessageHandler<T>`) shared by producers/consumers.
- `AppHost/tests/...` - NUnit suites for both microservices, covering unit- and RabbitMQ-backed integration cases.
- `webapp/` - Next.js 14 storefront that sells three sample products, drives the cart/order UX, and calls `WebApi.Service`.
- `run-services.sh|.ps1`, `start-docker-instances.sh` - Developer tooling to build/run the solution or its containers.

## Solution Topology

![System Architecture](https://github.com/DragceSU/AspireProject/blob/main/System%20Architecture.png?raw=true)

```mermaid
flowchart LR
    subgraph Experiences
        W[Next.js Webapp<br/>SPA]
    end

    subgraph Services
        A[WebApi.Service<br/>HTTP API]
        B[InvoiceMicroservice<br/>Publisher]
        C[PaymentMicroservice<br/>Consumer]
        T[TestConsumer<br/>Kafka Consumer]
    end

    W -- REST/JSON --> A
    B -- InvoiceCreated --> Q[(RabbitMQ<br/>Exchange: invoice-service)]
    Q -- fanout/queue --> C
    B -- InvoiceCreated --> K[(Kafka<br/>Topic: messagecontracts.messages.invoice.invoicecreated)]
    K --> T
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
        test[(TestConsumer)]
    end
    bus[[RabbitMQ<br/>invoice-service & order-service]]
    kafka[[Kafka<br/>invoicecreated topic]]

    user --> spa
    spa --> api
    api --> user
    invoice -. async .-> bus
    bus -. async .-> payment
    api -. OrderSubmission .-> bus
    invoice -. async .-> kafka
    kafka -. async .-> test
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
        test[TestConsumer<br/>Kafka Consumer]
    end
    bus[(RabbitMQ<br/>invoice-service exchange)]
    kafka[(Kafka<br/>invoicecreated topic)]
    webapp[Storefront<br/>Next.js 14]

    webapp -- REST + Swagger --> api
    api -- references --> shared
    invoice -- references --> shared
    payment -- references --> shared
    test -- references --> shared
    invoice -. async .-> bus
    bus -. async .-> payment
    invoice -. async .-> kafka
    kafka -. async .-> test
    api -. OrderSubmission .-> bus
```

Editable Excalidraw source for these diagrams lives at `C:\Git\Untitled-2025-11-13-2006.excalidraw`.

### Component Diagram – Web API

```mermaid
flowchart LR
    subgraph WebApi.Service
        controller[Controllers<br/>Orders + Weather]
        services[OrderProducer<br/>+ MessageProducer]
        configs[AppConfiguration<br/>& Options]
    end
    sharedContracts[(MessageContracts<br/>DTOs)]
    masstransit[MassTransit<br/>+ RabbitMQ client]
    swagger[Swagger/OpenAPI]
    cors[CORS Policy]
    bus[(RabbitMQ Exchanges)]

    controller --> services
    controller --> swagger
    controller --> cors
    services --> masstransit --> bus
    services --> sharedContracts
    controller --> sharedContracts
    configs --> controller
    configs --> services
```

### Component Diagram – Worker Services

```mermaid
flowchart LR
    subgraph InvoiceMicroservice
        input[Console Input<br/>InvoiceGenerator]
        invoiceProducer[IMessageProducer<InvoiceCreated>]
        orderConsumer[OrderSubmission Handler]
    end

    subgraph PaymentMicroservice
        consumer[InvoiceCreatedConsumer]
        handler[IMessageHandler<InvoiceCreated>]
    end

    contracts[(MessageContracts)]
    messaging[(Messaging Abstractions)]
    rabbit[(RabbitMQ Exchanges)]
    kafka[(Kafka Topic: invoicecreated)]

    input --> invoiceProducer
    invoiceProducer --> rabbit
    orderConsumer <-- rabbit
    consumer --> handler
    consumer <-- rabbit
    invoiceProducer --> kafka
    kafka --> consumer
    orderConsumer --> messaging
    invoiceProducer --> messaging
    handler --> messaging
    messaging --> contracts
```

## Core Scenarios (“Cases”)

1. **Invoice creation & publishing** - The `InvoiceMicroservice` (`AppHost/InvoiceMicroservice/Program.cs`) reads RabbitMQ settings (appsettings or `RABBIT_HOST`) and waits for keyboard input. Each keystroke (except `q`) generates deterministic-but-random invoices and publishes them via `IMessageProducer<InvoiceCreated>`, ensuring traceable IDs and sample line items for downstream consumers.
2. **Payment ingestion & handling** - `PaymentMicroservice` (`AppHost/PaymentMicroservice/Program.cs`) configures MassTransit with an `InvoiceCreatedConsumer`. It binds the `payment-microservice` queue to the `invoice-service` exchange (default `fanout`), delivering events to the reusable `IMessageHandler<InvoiceCreated>` which currently logs but can be swapped for real payment logic or orchestrated retry workflows.
3. **Order submission pipeline** - The Next.js storefront calls `WebApi.Service/api/orders` (default `http://localhost:5088`). The controller validates the payload, publishes an `OrderSubmission` message via MassTransit (RabbitMQ), and replies with an acknowledgement so the UI can display the “latest receipt”. A sibling `POST /api/orders/kafka` endpoint reuses the same validation/DTO mapping but writes to Kafka instead of RabbitMQ. `InvoiceMicroservice` now also consumes `OrderSubmission` messages via a dedicated queue/exchange and routes them through `IMessageHandler<OrderSubmission>` for downstream processing.
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

2. **Start Kafka (single-node KRaft)**  
   ```bash
   docker compose up -d kafka-kraft kowl
   ```
   This provides the broker that `POST /api/orders/kafka` targets and a Kowl UI at `http://localhost:8080`. Stop with `docker compose down` when done.

3. **Build everything**  
   ```bash
   cd AppHost
   dotnet build AppHost.slnx
   ```

4. **Run services quickly**  
   - **Aspire host**: `dotnet run --project AppHost/AppHost/AppHost.csproj` (launches all three services with Aspire dashboards when enabled).
   - **Windows**: `./run-services.ps1 -InvoiceInstances 1 -PaymentInstances 3 -RabbitHost localhost`
   - **Unix/macOS**: `./run-services.sh --paymentInstances 3 --rabbitHost localhost`

   Scripts compile the solution, then launch invoice & payment workers (multiple instances supported). Alternatively, run each project manually:
   ```bash
   cd AppHost/InvoiceMicroservice && dotnet run
   # separate terminal
   cd AppHost/PaymentMicroservice && dotnet run
   # Kafka-only consumer
   cd AppHost/TestConsumer && dotnet run
   # API
   cd AppHost/WebApi.Service && dotnet run
   ```

5. **Run the storefront (optional)**  
   ```bash
   cd webapp
   npm install
   NEXT_PUBLIC_WEBAPI_BASE_URL=http://localhost:5088 npm run dev
   ```
   The app runs at `http://localhost:3000`, calls the API via the `NEXT_PUBLIC_WEBAPI_BASE_URL` env var, and expects the API to have CORS enabled (defaults already permit `http://localhost:3000`).

6. **Dockerized services helper**  
   ```bash
   ./start-docker-instances.sh InvoiceCount 1 PaymentCount 2
   ```
   Requirements: the RabbitMQ container named `rabbitmq` and the Kafka container named `kafka-kraft` must already be running and the `invoice-microservice`, `payment-microservice`, and `aspire-webapi` images must exist. The script (a) cleans up old invoice/payment containers, (b) launches the requested counts with the `host.docker.internal` gateway mapping, (c) mounts `AppHost/InvoiceMicroservice/appsettings.docker.json` and `AppHost/PaymentMicroservice/appsettings.docker.json` so the containers use `rabbitmq` and `kafka-kraft` hostnames, and (d) ensures both `aspire-net` and `aspireproject_kafka-net` are used so the containers can reach RabbitMQ and Kafka.
   To disable the Web API container, pass `WebApiEnabled false`:
   ```bash
   ./start-docker-instances.sh InvoiceCount 1 PaymentCount 2 WebApiEnabled false
   ```

7. **Dockerized Web API**  
   Build from the `AppHost` directory (so the Dockerfile can locate shared projects):
   ```bash
   cd AppHost
   docker build -t aspire-webapi -f WebApi.Service/Dockerfile .
   ```
   The API container needs to reach RabbitMQ via Docker DNS (and Kafka if using `/api/orders/kafka`). Create a bridge network once, then attach both containers and run the API:
   ```bash
   docker network create aspire-net                           # no-op if it already exists
   docker network connect aspire-net rabbitmq                # only needed the first time
   docker run -d --rm --name aspire-webapi \
     --network aspire-net \
     -p 5088:8080 \
     -e ASPNETCORE_ENVIRONMENT=Development \
     aspire-webapi
   ```
   `MassTransit` now resolves the broker at `rabbitmq:5672`, while the host accesses Swagger at `http://localhost:5088/swagger`. Use `docker logs aspire-webapi` to confirm `Bus started: rabbitmq://rabbitmq/`. If you need Kafka from inside the container, attach it to the Kafka network as well:
   ```bash
   docker network connect aspireproject_kafka-net aspire-webapi
   ```

## Configuration Notes

- **Web API CORS**: `AppHost/WebApi.Service/appsettings.json` exposes `AllowedOrigins`. Override (or use user secrets/environment variables) to permit whichever hosts the storefront runs under (`http://localhost:3000` by default).
- **Storefront API base URL**: The Next.js client reads `NEXT_PUBLIC_WEBAPI_BASE_URL`; fallback is `http://localhost:5088`. Update in `.env.local` when deploying elsewhere.
- **Kafka integration**: `AppHost/WebApi.Service/appsettings.json` configures `Kafka:BootstrapServers` (defaults to `localhost:9092`). Kafka topics reuse the CLR type name (e.g., `OrderSubmission`) so new message types automatically map to their own topic. When running in Docker, use `kafka-kraft:9092` (see `AppHost/*/appsettings.docker.json`). `TestConsumer` listens on `messagecontracts.messages.invoice.invoicecreated` with `BootstrapServers=localhost:29092` when running on the host.
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
