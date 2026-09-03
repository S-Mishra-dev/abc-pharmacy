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

From the repo root (Windows or any OS â€” manual):

```bash
dotnet restore abc-pharmacy.slnx
dotnet build abc-pharmacy.slnx -c Debug
cd pharmacy-ui && npm install
```

On macOS/Linux (or Git Bash), you can use `./setup.sh` instead. Full details: [SETUP.md](./SETUP.md).

## Project structure

```
abc-pharmacy/
â”œâ”€â”€ PharmacyApi/                 # Web API
â”‚   â”œâ”€â”€ Controllers/             # MedicinesController
â”‚   â”œâ”€â”€ Data/                    # medicines.json, sales.json (runtime)
â”‚   â”œâ”€â”€ Infrastructure/          # GlobalExceptionHandler
â”‚   â”œâ”€â”€ Models/                  # Medicine, requests, SaleRecord
â”‚   â””â”€â”€ Services/                # IMedicineRepository, JsonMedicineRepository
â”œâ”€â”€ PharmacyApi.Tests/           # Controller + exception-handler unit tests
â”œâ”€â”€ pharmacy-ui/                 # Angular SPA
â”‚   â”œâ”€â”€ proxy.conf.json          # Dev proxy /api â†’ API
â”‚   â””â”€â”€ src/app/
â”‚       â”œâ”€â”€ components/medicine-dashboard/
â”‚       â”œâ”€â”€ models/
â”‚       â””â”€â”€ services/
â”œâ”€â”€ abc-pharmacy.slnx
â”œâ”€â”€ SETUP.md
â””â”€â”€ setup.sh                     # Optional automated setup (Unix / Git Bash)
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

Tests mock `IMedicineRepository` â€” no disk I/O during unit tests.

## API endpoints

| Method | Path | Success | Notes |
| --- | --- | --- | --- |
| `GET` | `/api/medicines` | `200` | List all medicines |
| `GET` | `/api/medicines/{id}` | `200` / `404` | Single medicine |
| `POST` | `/api/medicines` | `201` | Create; validates price â‰¤ 2 decimal places |
| `POST` | `/api/medicines/{id}/sell` | `200` | Body: `{ "quantity": n }`; `409` if insufficient stock |

## Configuration notes

- **CORS:** API policy `AngularDev` allows `http://localhost:4200` and `https://localhost:4200`.
- **Proxy:** UI `environment.apiBaseUrl` is `/api`; `angular.json` serve uses `proxy.conf.json`.
- **Zoneless Angular:** Angular 22 apps are zoneless by default (no `zone.js`); see comment in `app.config.ts`.
- **JSON naming:** API uses camelCase JSON; decimals serialize to 2 decimal places.

## Development notes (contributors)

- Keep JSON persistence on disk with `SemaphoreSlim` â€” do not replace with unsynced in-memory-only storage.
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
