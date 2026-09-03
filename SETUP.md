# Setup guide â€” ABC Pharmacy

Local setup for the .NET 10 Web API and Angular 22 UI. For product overview and API docs, see [README.md](./README.md).

## Prerequisites

| Tool | Notes |
| --- | --- |
| [.NET 10 SDK](https://dotnet.microsoft.com/download) | `dotnet --version` should report `10.x` |
| [Node.js](https://nodejs.org/) 22+ (LTS) | Includes npm; UI targets Angular 22 / `npm@11` |

Confirm both are on your `PATH`:

```bash
dotnet --version
node --version
npm --version
```

## Manual setup (any OS, including Windows)

From the repository root:

```bash
dotnet restore abc-pharmacy.slnx
dotnet build abc-pharmacy.slnx -c Debug

cd pharmacy-ui
npm install
```

Without the solution file, restore/build the projects individually:

```bash
dotnet restore PharmacyApi/PharmacyApi.csproj
dotnet build PharmacyApi/PharmacyApi.csproj -c Debug
dotnet restore PharmacyApi.Tests/PharmacyApi.Tests.csproj
dotnet build PharmacyApi.Tests/PharmacyApi.Tests.csproj -c Debug
```

## Run the API

```bash
cd PharmacyApi
dotnet run --launch-profile http
```

| Profile | URLs |
| --- | --- |
| `http` (recommended for local UI) | `http://localhost:5198` |
| `https` | `https://localhost:7228` and `http://localhost:5198` |

Launch profiles are defined in `PharmacyApi/Properties/launchSettings.json`. On first start, if `PharmacyApi/Data/medicines.json` and `sales.json` are missing, the API seeds sample medicines.

## Run the UI

Start the API first, then in a second terminal:

```bash
cd pharmacy-ui
npm start
```

- App: `http://localhost:4200`
- `environment.apiBaseUrl` is `/api`
- Dev proxy (`proxy.conf.json`, wired via `angular.json`) forwards `/api` â†’ `http://localhost:5198`

CORS on the API allows `http://localhost:4200` and `https://localhost:4200` (policy `AngularDev`).

## Run tests

```bash
# From repo root
dotnet test PharmacyApi.Tests/PharmacyApi.Tests.csproj
```

Or via the solution:

```bash
dotnet test abc-pharmacy.slnx
```

Controller unit tests mock `IMedicineRepository` (no disk I/O).

Optional UI build check:

```bash
cd pharmacy-ui
npm run build
```

## Troubleshooting

**`dotnet` / `node` / `npm` not found**  
Install the tools above and ensure their install directories are on `PATH`. Open a new terminal after installing. On Windows, Node.js and the .NET SDK each add their own PATH entries during install.

**Wrong .NET major version**  
Install the .NET 10 SDK if `dotnet --version` is not `10.x` and restore/build fails.

**UI cannot reach the API**  
Confirm the API is listening on `http://localhost:5198` with the `http` launch profile. The Angular proxy only targets that URL. Restart `npm start` after changing `proxy.conf.json`.

**Port already in use**  
Stop whatever is bound to `5198` (API) or `4200` (UI), or change the launch profile / `ng serve` port intentionally and update the proxy/CORS to match.

**`npm install` fails in `pharmacy-ui`**  
Use Node 22+ and a recent npm (project notes `npm@11`). Delete `node_modules` and retry `npm install` from `pharmacy-ui/`.
