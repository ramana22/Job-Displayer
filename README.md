# Job Displayer

A lightweight Flask application that stores the positions surfaced by the HiringCafe Job Watcher, calculates a resume-based matching score, and displays everything in a web dashboard.

## Features

- REST API to ingest job postings (`POST /api/jobs`).
- Resume upload endpoint (`POST /api/resume`) to keep the latest resume on record.
- Matching score calculation between the active resume and every job.
- Dashboard that lists jobs in a sortable table with timeframe filters (Recent, Last 24 Hours, Past 3 Days, Past 5 Days).

## Getting Started

### Requirements

- Python 3.10+
- pip

### Installation

```bash
python -m venv .venv
source .venv/bin/activate
pip install -r requirements.txt
```

### Running the application

```bash
flask --app app run --debug
```

The application starts on <http://localhost:5000>. The default SQLite database is created automatically as `jobs.db`. You can override the database path by setting the `JOB_DISPLAYER_DATABASE` environment variable to a SQLAlchemy compatible URI.

### API Summary

- `POST /api/jobs`
  - Accepts either a single job object or a list of jobs.
  - Each job supports the following keys: `job_title`, `company`, `location`, `salary`, `apply_link`, `search_key`, `description`, `posted_at` (ISO 8601 string).
- `GET /api/jobs?timeframe=<recent|24h|3d|5d>`
  - Returns the stored jobs sorted by newest first and the calculated matching score.
- `POST /api/resume`
  - Accepts a `multipart/form-data` upload (`file` field). The most recently uploaded resume is used to compute scores.
- `GET /api/resume`
  - Retrieves metadata about the most recently uploaded resume.

### HiringCafe Job Watcher Integration

Configure your HiringCafe Job Watcher workflow to `POST` the JSON payload directly to `/api/jobs`. The application deduplicates jobs by `apply_link` when provided.

## Matching Score

The matching score is a simple keyword overlap between the resume and each job (using job title, company, location, salary, search key, and description). Uploading a resume enables the score calculation; otherwise the table prompts the user to upload one.
