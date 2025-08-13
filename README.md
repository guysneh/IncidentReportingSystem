# Incident Reporting System

A professionally structured .NET 8 Web API for reporting and managing incidents, built using Clean Architecture principles and modern best practices.

![Docker Ready](https://img.shields.io/badge/Docker-Ready-blue)
![License: MIT](https://img.shields.io/badge/license-MIT-green)

---

## 🚀 Features

- RESTful API (v1)
- Create and update incident reports
- Filter and search incidents
- View statistics by category and severity
- JWT-based authentication (demo only)
- Full Swagger UI documentation
- PostgreSQL database
- Docker Compose support
- Built-in rate limiting and CORS
- X-Correlation-ID tracing support
- Extensive test coverage (unit + integration)

---

## ⚙️ Getting Started

### 1. Requirements

- Docker
- Docker Compose
- [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) (for running tests locally)

### 2. Clone the Repository

```bash
git clone https://github.com/guysneh/IncidentReportingSystem.git
cd IncidentReportingSystem
```

### 3. Environment Configuration

Copy the example environment file and rename it:

```bash
cp .env.example .env
```

> You can customize the values in the `.env` file (e.g., ports, passwords), but the defaults should work out of the box.

---

## 🐳 Running the Application

Use Docker Compose to start all services:

```bash
docker compose up --build
```

The following services will be available:

| Service          | URL                                                                                  |
| ---------------- | ------------------------------------------------------------------------------------ |
| API (Swagger UI) | [http://localhost:8080/swagger/index.html](http://localhost:8080/swagger/index.html) |
| pgAdmin          | [http://localhost:5050](http://localhost:5050)                                       |

> 📌 **Note:** pgAdmin default port is **5050**.

---

## 🔐 Authentication (Demo Only)

All endpoints require a valid JWT token in the `Authorization` header:

```http
Authorization: Bearer your-token-here
```

To generate a demo token, use the following hardcoded values:

```json
{
  "userId": "demo",
  "role": "Admin"
}
```

> 🔒 This is a mock authentication setup intended for demonstration purposes only.

---

## 📊 Swagger UI

Swagger is enabled to help you explore and test the API.

- URL: [http://localhost:8080/swagger/index.html](http://localhost:8080/swagger/index.html)
- Click **"Authorize"** and paste your JWT token to access protected endpoints

---

## 📁 Project Structure

```plaintext
IncidentReportingSystem
├── .env.example                            → Sample environment variables
├── docker-compose.yml                      → Main Docker Compose setup
├── IncidentReportingSystem.API             → Controllers, Middleware, Program.cs
├── IncidentReportingSystem.Application     → CQRS Handlers, Validators, Behaviors
├── IncidentReportingSystem.Domain          → Domain models and Enums
├── IncidentReportingSystem.Infrastructure  → EF Core, Repositories, DB context
├── IncidentReportingSystem.Tests           → Unit & Integration tests
```

---

## 🧪 Testing

Run all tests using:

```bash
dotnet test
```

Tests cover:

- CQRS Handlers
- Middleware behaviors (e.g. error handling, logging)
- Authorization handling
- Rate limiting behavior
- CORS configuration
- Token validation
- Smoke tests for API endpoints

---

## 🏗️ Architecture

This project follows the principles of **Clean Architecture**, separating concerns across well-defined layers:

- **API**: Handles HTTP requests, routing, and middleware
- **Application**: Contains business logic, CQRS handlers, validation, and MediatR setup
- **Domain**: Defines core domain models and enums (pure logic)
- **Infrastructure**: Handles persistence using EF Core
- **Tests**: Unit and integration tests to ensure behavior and stability

```mermaid
graph TD
    A[Client] --> B[API Layer]
    B --> C[MediatR CQRS]
    C --> D[Command / Query Handlers]
    D --> E[Repositories]
    E --> F[EF Core]
    F --> G[PostgreSQL DB]
    B --> H[Middleware: Logging / Auth / Correlation ID]
```

> 🧠 This structure enforces separation of concerns, making the system scalable, testable, and maintainable.

---

## 🛰️ Observability & Tracing

- Each incoming request is automatically assigned a `X-Correlation-ID` header (generated if not provided), logged end-to-end.
- **Cloud:** Application Insights (workspace-based) + Log Analytics with KQL queries for `requests` and `exceptions`.
- **Alerts (cloud):** log-based alerts for **any 5xx** and **/health non-200** in a 5-minute window.
- **Email notifications:** delivered via **Azure Monitor Action Group** connected to those alerts.

---

## ☁️ Cloud Deployment on Azure

> This repository includes Terraform and CI/CD to deploy the API on Azure: **App Service (Linux)**, **Azure PostgreSQL Flexible Server**, **Azure Key Vault (RBAC)**, **Application Insights (workspace-based)**, and a **$100/month budget guardrail**. CI/CD is **manual-only** via GitHub Actions (OIDC).

### High-level Azure Architecture
![Azure Architecture](docs/diagrams/azure-architecture.png)

### CI/CD Flow (manual-only)
![CI/CD Flow](docs/diagrams/cicd-flow.png)

### Key Points
- **Compute:** Azure App Service (Linux, .NET 8), `Always On` enabled, health check path **`/health`** (pinned via Terraform).
- **Data:** Azure PostgreSQL Flexible Server → database `incidentdb` (B1ms, 32 GB). Public access enabled; firewall allows App Service outbound IPs + “AllowAllAzure”.
- **Secrets:** Azure Key Vault (RBAC). App consumes secrets via **Key Vault references**.
- **Observability:** Application Insights (workspace-based) + Log Analytics; log-based alerts for 5xx and `/health` non-200.
- **Cost:** Monthly budget guardrail at **$100**.
- **IaC:** Terraform (remote backend in Azure Storage); modules: RG, App Service Plan, App Service, Postgres, Key Vault, Monitoring, Budget.
- **CI/CD:** Manual GitHub Actions (OIDC) with **EF Core migrations out-of-band (before deploy)**, zip deploy, health wait, and DB smoke test.

### Cloud Resources
- `incident-rg` — Resource Group (tagged)
- `incident-app-plan` — App Service Plan (Linux, Basic)
- `incident-api` — Web App (System-assigned managed identity)
- `incident-db` — PostgreSQL Flexible Server + DB `incidentdb`
- `incident-kv` — Key Vault (RBAC)
- `incident-rg-law` — Log Analytics Workspace
- `incident-rg-appi` — Application Insights (workspace-based)
- `incident-rg-budget` — Subscription budget

### Secrets & RBAC
- **Key Vault secrets** (consumed via references):
  - `PostgreSqlConnectionString` → mapped to `ConnectionStrings__DefaultConnection`
  - `jwt-issuer`, `jwt-audience`, `jwt-secret`, `jwt-expiry-minutes`
- **RBAC:**
  - App Service **Managed Identity** → `Key Vault Secrets User` (read-only)
  - GitHub OIDC SP → `Key Vault Secrets Officer` (manage secrets)

> Secret rotation: update secret in Key Vault → wait 1–2 minutes → restart Web App.

### Operations Runbook (Cloud)
- **Deploy:** run the manual GitHub workflow → migrations (out-of-band), deploy, `/health` wait, DB smoke test.
- **Investigate errors:** App Insights Logs (KQL: `requests`, `exceptions`), App Service **Log stream**.
- **Common issues:**
  - `42P01 (relation does not exist)` → migrations didn’t run against `incidentdb` → re-run deploy.
  - Key Vault reference unresolved → ensure App MI has `Key Vault Secrets User`.
  - Connection issues → ensure App Service outbound IPs in Postgres firewall and `Ssl Mode=Require`.

---

## 🧠 Example API Workflow

1. Generate a JWT token via `/api/v1/auth/token?userId=demo&role=Admin`
2. Send a POST request to `/api/v1/incidentreports` with incident details
3. Use GET `/api/v1/incidentreports/{id}` to retrieve the report
4. Update status via PUT `/api/v1/incidentreports/{id}/status`

> All requests must include the JWT token in the Authorization header.

---

## 🎯 Potential Improvements

- Replace mock auth with Identity Provider (e.g., Azure AD, Auth0)
- **OpenTelemetry tracing integration** (export to Application Insights)
- Apply soft-deletion and audit logging
- Introduce background processing (e.g. using Hosted Services or Hangfire)
- **Cloud hardening:** split DB users (runtime least-privilege vs admin for migrations), SAS-based blob uploads (private container), additional dashboards and alerts

---

## 📝 License

This project is licensed under the [MIT License](LICENSE).
