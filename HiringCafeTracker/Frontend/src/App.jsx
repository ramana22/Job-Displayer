import React, { useCallback, useEffect, useMemo, useState } from 'react';
import JobTable from './components/JobTable.jsx';
import ResumeUpload from './components/ResumeUpload.jsx';
import CompaniesList from './components/CompaniesList.jsx';
import { useJobsApi } from './hooks/useJobsApi.js';

const timeframeOptions = [
  { label: 'Recent', value: 'recent' },
  { label: 'Last 24 Hours', value: '24h' },
  { label: 'Past 3 Days', value: '3d' },
  { label: 'Past 5 Days', value: '5d' }
];

const statusOptions = [
  { label: 'Not Applied', value: 'Not Applied' },
  { label: 'Applied', value: 'Applied' }
];

export default function App() {
  const { fetchJobs, fetchSources, fetchCompanies, fetchActiveResume, markApplied, uploadResume } = useJobsApi();
  const [jobs, setJobs] = useState([]);
  const [sources, setSources] = useState([]);
  const [companies, setCompanies] = useState([]);
  const [activeResume, setActiveResume] = useState(null);
  const [timeframe, setTimeframe] = useState('recent');
  const [sourceFilter, setSourceFilter] = useState('');
  const [statusFilter, setStatusFilter] = useState('Not Applied');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  const sourceOptions = useMemo(
    () => [{ label: 'All Sources', value: '' }, ...sources.map((source) => ({ label: source, value: source }))],
    [sources]
  );

  const queryParameters = useMemo(() => ({
    timeframe: timeframe === 'recent' ? undefined : timeframe,
    source: sourceFilter || undefined,
    status: statusFilter || undefined
  }), [timeframe, sourceFilter, statusFilter]);

  const hydrate = useCallback(async () => {
    setLoading(true);
    setError('');
    try {
      const [jobsData, sourcesData, companiesData, resumeData] = await Promise.all([
        fetchJobs(queryParameters),
        fetchSources(),
        fetchCompanies(),
        fetchActiveResume()
      ]);
      setJobs(jobsData);
      setSources(sourcesData);
      setCompanies(companiesData);
      setActiveResume(resumeData);
    } catch (err) {
      console.error(err);
      setError('Unable to load dashboard data. Make sure the backend is running.');
    } finally {
      setLoading(false);
    }
  }, [fetchJobs, fetchSources, fetchCompanies, fetchActiveResume, queryParameters]);

  useEffect(() => {
    hydrate();
  }, [hydrate]);

  const handleApply = async (job) => {
    const confirmed = window.confirm(`Mark ${job.jobTitle} at ${job.company} as applied?`);
    if (!confirmed) return;

    try {
      await markApplied(job.id);
      setJobs((current) => current.map((j) => (j.id === job.id ? { ...j, status: 'Applied' } : j)));
    } catch (err) {
      console.error(err);
      window.alert('Unable to update job status.');
    }
  };

  const handleResumeUpload = async (file) => {
    await uploadResume(file);
    const resume = await fetchActiveResume();
    setActiveResume(resume);
    const updatedJobs = await fetchJobs(queryParameters);
    setJobs(updatedJobs);
  };

  return (
    <div className="min-h-screen bg-slate-100 pb-16">
      <header className="bg-white shadow-sm">
        <div className="mx-auto flex max-w-7xl flex-col gap-2 px-6 py-6 sm:flex-row sm:items-center sm:justify-between">
          <div>
            <h1 className="text-2xl font-bold text-slate-900">HiringCafe Job Tracker</h1>
            <p className="text-sm text-slate-500">Ingest HiringCafe job feeds, upload your resume, and track your application pipeline.</p>
          </div>
          <div className="flex items-center gap-2 text-xs text-slate-500">
            <span className="inline-flex items-center gap-1 rounded-full bg-emerald-100 px-3 py-1 font-medium text-emerald-700">
              Matches ≥ 70% are high priority
            </span>
            <span className="inline-flex items-center gap-1 rounded-full bg-amber-100 px-3 py-1 font-medium text-amber-700">
              40-69% consider tailoring
            </span>
          </div>
        </div>
      </header>

      <main className="mx-auto mt-6 flex max-w-7xl flex-col gap-6 px-6">
        <ResumeUpload activeResume={activeResume} onUpload={handleResumeUpload} />

        <section className="rounded-xl border border-slate-200 bg-white p-6 shadow-sm">
          <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
            <div>
              <h2 className="text-lg font-semibold text-slate-900">Job Pipeline</h2>
              <p className="text-sm text-slate-500">{jobs.length} postings loaded</p>
            </div>
            <div className="flex flex-wrap gap-3">
              <div className="flex items-center gap-2">
                {timeframeOptions.map((option) => (
                  <button
                    key={option.value}
                    type="button"
                    onClick={() => setTimeframe(option.value)}
                    className={`rounded-full px-4 py-2 text-xs font-semibold ${timeframe === option.value ? 'bg-primary text-white shadow-sm' : 'bg-slate-100 text-slate-600 hover:bg-slate-200'}`}
                  >
                    {option.label}
                  </button>
                ))}
              </div>
              <select
                className="rounded-lg border border-slate-200 px-3 py-2 text-sm text-slate-700"
                value={sourceFilter}
                onChange={(event) => setSourceFilter(event.target.value)}
              >
                {sourceOptions.map((option) => (
                  <option key={option.value || 'all'} value={option.value}>
                    {option.label}
                  </option>
                ))}
              </select>
              <select
                className="rounded-lg border border-slate-200 px-3 py-2 text-sm text-slate-700"
                value={statusFilter}
                onChange={(event) => setStatusFilter(event.target.value)}
              >
                {statusOptions.map((status) => (
                  <option key={status.value} value={status.value}>
                    {status.label}
                  </option>
                ))}
              </select>
            </div>
          </div>

          {loading ? (
            <div className="mt-6 rounded-lg border border-dashed border-slate-200 p-12 text-center text-slate-500">
              Loading jobs…
            </div>
          ) : error ? (
            <div className="mt-6 rounded-lg border border-red-200 bg-red-50 p-6 text-sm text-red-700">{error}</div>
          ) : (
            <div className="mt-6">
              <JobTable jobs={jobs} onApply={handleApply} />
            </div>
          )}
        </section>

        <CompaniesList companies={companies} />
      </main>
    </div>
  );
}
