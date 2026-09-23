import axios from 'axios';
import { useAuthStore } from '../store/authStore';

const baseURL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5207';

const apiClient = axios.create({
  baseURL,
  headers: {
    'Content-Type': 'application/json',
  },
});

let refreshPromise = null;

async function refreshAccessToken() {
  const { refreshToken, setAuth, logout } = useAuthStore.getState();
  if (!refreshToken) {
    logout();
    throw new Error('Session expired. Please log in again.');
  }

  if (!refreshPromise) {
    refreshPromise = axios
      .post(`${baseURL}/api/auth/refresh`, { refreshToken })
      .then((response) => {
        setAuth(response.data);
        return response.data.accessToken;
      })
      .catch((error) => {
        logout();
        throw error;
      })
      .finally(() => {
        refreshPromise = null;
      });
  }

  return refreshPromise;
}

apiClient.interceptors.request.use((config) => {
  const token = useAuthStore.getState().accessToken;
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }

  // Let the browser set multipart boundary for FormData uploads.
  if (typeof FormData !== 'undefined' && config.data instanceof FormData) {
    if (typeof config.headers?.set === 'function') {
      config.headers.set('Content-Type', false);
    } else if (config.headers) {
      delete config.headers['Content-Type'];
      delete config.headers['content-type'];
    }
    config.timeout = 60000;
  }

  return config;
});

apiClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config;
    const status = error.response?.status;

    if (status === 401 && originalRequest && !originalRequest._retry) {
      const url = originalRequest.url || '';
      const isAuthRoute =
        url.includes('/api/auth/login') ||
        url.includes('/api/auth/register') ||
        url.includes('/api/auth/refresh');

      if (!isAuthRoute && useAuthStore.getState().refreshToken) {
        originalRequest._retry = true;
        try {
          const accessToken = await refreshAccessToken();
          originalRequest.headers = originalRequest.headers || {};
          originalRequest.headers.Authorization = `Bearer ${accessToken}`;
          return apiClient(originalRequest);
        } catch {
          // fall through to reject below
        }
      }
    }

    const message =
      error.response?.data?.message ||
      error.response?.data?.error ||
      (status === 401
        ? 'Session expired. Please log in again.'
        : !error.response
          ? 'Network error — check the API is running on http://localhost:5207 and try a JPEG/PNG under 2 MB.'
          : error.message) ||
      'Request failed';
    return Promise.reject(new Error(message));
  }
);

export default apiClient;
