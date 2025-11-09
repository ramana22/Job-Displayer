import os
import re
from datetime import datetime, timedelta

from flask import Flask, jsonify, render_template, request
from flask_sqlalchemy import SQLAlchemy
from sqlalchemy import func

app = Flask(__name__)

DATABASE_URL = os.environ.get("JOB_DISPLAYER_DATABASE", "sqlite:///jobs.db")
app.config["SQLALCHEMY_DATABASE_URI"] = DATABASE_URL
app.config["SQLALCHEMY_TRACK_MODIFICATIONS"] = False

db = SQLAlchemy(app)


class Job(db.Model):
    __tablename__ = "jobs"

    id = db.Column(db.Integer, primary_key=True)
    job_title = db.Column(db.String(255), nullable=False)
    company = db.Column(db.String(255), nullable=True)
    location = db.Column(db.String(255), nullable=True)
    salary = db.Column(db.String(255), nullable=True)
    apply_link = db.Column(db.String(1024), nullable=True)
    search_key = db.Column(db.String(255), nullable=True)
    description = db.Column(db.Text, nullable=True)
    posted_at = db.Column(db.DateTime, nullable=True)
    created_at = db.Column(db.DateTime, nullable=False, default=datetime.utcnow)


class Resume(db.Model):
    __tablename__ = "resumes"

    id = db.Column(db.Integer, primary_key=True)
    filename = db.Column(db.String(255), nullable=False)
    content = db.Column(db.Text, nullable=False)
    uploaded_at = db.Column(db.DateTime, nullable=False, default=datetime.utcnow)


with app.app_context():
    db.create_all()


TOKEN_PATTERN = re.compile(r"[A-Za-z0-9]+")


def tokenize(text: str) -> set[str]:
    if not text:
        return set()
    tokens = [match.group(0).lower() for match in TOKEN_PATTERN.finditer(text)]
    return {token for token in tokens if len(token) > 2}


def get_active_resume_tokens() -> tuple[set[str], str | None]:
    resume = (
        Resume.query.order_by(Resume.uploaded_at.desc()).first()
    )
    if not resume:
        return set(), None
    return tokenize(resume.content), resume.filename


def compute_matching_score(job: Job, resume_tokens: set[str]) -> float | None:
    if not resume_tokens:
        return None

    job_text_parts = [
        job.job_title,
        job.company,
        job.location,
        job.salary,
        job.search_key,
        job.description,
    ]
    job_tokens = tokenize(" ".join(filter(None, job_text_parts)))
    if not job_tokens:
        return 0.0

    overlap = job_tokens.intersection(resume_tokens)
    score = (len(overlap) / len(job_tokens)) * 100
    return round(score, 2)


@app.route("/")
def index():
    return render_template("index.html")


@app.post("/api/resume")
def upload_resume():
    if "file" not in request.files:
        return jsonify({"error": "No file part in the request."}), 400

    file_storage = request.files["file"]
    if file_storage.filename == "":
        return jsonify({"error": "No file selected."}), 400

    content_bytes = file_storage.read()
    try:
        content = content_bytes.decode("utf-8")
    except UnicodeDecodeError:
        content = content_bytes.decode("latin-1", errors="ignore")

    resume = Resume(filename=file_storage.filename, content=content)
    db.session.add(resume)
    db.session.commit()

    return jsonify({
        "message": "Resume uploaded successfully.",
        "filename": resume.filename,
        "uploaded_at": resume.uploaded_at.isoformat(),
    })


@app.get("/api/resume")
def get_resume():
    resume = Resume.query.order_by(Resume.uploaded_at.desc()).first()
    if not resume:
        return jsonify({"resume": None})

    return jsonify(
        {
            "resume": {
                "filename": resume.filename,
                "uploaded_at": resume.uploaded_at.isoformat(),
            }
        }
    )


@app.post("/api/jobs")
def ingest_jobs():
    payload = request.get_json(silent=True)
    if payload is None:
        return jsonify({"error": "Invalid or missing JSON payload."}), 400

    jobs_data = payload if isinstance(payload, list) else [payload]
    created_jobs = []

    for entry in jobs_data:
        job_title = entry.get("job_title") or entry.get("title")
        if not job_title:
            continue

        apply_link = entry.get("apply_link") or entry.get("link")
        search_key = entry.get("search_key")

        posted_at_value = entry.get("posted_at") or entry.get("date_posted")
        posted_at = None
        if posted_at_value:
            try:
                posted_at = datetime.fromisoformat(posted_at_value)
            except (TypeError, ValueError):
                posted_at = None

        job = Job(
            job_title=job_title,
            company=entry.get("company"),
            location=entry.get("location"),
            salary=entry.get("salary"),
            apply_link=apply_link,
            search_key=search_key,
            description=entry.get("description"),
            posted_at=posted_at,
        )

        existing = None
        if apply_link:
            existing = Job.query.filter(
                func.lower(Job.apply_link) == apply_link.lower()
            ).first()

        if existing:
            existing.job_title = job.job_title
            existing.company = job.company
            existing.location = job.location
            existing.salary = job.salary
            existing.search_key = job.search_key
            existing.description = job.description
            existing.posted_at = job.posted_at
            created_jobs.append(existing.id)
        else:
            db.session.add(job)
            db.session.flush()
            created_jobs.append(job.id)

    db.session.commit()
    return jsonify({"job_ids": created_jobs}), 201


@app.get("/api/jobs")
def list_jobs():
    timeframe = request.args.get("timeframe", "recent")
    query = Job.query.order_by(Job.created_at.desc())

    if timeframe and timeframe != "recent":
        now = datetime.utcnow()
        delta_map = {
            "24h": timedelta(hours=24),
            "3d": timedelta(days=3),
            "5d": timedelta(days=5),
        }
        delta = delta_map.get(timeframe)
        if delta:
            cutoff = now - delta
            query = query.filter(Job.created_at >= cutoff)

    resume_tokens, resume_filename = get_active_resume_tokens()

    jobs_response = []
    for job in query:
        score = compute_matching_score(job, resume_tokens)
        jobs_response.append(
            {
                "id": job.id,
                "job_title": job.job_title,
                "company": job.company,
                "location": job.location,
                "salary": job.salary,
                "apply_link": job.apply_link,
                "search_key": job.search_key,
                "description": job.description,
                "matching_score": score,
                "created_at": job.created_at.isoformat(),
                "posted_at": job.posted_at.isoformat() if job.posted_at else None,
            }
        )

    return jsonify(
        {
            "jobs": jobs_response,
            "resume": {
                "filename": resume_filename,
                "available": bool(resume_tokens),
            },
        }
    )


if __name__ == "__main__":
    app.run(host="0.0.0.0", port=8000, debug=True)
