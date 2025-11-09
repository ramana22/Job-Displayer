const jobsBody = document.getElementById("jobs-body");
const timeframeSelect = document.getElementById("timeframe-select");
const resumeForm = document.getElementById("resume-form");
const resumeStatus = document.getElementById("resume-status");

async function fetchResumeStatus() {
  try {
    const response = await fetch("/api/resume");
    if (!response.ok) {
      throw new Error("Failed to fetch resume status");
    }
    const data = await response.json();
    if (data.resume) {
      const uploadedDate = new Date(data.resume.uploaded_at);
      resumeStatus.textContent = `Active resume: ${data.resume.filename} (uploaded ${uploadedDate.toLocaleString()})`;
    } else {
      resumeStatus.textContent = "No resume uploaded yet.";
    }
  } catch (error) {
    resumeStatus.textContent = "Unable to load resume details.";
  }
}

async function fetchJobs() {
  const timeframe = timeframeSelect.value;
  try {
    const response = await fetch(`/api/jobs?timeframe=${encodeURIComponent(timeframe)}`);
    if (!response.ok) {
      throw new Error("Failed to fetch jobs");
    }

    const data = await response.json();
    const jobs = data.jobs ?? [];

    jobsBody.innerHTML = "";

    if (jobs.length === 0) {
      const row = document.createElement("tr");
      const cell = document.createElement("td");
      cell.colSpan = 7;
      cell.textContent = "No job listings found for this timeframe.";
      cell.classList.add("text-center");
      row.appendChild(cell);
      jobsBody.appendChild(row);
      return;
    }

    jobs.forEach((job) => {
      const row = document.createElement("tr");

      const titleCell = document.createElement("td");
      titleCell.textContent = job.job_title ?? "-";
      row.appendChild(titleCell);

      const companyCell = document.createElement("td");
      companyCell.textContent = job.company ?? "-";
      row.appendChild(companyCell);

      const locationCell = document.createElement("td");
      locationCell.textContent = job.location ?? "-";
      row.appendChild(locationCell);

      const salaryCell = document.createElement("td");
      salaryCell.textContent = job.salary ?? "-";
      row.appendChild(salaryCell);

      const searchKeyCell = document.createElement("td");
      searchKeyCell.textContent = job.search_key ?? "-";
      row.appendChild(searchKeyCell);

      const scoreCell = document.createElement("td");
      if (job.matching_score === null || job.matching_score === undefined) {
        scoreCell.textContent = "Upload resume";
        scoreCell.classList.add("warning");
      } else {
        scoreCell.textContent = `${job.matching_score.toFixed(2)}%`;
      }
      row.appendChild(scoreCell);

      const linkCell = document.createElement("td");
      if (job.apply_link) {
        const anchor = document.createElement("a");
        anchor.href = job.apply_link;
        anchor.target = "_blank";
        anchor.rel = "noopener";
        anchor.textContent = "Apply";
        linkCell.appendChild(anchor);
      } else {
        linkCell.textContent = "-";
      }
      row.appendChild(linkCell);

      jobsBody.appendChild(row);
    });
  } catch (error) {
    jobsBody.innerHTML = "";
    const row = document.createElement("tr");
    const cell = document.createElement("td");
    cell.colSpan = 7;
    cell.textContent = "Error loading jobs.";
    cell.classList.add("text-center", "warning");
    row.appendChild(cell);
    jobsBody.appendChild(row);
  }
}

resumeForm.addEventListener("submit", async (event) => {
  event.preventDefault();
  const formData = new FormData(resumeForm);

  resumeStatus.textContent = "Uploading resume...";

  try {
    const response = await fetch("/api/resume", {
      method: "POST",
      body: formData,
    });

    if (!response.ok) {
      const data = await response.json();
      throw new Error(data.error ?? "Upload failed");
    }

    await fetchResumeStatus();
    await fetchJobs();
    resumeForm.reset();
  } catch (error) {
    resumeStatus.textContent = `Upload failed: ${error.message}`;
  }
});

timeframeSelect.addEventListener("change", () => {
  fetchJobs();
});

window.addEventListener("DOMContentLoaded", async () => {
  await fetchResumeStatus();
  await fetchJobs();
});
