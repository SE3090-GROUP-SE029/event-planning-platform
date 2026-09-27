import { useQuery } from '@tanstack/react-query';
import apiClient from '../../../shared/api/apiClient';

export async function getAdminAnalytics() {
  const response = await apiClient.get('/api/admin/analytics');
  return response.data;
}

export async function getAdminUsers(params) {
  const response = await apiClient.get('/api/admin/users', { params });
  return response.data;
}

export async function getAdminVendors(params) {
  const response = await apiClient.get('/api/admin/vendors', { params });
  return response.data;
}

export async function getAdminHealth() {
  const response = await apiClient.get('/api/admin/health');
  return response.data;
}

export function useAdminAnalytics() {
  return useQuery({
    queryKey: ['admin-analytics'],
    queryFn: getAdminAnalytics,
  });
}

export function useAdminUsers(params) {
  return useQuery({
    queryKey: ['admin-users', params],
    queryFn: () => getAdminUsers(params),
    placeholderData: (previous) => previous,
  });
}

export function useAdminUserVendors(params) {
  return useQuery({
    queryKey: ['admin-vendors', params],
    queryFn: () => getAdminVendors(params),
    placeholderData: (previous) => previous,
  });
}

export function useAdminHealth() {
  return useQuery({
    queryKey: ['admin-health'],
    queryFn: getAdminHealth,
    refetchInterval: 60_000,
  });
}
