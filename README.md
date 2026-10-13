# Library API: ASP.NET Core

[![CI](https://github.com/devfoundry-training/Library_API_DotNet/actions/workflows/ci.yml/badge.svg)](https://github.com/devfoundry-training/Library_API_DotNet/actions/workflows/ci.yml)

Part of the DevFoundry Library API series: one REST API for a small library (books, members, loans and fines),
built the same way in several stacks. This repository is the ASP.NET Core version: a minimal API on .NET 10 with
Entity Framework Core 10, migrations for SQLite and PostgreSQL, and xUnit tests for the service layer.

The contract it implements (the OpenAPI description, the business rules, the schema, the sample data and the
conformance suite) lives in [Library_API_Contract](https://github.com/devfoundry-training/Library_API_Contract).
This is the **basic** version: the contract and nothing more.

---

## Who Is This For?

- C# developers who want to see a layered minimal API built to a fixed specification
- Delegates on ASP.NET Core and Entity Framework Core courses who want a realistic project to read and extend
- C# Zero to Hero learners taking the Library Manager from a console program to a web API
- Developers comparing stacks: the FastAPI, Django and Spring Boot versions answer the same requests

Comfort with C#, HTTP and basic SQL is assumed.

---

## Prerequisites

| Tool | Minimum Version | Needed for |
|------|-----------------|------------|
| .NET SDK | 10.0 | building and running the API |
| Python | 3.10+ (the series uses 3.13) | the conformance suite (standard library only) |
| Docker with Compose | 24+ | PostgreSQL and running the API in a container |

```bash
dotnet --version
py -3.13 --version      # Windows
python3 --version       # macOS/Linux
```

---

## Getting Started

Clone this repository and the contract side by side; the conformance commands below expect both folders in the
same parent folder.

```bash
git clone https://github.com/devfoundry-training/Library_API_DotNet.git
git clone https://github.com/devfoundry-training/Library_API_Contract.git
cd Library_API_DotNet
```

### Option 1: Local run with SQLite (Recommended to start)

```bash
dotnet run --project src/Library.Api
```

The API listens on http://localhost:8000. On start-up it runs the EF Core migrations, creates
`src/Library.Api/library.db` (`dotnet run` starts the app in the project folder) and loads the sample data
(20 books, 9 members, 12 loans) if the books table is empty. Delete the file to start again from the sample data.

```bash
curl http://localhost:8000/health
curl http://localhost:8000/books/4
curl "http://localhost:8000/loans?overdue=true"
```

To run in test mode (needed by the conformance suite):

```bash
# Windows PowerShell
$env:TEST_MODE = "1"; dotnet run --project src/Library.Api
# macOS/Linux
TEST_MODE=1 dotnet run --project src/Library.Api
```

In PowerShell the variable stays set for the rest of the session; `Remove-Item Env:TEST_MODE` clears it.

### Option 2: Docker Compose with PostgreSQL

```bash
docker compose up --build                  # the API and PostgreSQL 17
TEST_MODE=1 docker compose up --build      # the same, in test mode
docker compose down -v                     # stop and remove the database volume
```

The API is on http://localhost:8000 again. The database port is not published, so it never clashes with a
PostgreSQL already running on your machine.

> **Note:** On first start against an empty PostgreSQL database, EF Core logs one failed query on
> `__EFMigrationsHistory` before it creates that table. It is expected and only happens once.

### Environment variables

| Variable | Default | Meaning |
|----------|---------|---------|
| `PORT` | `8000` | the port the API listens on |
| `DATABASE_URL` | `sqlite:///library.db` | `sqlite:///<file>` or `postgresql://user:password@host:5432/database` |
| `TEST_MODE` | unset (off) | `1` turns on `POST /test/reset` and the `X-Today` header. Never set it on a deployed server |
| `LIBRARY_TODAY` | unset | `YYYY-MM-DD`: fixes today for the whole server |

`DATABASE_URL` is converted to an Npgsql connection string in one place, `Data/DatabaseUrl.cs`. On a laptop the
API listens on `localhost` only; inside a container (the official .NET images set `DOTNET_RUNNING_IN_CONTAINER`)
it listens on every interface.

---

## Architecture

Four layers, each talking only to the one below it:

```
  HTTP request
       |
       v
+---------------------------+   Endpoints/, Contracts/, Errors/, Today/
|  API layer                |   routes, JSON in and out, status codes, the error format, X-Today
+---------------------------+
       |  plain values (ids, BookData, MemberData, today)
       v
+---------------------------+   Services/
|  Service layer            |   every business rule and its exact message. No HTTP, no SQL
+---------------------------+
       |  entities
       v
+---------------------------+   Data/Repositories/, Data/LibraryDbContext.cs
|  Repository layer         |   queries and saves through EF Core
+---------------------------+
       |
       v
+---------------------------+   SQLite file locally, PostgreSQL 17 in Docker Compose and CI,
|  Database                 |   tables built by Data/Migrations/
+---------------------------+
```

Where each rule lives:

| Rule | Where |
|------|-------|
| Loan policies (14 days/25p/3, 21 days/10p/5, 28 days/0p/10) | `Services/LoanPolicy.cs` |
| Due date, overdue, days late, fine (worked out, never stored) | `LoanService.Describe` |
| Borrowing checks in order: member, book, limit, copies | `LoanService.BorrowAsync` |
| Returning: already returned, not before borrowed | `LoanService.ReturnAsync` |
| Available = copies minus open loans; copies not below on loan; no deleting a lent book | `Services/BookService.cs` |
| Unique email, joined defaults to today | `Services/MemberService.cs` |
| Value rules: blanks, lengths, year range, formats, member types, email shape | `Validate` in each service |
| JSON shape: fields present, whole numbers that really are numbers, dates as `YYYY-MM-DD` | `Contracts/JsonBody.cs`, `Contracts/Requests.cs` |
| What is today (`X-Today` with `TEST_MODE=1`, then `LIBRARY_TODAY`, then Europe/London) | `Today/RequestToday.cs` |
| Exceptions, unknown routes and wrong methods to `{"error": {"code", "message"}}` | `Errors/ErrorHandlingMiddleware.cs` |

The service layer throws its own exceptions (`BookNotFound`, `LoanLimitReached`, ...) carrying the code and the
message from the contract's RULES.md. The middleware is the only place that decides the HTTP status for them.

A few framework defaults are switched off on purpose, because they break the contract:

- Request bodies are read as `JsonElement` and checked by hand, so `"101"`, `true` and `4.5` are a `400`, not
  silently converted, and every bad body answers in the contract's error format.
- Routes use `{id:int}`, so `/books/abc` matches nothing and is the normal `404 not_found`.
- The empty 404 and 405 answers from routing are rewritten into the error format by the same middleware.

### Two databases, two sets of migrations

EF Core migrations are written for one provider, so the model lives in `LibraryDbContext` and each database has a
small subclass with its own migrations: `SqliteLibraryDbContext` (`Data/Migrations/Sqlite`) and
`PostgreSqlLibraryDbContext` (`Data/Migrations/PostgreSql`). `DATABASE_URL` decides which one is registered. After
changing the model, add a migration for both:

```bash
dotnet tool restore
dotnet ef migrations add <Name> --project src/Library.Api --context SqliteLibraryDbContext --output-dir Data/Migrations/Sqlite
dotnet ef migrations add <Name> --project src/Library.Api --context PostgreSqlLibraryDbContext --output-dir Data/Migrations/PostgreSql
```

The sample data is `Data/seed.sql`, a copy of `db/seed.sql` from the contract, built into the assembly.
`POST /test/reset` (test mode only) empties the tables, loads it again and resets the id counters in one
transaction: `TRUNCATE ... RESTART IDENTITY` and `setval` on PostgreSQL; on SQLite, where EF Core declares the keys
`AUTOINCREMENT`, it also clears `sqlite_sequence`.

### Repository Structure

```
src/Library.Api/
  Program.cs               # services, middleware, MapXxxEndpoints calls, test endpoints when TEST_MODE=1
  LibrarySettings.cs       # PORT, DATABASE_URL, TEST_MODE, LIBRARY_TODAY
  appsettings.json         # log levels
  Endpoints/               # API layer: Book, Member, Loan, Health and Test endpoints
  Contracts/               # request parsing (JsonBody, Requests, QueryValues) and response records
  Errors/                  # ApiException, ErrorHandlingMiddleware -> the error format
  Today/                   # IToday and RequestToday, built on TimeProvider
  Services/                # Service layer: BookService, MemberService, LoanService, LoanPolicy, LibraryErrors
  Data/                    # Repository layer
    LibraryDbContext.cs    # the model, plus the SQLite and PostgreSQL subclasses
    DatabaseUrl.cs         # DATABASE_URL -> provider and connection string
    DatabaseSetup.cs       # registration, migrate and seed at start-up, design-time factories
    LibrarySeeder.cs       # seed on an empty database, reset for tests
    seed.sql               # copy of the contract's db/seed.sql
    Entities/              # Book, Member, Loan
    Configurations/        # table and column names, lengths, CHECK rules, indexes
    Repositories/          # BookRepository, MemberRepository, LoanRepository
    Migrations/Sqlite/     # EF Core migrations for SQLite
    Migrations/PostgreSql/ # EF Core migrations for PostgreSQL
tests/Library.UnitTests/   # xUnit tests for the service layer, no web server
.github/workflows/ci.yml   # build, unit tests, conformance on PostgreSQL and SQLite
Library.sln
global.json                # .NET 10 SDK, Microsoft.Testing.Platform for dotnet test
dotnet-tools.json          # dotnet-ef, pinned
Dockerfile                 # multi-stage: SDK to publish, ASP.NET runtime to run, non-root user
docker-compose.yml         # the API and postgres:17
```

---

## API

Every path, status code and JSON shape is in the contract's
[openapi.yaml](https://github.com/devfoundry-training/Library_API_Contract/blob/main/openapi.yaml), and the rules
behind them, with worked examples, are in
[RULES.md](https://github.com/devfoundry-training/Library_API_Contract/blob/main/RULES.md). In short:

| Method | Path | Success |
|--------|------|---------|
| GET | `/health` | 200 `{"status": "ok"}` |
| GET, POST | `/books` | 200 list (filters `title`, `author`, `genre`), 201 + `Location` |
| GET, PUT, DELETE | `/books/{id}` | 200, 200, 204 |
| GET, POST | `/members` | 200 list, 201 + `Location` |
| GET, PUT | `/members/{id}` | 200 |
| GET | `/members/{id}/loans` | 200 list (filters `active`, `overdue`) |
| GET, POST | `/loans` | 200 list (filters `member_id`, `active`, `overdue`), 201 + `Location` (borrow) |
| GET | `/loans/{id}` | 200 |
| POST | `/loans/{id}/return` | 200 |
| POST | `/test/reset` | 204, only with `TEST_MODE=1` |

---

## Running the Tests

### Unit tests

```bash
dotnet test
```

The tests call the services directly. Each test gets a fresh in-memory SQLite database built by the real
migrations and loaded with the sample data, and today is passed in as 2026-10-12, so no web server and no clock
are involved. They cover borrowing (the order of the checks, the limits, copies), dates and fines (each policy,
the year end, a leap year, due today, staff never fined, a returned loan's final fine), returning, every sample
loan from RULES.md, book and member changes, and the validation rules.

### Conformance suite

Start the API in test mode (see Getting Started), then from this folder:

```bash
py -3.13 ../Library_API_Contract/conformance/run.py --base-url http://localhost:8000    # Windows
python3 ../Library_API_Contract/conformance/run.py --base-url http://localhost:8000    # macOS/Linux
```

It prints one line per test and ends with `60 passed, 0 failed, 60 tests`. Run it against both databases:
once with `dotnet run` (SQLite) and once with `TEST_MODE=1 docker compose up --build` (PostgreSQL).

CI does the same on every push and pull request: build and unit tests, then the conformance suite against
PostgreSQL 17 (a service container) and against SQLite.

---

## What This Version Leaves Out

No authentication, pagination, caching, events or tracing. Those come in the advanced copies, each built on this
passing basic version; see
[Basic and Advanced](https://github.com/devfoundry-training/Library_API_Contract#basic-and-advanced) in the
contract.

---

## Tech Stack

| Technology | Purpose |
|------------|---------|
| .NET 10, C# | Language and runtime |
| ASP.NET Core 10 minimal API | HTTP endpoints |
| Entity Framework Core 10.0.12 | ORM and migrations |
| Microsoft.EntityFrameworkCore.Sqlite 10.0.12 | SQLite provider (local runs) |
| Npgsql.EntityFrameworkCore.PostgreSQL 10.0.3 | PostgreSQL provider (Docker Compose and CI) |
| xUnit v3 4.0.2 on Microsoft.Testing.Platform | Unit tests |
| SQLite 3, PostgreSQL 17 | Databases |
| Docker, Docker Compose | Containers |
| GitHub Actions | CI |

No controllers, no AutoMapper, no MediatR: the minimal API and three small layers are the whole design.

---

## Related DevFoundry Courses

[ASP.NET Core Web APIs](https://devfoundry.co.uk/courses/aspnet-core-web-apis) ·
[Entity Framework Core](https://devfoundry.co.uk/courses/entity-framework-core) ·
[Building APIs with FastAPI](https://devfoundry.co.uk/courses/fastapi-apis) ·
[Django Web Development](https://devfoundry.co.uk/courses/django-web-development) ·
[Database Access with SQLAlchemy](https://devfoundry.co.uk/courses/sqlalchemy-python)

---

## What's Next?

The same contract, built in the other stacks of the series:

- [Library_API_FastAPI](https://github.com/devfoundry-training/Library_API_FastAPI): Python 3.13, FastAPI, SQLAlchemy 2.0, Alembic
- [Library_API_Django](https://github.com/devfoundry-training/Library_API_Django): Python 3.13, Django, Django REST framework
- [Library_API_SpringBoot](https://github.com/devfoundry-training/Library_API_SpringBoot): Java 21, Spring Boot 3, Spring Data JPA, Flyway
- [Library_API_Express](https://github.com/devfoundry-training/Library_API_Express): Node.js, Express, TypeScript (later)

Then the advanced copies, each adding one idea to this basic version: security (JWT and roles), scale (pagination,
caching, Redis), events (RabbitMQ or Kafka with an outbox), services (an API gateway), operations (OpenTelemetry)
and delivery (Kubernetes and Argo CD). The roadmap is in the
[contract README](https://github.com/devfoundry-training/Library_API_Contract#basic-and-advanced).

The library itself starts as a console program in [C# Zero to Hero](https://github.com/devfoundry-training/CS_zero_hero).
