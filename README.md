# HiringCafe Job Tracker

A full-stack application that ingests HiringCafe job watcher payloads, stores them in SQL Server, and surfaces a resume-aware dashboard for managing your pipeline.

## Project Structure

```
HiringCafeTracker/
 ├── Backend/
 │   ├── Controllers/
 │   ├── Data/
 │   ├── DTOs/
 │   ├── Models/
 │   ├── Services/
 │   ├── HiringCafeTracker.Backend.csproj
 │   ├── Program.cs
 │   └── appsettings.json
 ├── Frontend/
 │   ├── src/
 │   ├── index.html
 │   ├── package.json
 │   ├── tailwind.config.js
 │   └── vite.config.js
 └── Database/
     └── schema.sql
```

## Backend Setup (ASP.NET Core 8 + SQL Server)

1. **Restore dependencies**
   ```bash
   cd HiringCafeTracker/Backend
   dotnet restore
   ```

2. **Apply database schema**
   * Update the `DefaultConnection` string in `appsettings.json` if needed.
   * Either run the SQL in `../Database/schema.sql` on your SQL Server instance or use EF Core migrations (`dotnet ef database update`).

3. **Configure API key**
   * Set the `ApiKey` value in `appsettings.json` (or via environment variable `ApiKey`).
   * The GitHub Job Watcher must supply this key in the `X-API-KEY` header.

4. **Run the API**
   ```bash
   dotnet run --urls "https://localhost:5001;http://localhost:5000"
   ```

### API Reference

* `POST /api/jobs/import` — Bulk ingest HiringCafe jobs. Requires `X-API-KEY`.
* `GET /api/jobs` — Retrieve jobs with optional `timeframe`, `source`, and `status` query string filters.
* `POST /api/jobs/{id}/apply` — Mark a job as applied.
* `GET /api/jobs/sources` — List distinct sources for filter menus.
* `GET /api/companies` — Get unique companies and their apply links.
* `POST /api/resumes` — Upload a PDF/DOCX resume (updates matching scores).
* `GET /api/resumes/active` — Returns the active resume metadata.

### HiringCafe Watcher Payload Example

```bash
curl -X POST "https://localhost:5001/api/jobs/import" \
     -H "Content-Type: application/json" \
     -H "X-API-KEY: <your-api-key>" \
     -d '[{"JobId":"HC12345","JobTitle":".NET Developer","Company":"TechNova Systems","Location":"Austin, TX","Salary":"$90,000 – $110,000","Description":"Looking for experienced .NET Core developer...","ApplyLink":"https://hiringcafe.com/job/HC12345","SearchKey":".NET Developer","PostedTime":"2025-11-09T10:00:00","Source":"HiringCafe"}]'
```

## Frontend Setup (React + Vite + Tailwind)

1. **Install dependencies**
   ```bash
   cd HiringCafeTracker/Frontend
   npm install
   ```

2. **Start the dev server**
   ```bash
   npm run dev
   ```
   The Vite dev server proxies `/api` requests to `https://localhost:5001` by default.

3. **Build for production**
   ```bash
   npm run build
   ```

## Resume Matching

* Upload a PDF or DOCX resume via the dashboard.
* The backend extracts text with PdfPig/OpenXML and computes token overlap against job fields to derive the `MatchingScore`.
* Scores are recalculated automatically when jobs are imported or the resume changes.

## Seeding Data

If you want to seed records without the GitHub bot, you can:

1. Run the SQL statements in `Database/schema.sql` to create tables.
2. Insert sample rows into `Jobs` using SQL or the `/api/jobs/import` endpoint.
3. Use the dashboard to upload a resume and begin triaging jobs.

## Logging & Error Handling

* ASP.NET logging is configured for informational output.
* Resume uploads log failures server-side and return a generic 500 error to the client.
* The React UI displays friendly error banners for API failures.

## Security Notes

* Protect the ingestion endpoint with the `X-API-KEY` header.
* Enforce HTTPS when exposing the API publicly.
* Resume files are stored on disk (`resumes/` by default); ensure proper filesystem permissions in production.

## Testing

* Backend: `dotnet test` once you add unit/integration tests.
* Frontend: Add component tests with Vitest/React Testing Library as needed.

Happy tracking!
