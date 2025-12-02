# CNAB Store Importer (.NET 9 + SQL Server)

This project is a solution for the ByCoders backend challenge.  
It receives a CNAB file with financial transactions, validates and imports it into a SQL Server database, and exposes a minimal frontend to upload files and inspect store balances.

---

## 1. Project structure

The solution follows a simple layered architecture inside a **single Web API project**, plus a separate test project.

```text
CnabStore.sln
docker-compose.yml
.gitignore
.dockerignore

/src
  /CnabStore.Api
    CnabStore.Api.csproj
    Program.cs
    appsettings.json

    /Domain
      Store.cs
      Transaction.cs
      TransactionType.cs
      TransactionTypeMetadata.cs

    /Application
      /Dtos
        TransactionDto.cs
        StoreSummaryDto.cs
        CnabImportErrorDto.cs
        CnabImportSuccessDto.cs
        CnabImportResultDto.cs
      ICnabLineParser.cs
      CnabLineParser.cs
      ICnabImportService.cs
      CnabImportService.cs
      DependencyInjection.cs

    /Infrastructure
      AppDbContext.cs
      /Configurations
        StoreConfiguration.cs
        TransactionConfiguration.cs

    /Web
      /wwwroot
        index.html   # Minimal HTML+CSS+JS frontend

/tests
  /CnabStore.Tests
    CnabLineParserTests.cs
    CnabImportServiceTests.cs
    TransactionTypeMetadataTests.cs
````

### Layers

* **Domain**

  * Pure domain entities and metadata:

    * `Store` and `Transaction`
    * `TransactionType` and `TransactionTypeMetadata` (income/expense sign logic)
  * No external dependencies.

* **Application**

  * Application use cases and DTOs:

    * `CnabLineParser` (`ICnabLineParser`): parses and validates each CNAB line strictly respecting field sizes and formats.
    * `CnabImportService` (`ICnabImportService`): orchestrates import, upserts stores, persists transactions, and returns detailed import results (success + failures).
  * DTOs used between layers and in API responses:

    * `TransactionDto`, `StoreSummaryDto`, `CnabImportResultDto`, etc.
  * `DependencyInjection` extension to register application services in ASP.NET DI.

* **Infrastructure**

  * EF Core DbContext and mappings:

    * `AppDbContext` (DbSets: `Stores`, `Transactions`)
    * `StoreConfiguration`, `TransactionConfiguration` (table names, indexes, constraints, numeric precision, etc.)
  * Uses **SQL Server** via `UseSqlServer`.

* **Presentation (Minimal API + Frontend)**

  * `Program.cs`:

    * Configures DbContext, DI, Swagger.
    * Applies migrations on startup.
    * Exposes:

      * `GET /` → serves `Web/wwwroot/index.html`.
      * `POST /api/cnab/upload` → imports CNAB file.
      * `GET /api/stores/summary` → paginated store balances.
  * `Web/wwwroot/index.html`:

    * Vanilla HTML/CSS/JS (no CSS framework).
    * Upload form, import result details, and a simple table with store balances + pagination.

* **Tests**

  * `CnabLineParserTests`: validates CNAB parsing rules and field constraints.
  * `CnabImportServiceTests`: uses EF Core InMemory + Moq to verify import behavior and result reporting.
  * `TransactionTypeMetadataTests`: ensures metadata and signs are correct and exhaustive.

---

## 2. How to run with Docker Compose

### Prerequisites

* Docker
* Docker Compose
* Port **8080** (API/Frontend) and **1433** (SQL Server) available on your machine

### Step-by-step

1. **Clone the repository**

```bash
git clone https://github.com/your-org/bycoders-backend-challenge.git
cd bycoders-backend-challenge
```

2. **Start everything with Docker Compose**

From the **solution root** (where `docker-compose.yml` lives):

```bash
docker compose up --build
```

---

## 3. Frontend endpoint

The frontend is a single static page served by the API.

* **URL:**

  ```text
  http://localhost:8080/
  ```

---

## 4. Backend endpoints

Swagger UI is available:

* **URL:**

  ```text
  http://localhost:8080/swagger
  ```

---

## 5. Next steps to make it production-ready

This project is intentionally kept simple to focus on the challenge requirements, but here are clear next steps to turn it into a production-ready application:

### 5.1. Configuration & secrets

* Move sensitive data (DB passwords, etc.) to:

  * environment variables
  * Azure Key Vault / AWS Secrets Manager / HashiCorp Vault, etc.
* Use strong passwords and rotate credentials.
* Add support for multiple environments (`Development`, `Staging`, `Production`) with dedicated config files and overrides.

### 5.2. Observability & logging

* Replace default logging with a structured logger (e.g., **Serilog**):

  * Correlation IDs
  * Request/response logs (with PII care)
  * Database and performance logs
* Add:

  * **Health checks** (`/health`, `/ready`) for liveness/readiness (important for Kubernetes).
  * Metrics via Prometheus/OpenTelemetry (request counts, latency, error rates).

### 5.3. Error handling & validation

* Add global exception handling middleware:

  * Consistent error payloads
  * Proper HTTP status codes (400, 422, 500, etc.)
* Enhance validation:

  * Maximum file size for uploads
  * Maximum number of lines per file
  * Better error messages and logging for invalid CNAB lines.

### 5.4. Security

* Add **authentication/authorization**:

  * e.g., JWT Bearer, OAuth2/OpenID Connect
  * Protect import endpoint so only authorized users can upload CNAB files.
* Enable **HTTPS only** in production.
* Configure CORS policies explicitly (if frontend is hosted on another domain).
* Apply request throttling / rate limiting to protect against abuse.

### 5.5. Database & performance

* Review and optimize indexes on `Stores` and `Transactions`.
* Use proper migrations management:

  * Separate migration step from runtime startup in real environments.
  * Control migration application with DevOps pipelines or migration jobs.
* Consider:

  * Batch inserts / bulk operations for very large CNAB files.
  * Background processing (e.g., queues / workers) for heavy imports.

