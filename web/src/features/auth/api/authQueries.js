import { useMutation, useQuery } from '@tanstack/react-query';
import { useAuthStore } from '../../../shared/store/authStore';
import apiClient from '../../../shared/api/apiClient';
import { hasAdminWebAccess } from '../../../shared/auth/roleAccess';

export const useLogin = () => {
  const { setAuth, setError } = useAuthStore();

  return useMutation({
    mutationFn: async (credentials) => {
      const response = await apiClient.post('/api/auth/login', credentials);
      if (!hasAdminWebAccess(response.data?.roles)) {
        throw new Error('Only Admin accounts may sign in to the website.');
      }
      return response.data;
    },
    onSuccess: (data) => {
      setAuth(data);
      setError(null);
    },
    onError: (error) => {
      useAuthStore.getState().logout();
      setError(error.message);
    },
  });
};

export const useRegister = () => {
  const { setAuth, setError } = useAuthStore();

  return useMutation({
    mutationFn: async (userData) => {
      const response = await apiClient.post('/api/auth/register', userData);
      return response.data;
    },
    onSuccess: (data) => {
      setAuth(data);
      setError(null);
    },
    onError: (error) => {
      setError(error.message);
    },
  });
};

export const useCurrentUser = (token) => {
  return useQuery({
    queryKey: ['currentUser', token],
    queryFn: async () => {
      const response = await apiClient.get('/api/auth/me');
      return response.data;
    },
    enabled: !!token,
  });
};