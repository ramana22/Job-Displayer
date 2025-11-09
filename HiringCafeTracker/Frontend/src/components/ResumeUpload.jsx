import React, { useRef, useState } from 'react';

export default function ResumeUpload({ activeResume, onUpload }) {
  const inputRef = useRef(null);
  const [isUploading, setIsUploading] = useState(false);
  const [error, setError] = useState('');

  const handleFileSelect = async (event) => {
    const file = event.target.files?.[0];
    if (!file) return;

    const extension = file.name.split('.').pop()?.toLowerCase();
    if (!['pdf', 'docx'].includes(extension)) {
      setError('Only PDF and DOCX resumes are supported.');
      return;
    }

    setError('');
    setIsUploading(true);

    try {
      await onUpload(file);
    } catch (err) {
      console.error(err);
      setError('Unable to upload resume. Please try again.');
    } finally {
      setIsUploading(false);
      if (inputRef.current) {
        inputRef.current.value = '';
      }
    }
  };

  return (
    <div className="rounded-xl border border-slate-200 bg-white p-6 shadow-sm">
      <div className="flex items-center justify-between">
        <div>
          <h3 className="text-base font-semibold text-slate-900">Resume</h3>
          <p className="mt-1 text-sm text-slate-500">
            Upload your latest resume to power the ATS-style matching score.
          </p>
          {activeResume ? (
            <p className="mt-2 text-xs text-slate-500">
              Active resume: <span className="font-medium text-slate-700">{activeResume.fileName}</span> (uploaded {new Date(activeResume.uploadedAt).toLocaleString()})
            </p>
          ) : (
            <p className="mt-2 text-xs text-amber-600">No resume uploaded yet.</p>
          )}
        </div>
        <div className="flex flex-col items-end gap-2">
          <button
            type="button"
            onClick={() => inputRef.current?.click()}
            disabled={isUploading}
            className="rounded-lg bg-primary px-4 py-2 text-sm font-semibold text-white shadow-sm hover:bg-blue-600 disabled:cursor-not-allowed disabled:opacity-60"
          >
            {isUploading ? 'Uploading…' : 'Upload Resume'}
          </button>
          <input
            ref={inputRef}
            type="file"
            accept=".pdf,.docx"
            className="hidden"
            onChange={handleFileSelect}
          />
          {error && <p className="text-xs text-red-500">{error}</p>}
        </div>
      </div>
    </div>
  );
}
