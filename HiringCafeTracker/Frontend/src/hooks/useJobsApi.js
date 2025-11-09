import { useCallback } from 'react';
import axios from 'axios';

const client = axios.create({
  baseURL: '/api'
});

export function useJobsApi() {
  const fetchJobs = useCallback(async (params = {}) => {
    const response = await client.get('/jobs', { params });
    return response.data;
  }, []);

  const fetchSources = useCallback(async () => {
    const response = await client.get('/jobs/sources');
    return response.data;
  }, []);

  const fetchCompanies = useCallback(async () => {
    const response = await client.get('/companies');
    return response.data;
  }, []);

  const fetchActiveResume = useCallback(async () => {
    const response = await client.get('/resumes/active');
    return response.data;
  }, []);

  const markApplied = useCallback(async (id) => {
    await client.post(`/jobs/${id}/apply`);
  }, []);

  const uploadResume = useCallback(async (file) => {
    const formData = new FormData();
    formData.append('file', file);
    return client.post('/resumes', formData, {
      headers: { 'Content-Type': 'multipart/form-data' }
    });
  }, []);

  return {
    fetchJobs,
    fetchSources,
    fetchCompanies,
    fetchActiveResume,
    markApplied,
    uploadResume
  };
}
