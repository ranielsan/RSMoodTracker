# RS Mood Tracker

A daily employee mood check-in built with Angular 18, Angular Material, ASP.NET Core 8, Entity Framework Core, and MySQL. Employees submit without signing in; administrators sign in to view entries and statistics.

## Run with Docker

Install and start Docker Desktop with Linux containers enabled. From the repository root, run:

```shell
docker compose build
docker compose up
```

These are the only commands needed to run the application. Docker builds the frontend and backend images and starts the three services. The backend waits for MySQL's health check, applies migrations, and seeds initial data. No manual database setup is required. Ports 4200, 8080, 8081, and 3306 must be available.

| Page or service | Address |
| --- | --- |
| Employee check-in | http://localhost:4200/ |
| Admin sign-in | http://localhost:4200/admin/login |
| Admin dashboard | http://localhost:4200/admin/moods |
| Backend API | http://localhost:8080 |

Keep the terminal running while using the application; Ctrl+C stops it. Optionally use `docker compose up -d` for background operation and `docker compose logs -f` to view logs. After saving code changes, run the build and startup commands again.

## Demo account and data

| Setting | Value |
| --- | --- |
| Admin username | `admin` |
| Admin password | `RSMoodTracker!2026` |
| Employee IDs | `EMP001`, `EMP002`, `EMP003`, `EMP004`, `EMP005` |

Startup creates the admin account from the `AdminSeed` configuration. Changing the configured password does not reset an existing account's password.

Docker enables test mood seeding: one entry per employee for each UTC date **September 7–9, 2026**, giving 15 entries on a fresh database. Ratings vary and some comments are empty. Existing employee/date submissions are preserved. Restarting does not duplicate entries or reactivate inactive employees.

Both dashboard dates default to today in UTC. Select September 7–9, 2026 and apply the filters, or select Clear, to view the historical sample entries.

Test mood seeding defaults to disabled in `InterviewProjectTemplate/appsettings.json`. Docker enables it through `SeedData__IncludeTestMoodEntries=true`. Set this to `false` and recreate the backend container to disable future test seeding. This does not delete existing records. For local development outside Docker, the equivalent application setting is `SeedData:IncludeTestMoodEntries`.

MySQL stores records in the named `mysql-data` volume. Rebuilding application images preserves the data. A fresh clone on another computer creates its own database and seed data; it does not copy your local records. Removing the database volume deletes its contents.

## Employee submissions

Enter an active employee ID, select a mood, and optionally add a comment of up to 1,000 characters. The choices are:

- Not good at all
- A bit “meh”
- Pretty good
- Feeling great

IDs are trimmed and normalized to uppercase. One submission is allowed per employee per UTC calendar day, with the next day starting at midnight UTC. A database unique constraint enforces the employee/date rule, including competing requests.

## Admin dashboard

Filter by an inclusive UTC From/To date range and mood. Clear removes the filters. Entries appear newest first, with descending ID breaking timestamp ties. Available page sizes are 10, 25, and 50.

Summary cards show matching submission totals and each mood's count and percentage. Percentages represent submissions, not unique employees. Both date and mood filters affect the summary; pagination does not. Percentages are rounded to one decimal place, so their sum can differ slightly from 100%. Empty results show zero counts and percentages.

The authenticated `GET /api/admin/moods/dashboard` endpoint accepts `from` and `to` (`YYYY-MM-DD`), `rating` (1–4), `page` (default 1), and `pageSize` (default 10, maximum 100). It returns `entries`, `totalCount`, `page`, `pageSize`, and `statistics`. Pages beyond the final page are clamped to the final page. Invalid filters return HTTP 400. The original `GET /api/admin/moods` list endpoint remains available.

## Structure and design decisions

- `InterviewProjectTemplate`: backend. Controllers handle HTTP requests and responses, DTOs define exchanged data, services contain application logic, and EF Core handles persistence.
- `InterviewProjectTemplate.Tests`: backend service and controller unit tests.
- `Client/web-client`: Angular components, services, and tests. Nginx serves the built frontend and supports direct routes and browser refreshes.
- `docker-compose.yml`: container builds, connections, demo settings, and database storage.

The application uses a controller/service structure rather than CQRS. ASP.NET Core Identity manages admin passwords and roles. Admin authentication uses an HttpOnly cookie, with antiforgery protection for login/logout. The backend enforces admin authorization independently of the Angular route guard. UTC dates make the daily submission rule consistent across browsers.

## Tests

Local tests require the .NET 8 SDK for the backend, and Node.js compatible with Angular 18 plus Chrome for the frontend. These local tools are not required just to run the application through Docker.

From the repository root:

```shell
dotnet test InterviewProjectTemplate.sln
```

From `Client/web-client`, in Windows PowerShell:

```powershell
npm.cmd ci
npm.cmd test -- --watch=false --browsers=ChromeHeadless
npm.cmd run build
```

Use `npm` instead of `npm.cmd` in other shells. Tests cover submission validation, employee checks, the daily rule, authentication behavior, dashboard filtering and statistics, and frontend forms. Backend database unit tests use EF Core's in-memory provider; they do not verify MySQL-specific behavior or HTTP authorization filters. Docker startup, seeding/restart, authentication, and dashboard API behavior have also been checked against MySQL manually.

## Known limitations

- Employees are not authenticated. Someone who knows another valid ID can submit using it. Administrators can see employee IDs and comments, so submissions are not anonymous.
- Supplied credentials and HTTP/localhost settings are for demonstration. Server deployment needs appropriate secrets, HTTPS, API URLs, and allowed origins.
- Authentication keys are not persisted explicitly. Recreating the backend container may require administrators to sign in again.
- Employee administration and password recovery screens are not implemented.
- The Angular production build succeeds but exceeds its initial bundle warning budget. Lazy loading the admin area is a possible future optimization.
- Test mood dates are fixed historical dates and may not appear under the default today filter.
