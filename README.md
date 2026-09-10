# InterviewProjectTemplate

## Project Structure

The backend code is located in the `InterviewProjectTemplate` directory. The frontend is under `Client/web-client`.

## Running

Once the codebase has been cloned, it can be run using the following two commands (note that these commands should be run from the top level directory, where the `docker-compose.yml` file is located):

`docker compose build`

`docker compose up`

This will build and run the ASP.NET Core backend, the Angular frontend, and the MySQL database inside a docker container.

## Database

A blank MySQL database is included inside the container, and will start up when the container is run. The ASP.NET Core backend is already configured with a connection string to this database.

## Frontend

The frontend will be run on `http://localhost:4200`. When making API calls to the backend, please ensure that `environment.apiUrl` is used for the URL, rather than hardcoding the value. This will ensure that we can easily re-configure the URL if needed for deployment.

## Admin dashboard

Open `http://localhost:4200/admin/moods` after signing in. Both calendar date pickers default to today in UTC on opening the dashboard. Apply a different UTC From/To date range and mood filter, or Clear to return to all dates and moods. Dates are inclusive and use the submission's UTC calendar day. Entries remain newest first, with ID descending to break timestamp ties.

Summary cards count submissions, not unique employees. Each percentage is the mood count divided by all matching submissions, rounded to one decimal place; rounded percentages can sum to slightly more or less than 100%. Both date and mood filters affect the summary. Pagination does not affect the summary. Empty results show all four moods at zero.

The authenticated `GET /api/admin/moods/dashboard` endpoint accepts `from` and `to` (`YYYY-MM-DD`), `rating` (1–4), `page` (default 1), and `pageSize` (default 10, maximum 100). It returns `entries`, `totalCount`, `page`, `pageSize`, and `statistics`. Pages beyond the final page are clamped to the final page. Invalid ranges, ratings and page parameters return 400. The original list endpoint remains available.

Run backend tests from the repository root with `dotnet test InterviewProjectTemplate.sln`. Run frontend tests from `Client/web-client` with `npm.cmd test -- --watch=false --browsers=ChromeHeadless` (Chrome must be installed).

