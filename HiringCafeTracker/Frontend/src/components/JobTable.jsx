import React from 'react';
import dayjs from 'dayjs';
import clsx from 'clsx';

const statusColors = {
  'Not Applied': 'bg-slate-200 text-slate-700',
  Applied: 'bg-emerald-100 text-emerald-700'
};

export default function JobTable({ jobs, onApply }) {
  if (!jobs.length) {
    return (
      <div className="rounded-lg border border-dashed border-slate-300 bg-white p-12 text-center text-slate-500">
        No jobs match your filters yet.
      </div>
    );
  }

  return (
    <div className="overflow-x-auto rounded-lg border border-slate-200 bg-white shadow-sm">
      <table className="min-w-full divide-y divide-slate-200 text-sm">
        <thead className="bg-slate-100 text-left uppercase tracking-wide text-slate-600">
          <tr>
            <th className="px-4 py-3">Job Id</th>
            <th className="px-4 py-3">Job Title</th>
            <th className="px-4 py-3">Company</th>
            <th className="px-4 py-3">Location</th>
            <th className="px-4 py-3">Salary</th>
            <th className="px-4 py-3">Search Key</th>
            <th className="px-4 py-3">Posted</th>
            <th className="px-4 py-3">Source</th>
            <th className="px-4 py-3">Matching Score</th>
            <th className="px-4 py-3">Status</th>
            <th className="px-4 py-3">Actions</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-slate-100">
          {jobs.map((job) => (
            <tr key={job.id} className="hover:bg-slate-50">
              <td className="px-4 py-3 font-mono text-xs text-slate-500">{job.jobId}</td>
              <td className="px-4 py-3">
                <div className="font-semibold text-slate-900">{job.jobTitle}</div>
                <p className="mt-1 line-clamp-3 text-xs text-slate-600">{job.description}</p>
              </td>
              <td className="px-4 py-3 text-slate-700">{job.company}</td>
              <td className="px-4 py-3 text-slate-600">{job.location}</td>
              <td className="px-4 py-3 text-slate-600">{job.salary}</td>
              <td className="px-4 py-3 text-slate-600">{job.searchKey}</td>
              <td className="px-4 py-3 text-slate-600">{job.postedTime ? dayjs(job.postedTime).format('MMM D, YYYY') : 'N/A'}</td>
              <td className="px-4 py-3 text-slate-600">{job.source}</td>
              <td className="px-4 py-3">
                <span className={clsx('inline-flex items-center rounded-full px-3 py-1 text-xs font-semibold', {
                  'bg-emerald-100 text-emerald-700': job.matchingScore >= 70,
                  'bg-amber-100 text-amber-700': job.matchingScore >= 40 && job.matchingScore < 70,
                  'bg-slate-200 text-slate-700': job.matchingScore < 40
                })}>
                  {job.matchingScore?.toFixed(2)}%
                </span>
              </td>
              <td className="px-4 py-3">
                <span className={clsx('inline-flex items-center rounded-full px-3 py-1 text-xs font-semibold', statusColors[job.status] ?? 'bg-slate-200 text-slate-700')}>
                  {job.status}
                </span>
              </td>
              <td className="px-4 py-3">
                <div className="flex flex-col gap-2">
                  <a
                    href={job.applyLink}
                    target="_blank"
                    rel="noreferrer"
                    className="rounded-md bg-primary px-3 py-1 text-center text-xs font-semibold text-white shadow-sm hover:bg-blue-600"
                  >
                    View Posting
                  </a>
                  {job.status !== 'Applied' && (
                    <button
                      type="button"
                      className="rounded-md border border-primary px-3 py-1 text-xs font-semibold text-primary hover:bg-blue-50"
                      onClick={() => onApply(job)}
                    >
                      Mark Applied
                    </button>
                  )}
                </div>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
