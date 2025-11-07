# Job Displayer (.NET)

An ASP.NET Core web application that stores HiringCafe Job Watcher results, calculates a resume-based matching score, and surfaces the data through a responsive dashboard.

## Features

- SQLite-backed persistence for job postings and uploaded resumes.
- REST API (`/api/jobs`) for the HiringCafe Job Watcher to push new postings.
- Dashboard with timeframe filters (recent, last 24 hours, past 3 days, past 5 days) and ATS-style matching scores.
- Resume uploader that stores the latest resume and uses plain-text extraction for keyword comparisons.

## Project Structure

```
JobDisplayer.sln
└── JobDisplayer.Web/            # ASP.NET Core MVC project
    ├── Controllers/             # Dashboard UI and jobs ingestion API
    ├── Data/                    # Entity Framework Core DbContext
    ├── Models/                  # Job posting & resume entities
    ├── Services/                # Matching score calculator & timeframe helper
    ├── ViewModels/              # DTOs for UI/API
    ├── Views/                   # Razor views for the dashboard
    └── wwwroot/                 # Static assets (CSS/JS)
```

## Prerequisites

- [.NET 7 SDK](https://dotnet.microsoft.com/download)

## Running the Application

Restore dependencies and run the web project:

```bash
dotnet restore
dotnet run --project JobDisplayer.Web
```

The site listens on <https://localhost:5001> (and HTTP on `5000`). A SQLite database `app.db` is created in the project root if it does not exist.

## Environment Configuration

Connection strings are stored in `appsettings.json`. Override the database path by setting `ConnectionStrings:DefaultConnection` (environment variable `ConnectionStrings__DefaultConnection`).

## API Contract

### `POST /api/jobs`

Accepts an array of job postings:

```json
[
  {
    "jobTitle": "Senior Backend Engineer",
    "company": "Hiring Café",
    "location": "Remote",
    "salary": "$140k",
    "applyLink": "https://example.com/apply",
    "searchKey": "backend engineer",
    "description": "Work on the Hiring Café platform",
    "postedAt": "2023-10-17T13:45:00Z"
  }
]
```

- `jobTitle` and `company` are required.
- `postedAt` is optional (defaults to current time if omitted).
- Duplicates are ignored when the `applyLink` already exists.

### `GET /api/jobs?timeframe=24h`

Returns up to 500 most recent records ordered by `postedAt`. Optional `timeframe` filter supports `recent` (default), `24h`, `3d`,
`5d`.

## Resume Matching

Uploading a resume from the dashboard stores the file and extracts text for keyword matching (plain text files are recommended). Matching scores are calculated by overlapping keywords from the resume with each job’s title, company, location, search key, and description.

## Integrating the HiringCafe Job Watcher

Update the watcher workflow to invoke the API after fetching new jobs:

```bash
curl -X POST "https://<your-host>/api/jobs" \
     -H "Content-Type: application/json" \
     -d @jobs.json
```

This allows the dashboard to display the latest openings, matching scores, and filtering options.
