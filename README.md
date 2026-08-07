# ecommerce-dotnet

eCommerce backend application, built on .NET 10 following Clean Architecture.

## Solution layout

```
eCommerce/
├── Directory.Build.props        Shared MSBuild properties (TFM, nullable, langversion)
├── Directory.Packages.props     Central package management — all versions live here
├── .config/dotnet-tools.json    Pinned local tools (dotnet-ef)
├── src/
│   ├── eCommerce.Domain          Entities, value objects, domain events. No dependencies.
│   ├── eCommerce.Application     Use cases (MediatR CQRS), validation, ports.
│   ├── eCommerce.Infrastructure  EF Core, repositories, interceptors.
│   └── eCommerce.Api             Controllers, DI composition, problem-details mapping.
└── tests/
    ├── eCommerce.Domain.UnitTests
    ├── eCommerce.Application.UnitTests
    └── eCommerce.Infrastructure.IntegrationTests
```

## The dependency rule

References point inwards only. `Domain` has no project or package references at all;
`Application` depends on `Domain`; `Infrastructure` depends on `Application`; `Api`
composes the lot and is the only project that knows every layer exists.

```
Api ──────────┐
  │           ▼
  ├──> Infrastructure ──> Application ──> Domain
  └────────────────────────────^
```

Where an inner layer needs something an outer layer provides, it declares an interface
and the outer layer implements it: `IProductRepository` and `IUnitOfWork` live in
`Domain`, `IDomainEventDispatcher` in `Application`, and all three are implemented in
`Infrastructure`.

## Request flow

`ProductsController` does no work beyond dispatching. A request travels:

```
Controller → MediatR → LoggingBehaviour → ValidationBehaviour → Handler → Repository
```

`ValidationBehaviour` runs every `AbstractValidator` registered for the request, so
handlers can assume well-formed input. Handlers load an aggregate, call a method on it,
and commit through `IUnitOfWork` — invariants stay inside the aggregate rather than in
the handler.

Domain events recorded by an aggregate are published by `ApplicationDbContext` *after*
`SaveChangesAsync` succeeds, so a handler never reacts to a transaction that was later
rolled back.

## Error handling

Exceptions map to RFC 9457 problem responses in `GlobalExceptionHandler`:

| Exception              | Status |
| ---------------------- | ------ |
| `ValidationException`  | 400 (with per-property errors) |
| `DomainException`      | 400 |
| `NotFoundException`    | 404 |
| `ConflictException`    | 409 |
| anything else          | 500 |

## Tests

| Project | Needs a database | Covers |
| ------- | ---------------- | ------ |
| `eCommerce.Domain.UnitTests` | no | Aggregate invariants and value objects |
| `eCommerce.Application.UnitTests` | no | Handlers, validators, pipeline behaviours |
| `eCommerce.Infrastructure.IntegrationTests` | **yes** | EF mapping, repository queries, interceptor, event dispatch |

The integration tests run against **SQL Server**, not the EF in-memory provider.
That is deliberate: the in-memory provider evaluates LINQ client-side, so it happily
accepts queries and value conversions that throw once they have to become real SQL.
The handler unit tests substitute `IProductRepository`, so nothing below the port is
exercised there either — value-converter and owned-type mapping bugs are only
reachable from this project.

Each run creates a uniquely named database, applies the committed migrations, and drops
it on teardown. LocalDB is the default; point the suite elsewhere with:

```powershell
$env:ECOMMERCE_TEST_CONNECTION = "Server=localhost,1433;User Id=sa;Password=...;TrustServerCertificate=True"
```

These tests fail rather than skip when no server is reachable — a suite that quietly
skips its only real coverage is worse than none.

## Getting started

```powershell
dotnet restore eCommerce/eCommerce.slnx
dotnet build   eCommerce/eCommerce.slnx
dotnet test    eCommerce/eCommerce.slnx

dotnet run --project eCommerce/src/eCommerce.Api
```

In Development the app applies migrations and seeds a few sample products on start-up.
The connection string defaults to LocalDB and lives in
`src/eCommerce.Api/appsettings.Development.json`. `appsettings.json` ships an empty
`DefaultConnection` — supply the real one per environment via user secrets or
environment variables, and do not commit it.

`src/eCommerce.Api/eCommerce.Api.http` exercises every endpoint, including the 400 and
409 paths.

### Migrations

`dotnet-ef` is a local tool, so restore it once with `dotnet tool restore`, then:

```powershell
cd eCommerce
dotnet dotnet-ef migrations add <Name> --project src/eCommerce.Infrastructure --startup-project src/eCommerce.Api --output-dir Persistence/Migrations
dotnet dotnet-ef database update --project src/eCommerce.Infrastructure --startup-project src/eCommerce.Api
```

`ApplicationDbContextFactory` supplies the design-time context, so these commands do not
boot the Api host. Point it at a different database with the
`ECOMMERCE_DESIGNTIME_CONNECTION` environment variable.

## Adding a use case

1. Add a command/query record plus its handler under
   `Application/Catalog/<Aggregate>/Commands|Queries/<UseCase>/`.
2. Add an `AbstractValidator` beside it — it is picked up by assembly scanning, with no
   registration needed.
3. Add an action to the controller that dispatches it.

Only step 3 touches an existing file.

## Package licensing

Two dependencies are deliberately held below their latest major version because newer
releases moved to a commercial licence. Both are pinned in `Directory.Packages.props`:

- **MediatR** — 12.5.0 is the last Apache-2.0 release (13.0.0+ is commercial).
- **FluentAssertions** — 7.2.2 is the last Apache-2.0 release (8.0.0+ is commercial).

Bump either if your organisation holds a licence.
