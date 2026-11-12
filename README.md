# BeautyBook

A booking platform for beauty services. Clients find independent providers and salons, book a time slot, pay and leave reviews. Providers manage their services and weekly schedule, salon owners manage their staff, and admins manage the whole platform.

The solution contains a REST API, a server-rendered web app with role-based areas, a separate MVC client that talks to the API, and a separate Vue 3 single-page client that talks to the same API.

## Features

**Clients**
- Browse and filter providers and salons, and view their services and portfolios
- Book a free time slot, view and cancel your own bookings
- Pay for bookings and see your payment history
- Rate and review providers, with comments
- Save favourite providers
- In-app notifications

**Service providers**
- Edit profile and portfolio
- Create and edit services, each with a category and type
- Set a weekly schedule and breaks. Bookable slots are generated from them for the coming weeks
- Review incoming booking requests. Requests that nobody answers expire automatically

**Salon owners**
- Manage their salons, staff and services, and see their bookings and dashboard

**Admins**
- Dashboard plus management of users, providers, salons, bookings, reviews, services, service categories and service types

**Cross-cutting**
- Two UI languages (English and Estonian). The interface uses `.resx` resource files, and service names and categories are translated in the database with a `LangStr` type
- Users can only read and change their own data in the API (IDOR protection), covered by tests

## Architecture

| Project | Purpose |
|---|---|
| `Base.Domain`, `Base.Contracts`, `Base.Helpers` | Shared building blocks: base entity, `LangStr`, helpers |
| `App.Domain` | Domain entities (bookings, payments, providers, salons, schedules, reviews, identity, ...) |
| `App.Contracts` | Interfaces for repositories, unit of work and services |
| `App.DAL.EF` | EF Core DbContext, entity configurations, migrations, repositories, unit of work, seed data |
| `App.BLL` | Business logic: booking rules, slot generation, reviews, service management, booking expiry |
| `App.DTO` | Public DTOs used by the API |
| `App.Resources` | Translation resources (`.resx`) |
| `WebApp` | REST API (versioned, Swagger), the web UI and the Admin, ServiceProvider and SalonOwner areas |
| `src/ClientApp` | Separate ASP.NET Core MVC client that consumes the REST API |
| `vue-client` | Separate Vue 3 + TypeScript client that consumes the REST API |
| `Tests.Unit`, `Tests.Integration` | xUnit tests |

The layers follow a clean/onion structure. Architecture tests check that, for example, the DAL doesn't reference the BLL.

**Stack:** .NET 10 · ASP.NET Core · Entity Framework Core · PostgreSQL 16 · ASP.NET Identity · JWT with refresh tokens · Swagger with API versioning · Vue 3 · TypeScript · Vite · Pinia · vue-i18n · Bootstrap 5 · Docker Compose · Playwright

## Running it with Docker

You need Docker. The stack starts the database, the API, the MVC client and the Vue client.

1. Create a `.env` file next to `docker-compose.yml`:
   ```env
   DB_PASSWORD=choose-a-database-password
   JWT_KEY=a-long-random-string-of-at-least-64-characters
   ADMIN_EMAIL=you@example.com
   ADMIN_PASSWORD=choose-a-strong-admin-password
   ```
   You can generate a key with `openssl rand -base64 64`. `ADMIN_EMAIL` and `ADMIN_PASSWORD` are optional: when both are set, an admin account with that login is created on startup.
2. Start everything:
   ```bash
   docker compose up --build
   ```

| Service | Address |
|---|---|
| REST API | http://localhost:82 (Swagger UI at `/swagger`) |
| MVC client | http://localhost:8081 |
| Vue client | http://localhost:8082 |

On first start the database is migrated and the roles are created. By default the stack runs in the `Production` environment, so **no demo accounts or demo data are created**. Log in with the admin account from your `.env` to get started.

To try the app with demo data locally, add `ASPNETCORE_ENVIRONMENT=Development` to `.env`. This seeds one demo account for each role and sample providers, salons and bookings (the accounts are defined in `App.DAL.EF/Seeding/InitialData.cs`). Never use the `Development` environment on a public server, because the demo passwords are public.

## Running it for development

You need the .NET 10 SDK, Node.js and a PostgreSQL server.

**API (`http://localhost:5065`)**
```bash
cd WebApp
dotnet user-secrets set "JWT:Key" "a-long-random-string-of-at-least-64-characters"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=beautybook;Username=postgres;Password=<your-password>"
dotnet run
```
The database is migrated and seeded on startup (see `DataInitialization` in `appsettings.json`). In the `Development` environment this includes the demo accounts and sample data described above.

**MVC client (`http://localhost:5024`)**
```bash
dotnet run --project src/ClientApp
```

**Vue client (`http://localhost:5173`)**
```bash
cd vue-client
npm install
npm run dev
```
The API address is set in `vue-client/.env.development`.

## Tests

```bash
dotnet test                      # unit and integration tests
cd vue-client && npm run test:e2e   # Playwright end-to-end tests (need the API running in Development, because they log in with a demo account)
```

- **Unit tests** include architecture tests that guard the layer dependencies.
- **Integration tests** run the API against an in-memory SQLite database and cover booking creation and cancellation, review creation and deletion, provider filtering, and authentication and IDOR protection.
- **End-to-end tests** (Playwright) cover login and logout in the Vue client.

## Deployment

`.gitlab-ci.yml` deploys the Docker Compose stack when changes reach `main`. It expects `DB_PASSWORD` and `JWT_KEY` to be set as CI variables. Set `ADMIN_EMAIL` and `ADMIN_PASSWORD` as well to create an admin account on a fresh database. The two clients are separate containers, and the API allows them through CORS.

## Notes

Built as a personal project for the C# web application development course at TalTech.
