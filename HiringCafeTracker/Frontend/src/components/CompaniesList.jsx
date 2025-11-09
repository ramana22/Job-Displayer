import React from 'react';

export default function CompaniesList({ companies }) {
  if (!companies.length) {
    return null;
  }

  return (
    <div className="rounded-xl border border-slate-200 bg-white p-6 shadow-sm">
      <h3 className="text-base font-semibold text-slate-900">Company Career Pages</h3>
      <p className="mt-1 text-sm text-slate-500">Quick links to every company represented in your pipeline.</p>
      <ul className="mt-4 grid gap-3 md:grid-cols-2">
        {companies.map((company) => (
          <li key={company.companyName} className="rounded-lg border border-slate-100 bg-slate-50 p-4">
            <h4 className="text-sm font-semibold text-slate-800">{company.companyName}</h4>
            <a
              href={company.careerSiteUrl}
              target="_blank"
              rel="noreferrer"
              className="mt-2 inline-flex items-center text-xs font-semibold text-primary hover:underline"
            >
              Visit career site →
            </a>
          </li>
        ))}
      </ul>
    </div>
  );
}
