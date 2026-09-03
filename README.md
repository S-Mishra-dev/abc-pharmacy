# ABC Pharmacy

Pharmacy Medicine Management SPA: track inventory, add medicines, and sell stock with expiry/low-stock alerts.

**Setup:** see [SETUP.md](./SETUP.md) for prerequisites, install steps, how to run the API/UI, tests, and troubleshooting.

## Architecture

| Layer | Stack | Location |
| --- | --- | --- |
| API | .NET 10 Web API (C# 12) | `PharmacyApi/` |
| UI | Angular 22 standalone SPA (zoneless, signals) | `pharmacy-ui/` |
| Tests | xUnit + Moq | `PharmacyApi.Tests/` |

**Storage:** Persistent JSON under `PharmacyApi/Data/` (`medicines.json`, `sales.json`) via `JsonMedicineRepository`, guarded by `SemaphoreSlim` for thread-safe async I/O. Sample medicines are seeded on first API start if the files are missing.

**Error handling:** Unhandled exceptions map to RFC 7807 `ProblemDetails` through `GlobalExceptionHandler` (`IExceptionHandler`). Request DTOs use DataAnnotations (`ValidationProblem` on failure). Insufficient stock returns `409 Conflict`.

## Key features

- Medicine inventory list with name/brand filter
- Add medicine (Angular signal forms + inline validation)
- Sell flow with quantity checks and stock decrement
- Row alerts: expiry &lt; 30 days (`#f8d7da`), quantity &lt; 10 (`#fff3cd`), both (`#f7c697`)
- Sale records appended to `sales.json`

## Quick start

Prerequisites: [.NET 10 SDK](https://dotnet.microsoft.com/download), [Node.js](https://nodejs.org/) 22+ (LTS) and npm.

From the repo root (Windows or any OS Ã¢â‚¬â€ manual):

```bash
dotnet restore abc-pharmacy.slnx
dotnet build abc-pharmacy.slnx -c Debug
cd pharmacy-ui && npm install
```

Full details: [SETUP.md](./SETUP.md).

## Project structure

```
abc-pharmacy/
Ã¢â€Å“Ã¢â€â‚¬Ã¢â€â‚¬ PharmacyApi/                 # Web API
Ã¢â€â€š   Ã¢â€Å“Ã¢â€â‚¬Ã¢â€â‚¬ Controllers/             # MedicinesController
Ã¢â€â€š   Ã¢â€Å“Ã¢â€â‚¬Ã¢â€â‚¬ Data/                    # medicines.json, sales.json (runtime)
Ã¢â€â€š   Ã¢â€Å“Ã¢â€â‚¬Ã¢â€â‚¬ Infrastructure/          # GlobalExceptionHandler
Ã¢â€â€š   Ã¢â€Å“Ã¢â€â‚¬Ã¢â€â‚¬ Models/                  # Medicine, requests, SaleRecord
Ã¢â€â€š   Ã¢â€â€Ã¢â€â‚¬Ã¢â€â‚¬ Services/                # IMedicineRepository, JsonMedicineRepository
Ã¢â€Å“Ã¢â€â‚¬Ã¢â€â‚¬ PharmacyApi.Tests/           # Controller + exception-handler unit tests
Ã¢â€Å“Ã¢â€â‚¬Ã¢â€â‚¬ pharmacy-ui/                 # Angular SPA
Ã¢â€â€š   Ã¢â€Å“Ã¢â€â‚¬Ã¢â€â‚¬ proxy.conf.json          # Dev proxy /api Ã¢â€ â€™ API
Ã¢â€â€š   Ã¢â€â€Ã¢â€â‚¬Ã¢â€â‚¬ src/app/
Ã¢â€â€š       Ã¢â€Å“Ã¢â€â‚¬Ã¢â€â‚¬ components/medicine-dashboard/
Ã¢â€â€š       Ã¢â€Å“Ã¢â€â‚¬Ã¢â€â‚¬ models/
Ã¢â€â€š       Ã¢â€â€Ã¢â€â‚¬Ã¢â€â‚¬ services/
Ã¢â€Å“Ã¢â€â‚¬Ã¢â€â‚¬ abc-pharmacy.slnx
Ã¢â€Å“Ã¢â€â‚¬Ã¢â€â‚¬ SETUP.md
```

## Run the API

```bash
cd PharmacyApi
dotnet run --launch-profile http
```

Listens on `http://localhost:5198` (see `Properties/launchSettings.json`). HTTPS profile also available (`https://localhost:7228`).

## Run the UI

In a second terminal (API should already be running):

```bash
cd pharmacy-ui
npm start
```

Open `http://localhost:4200`. The Angular dev server proxies `/api` to `http://localhost:5198` via `proxy.conf.json`.

## Run tests

```bash
dotnet test PharmacyApi.Tests/PharmacyApi.Tests.csproj
```

Or from the solution:

```bash
dotnet test abc-pharmacy.slnx
```

Tests mock `IMedicineRepository` Ã¢â‚¬â€ no disk I/O during unit tests.

## API endpoints

| Method | Path | Success | Notes |
| --- | --- | --- | --- |
| `GET` | `/api/medicines` | `200` | List all medicines |
| `GET` | `/api/medicines/{id}` | `200` / `404` | Single medicine |
| `POST` | `/api/medicines` | `201` | Create; validates price Ã¢â€°Â¤ 2 decimal places |
| `POST` | `/api/medicines/{id}/sell` | `200` | Body: `{ "quantity": n }`; `409` if insufficient stock |

## Configuration notes

- **CORS:** API policy `AngularDev` allows `http://localhost:4200` and `https://localhost:4200`.
- **Proxy:** UI `environment.apiBaseUrl` is `/api`; `angular.json` serve uses `proxy.conf.json`.
- **Zoneless Angular:** Angular 22 apps are zoneless by default (no `zone.js`); see comment in `app.config.ts`.
- **JSON naming:** API uses camelCase JSON; decimals serialize to 2 decimal places.

## Development notes (contributors)

- Keep JSON persistence on disk with `SemaphoreSlim` Ã¢â‚¬â€ do not replace with unsynced in-memory-only storage.
- Prefer Angular signals / signal forms and native `@if` / `@for`; no legacy template-driven forms.
- UI alert colors: expiry `#f8d7da`, low stock `#fff3cd`, combined `#f7c697`.
- API unit tests should mock `IMedicineRepository` and cover status codes for list/create/sell edge cases.
- Strict typing, explicit return types, no `any`, zero build warnings.

## Build checks

```bash
dotnet build abc-pharmacy.slnx
cd pharmacy-ui
npm run build
```

