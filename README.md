# GasTracker

A mobile-optimised personal fuel tracking app. Log fill-ups per vehicle, monitor fuel efficiency, and track running costs over time.

![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4)
![Blazor Server](https://img.shields.io/badge/Blazor-Server-7B2FBE)
![SQLite](https://img.shields.io/badge/SQLite-EF%20Core%2010-003B57)

---

## Features

- **Multi-vehicle support** — add as many cars as you like
- **Fill-up history** — record odometer, volume, cost, date, and notes per fill-up
- **Fuel efficiency** — L/km, L/10 km, L/100 km, or MPG depending on your unit preference
- **Cost tracking** — cost per km/mi with configurable scale (×1, ×10, ×100)
- **Stats dashboard** — line chart (efficiency) and bar chart (cost) with 30 d / 90 d / 1 y / All filters
- **Unit preferences** — metric (km / litres) or imperial (miles / gallons), plus custom currency symbol
- **Google OAuth login** — no passwords; users are provisioned on first sign-in

---

## Tech stack

| Layer | Technology |
|---|---|
| Framework | .NET 10, ASP.NET Core, Blazor Web App (Interactive Server) |
| Database | SQLite via EF Core 10, Repository + Unit of Work pattern |
| UI | Blazorise 1.7 + Bootstrap 5 + Font Awesome 6 |
| Charts | Blazorise.Charts (Chart.js 4) |
| Auth | Google OAuth 2.0 + ASP.NET Core cookie auth |
| Tests | xUnit, EF Core InMemory, Moq |

---

## Project structure

```
GasTracker/
├── src/
│   ├── GasTracker.Data/          # EF Core entities, repositories, migrations
│   └── GasTracker.Web/           # Blazor app, pages, services
├── tests/
│   ├── GasTracker.Data.Tests/    # Repository tests (EF InMemory)
│   └── GasTracker.Services.Tests/# Unit tests for calc + conversion services
├── Dockerfile
├── docker-compose.yml
└── .env.example
```

---

## Running locally

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)

### Development (no Google credentials needed)

```bash
dotnet run --project src/GasTracker.Web
```

Navigate to `https://localhost:5001` and click **Dev login** on the login page. The dev login is only visible when `ASPNETCORE_ENVIRONMENT=Development`.

The SQLite database is created automatically at `./data/gastracker.db` on first run.

### With Google OAuth

1. Create OAuth 2.0 credentials in [Google Cloud Console](https://console.cloud.google.com/)
2. Add `https://localhost:5001/signin-google` as an authorised redirect URI
3. Store the credentials using .NET user secrets:

```bash
dotnet user-secrets set "Authentication:Google:ClientId"     "<your-client-id>"     --project src/GasTracker.Web
dotnet user-secrets set "Authentication:Google:ClientSecret" "<your-client-secret>" --project src/GasTracker.Web
dotnet run --project src/GasTracker.Web
```

### Running tests

```bash
dotnet test
```

---

## Deployment (Docker)

```bash
# 1. Copy the example env file and fill in your credentials
cp .env.example .env

# 2. Build and start
docker compose up -d
```

The app listens on **port 8080** (HTTP). Put Nginx, Caddy, or Traefik in front for TLS termination.

Before going live, add your production domain to the authorised redirect URIs in Google Cloud Console:

```
https://yourdomain.com/signin-google
```

The SQLite database is persisted in a Docker volume (`gastracker-data`) and survives container restarts and rebuilds. Migrations run automatically on startup — no manual DB steps needed.

### Environment variables

| Variable | Description |
|---|---|
| `GOOGLE_CLIENT_ID` | OAuth 2.0 Client ID |
| `GOOGLE_CLIENT_SECRET` | OAuth 2.0 Client Secret |
| `ConnectionStrings__DefaultConnection` | SQLite connection string (default: `Data Source=/app/data/gastracker.db`) |
| `ASPNETCORE_ENVIRONMENT` | Set to `Production` for production builds |

---

## Configuration

User preferences are stored per account and configurable from the **Profile** page:

| Setting | Options |
|---|---|
| Preferred unit | km / litres (metric) · miles / gallons (imperial) |
| Stats scale | ×1 · ×10 · ×100 — controls the denominator for efficiency and cost labels |
| Currency | ISO code (e.g. `SEK`) + symbol (e.g. `kr`) |
