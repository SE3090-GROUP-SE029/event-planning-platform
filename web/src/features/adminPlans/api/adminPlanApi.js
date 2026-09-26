import { useQuery } from '@tanstack/react-query';
import apiClient from '../../../shared/api/apiClient';

export const PLAN_STATUSES = ['PENDING_PLANNER_REVIEW', 'APPROVED', 'REJECTED'];

export async function getAdminPlans(params) {
  const response = await apiClient.get('/api/admin/plans', { params });
  return response.data;
}

export async function getAdminPlan(id) {
  const response = await apiClient.get(`/api/admin/plans/${id}`);
  return response.data;
}

export function useAdminPlans(params) {
  return useQuery({
    queryKey: ['admin-plans', params],
    queryFn: () => getAdminPlans(params),
    placeholderData: (previous) => previous,
  });
}

export function useAdminPlan(id) {
  return useQuery({
    queryKey: ['admin-plan', id],
    queryFn: () => getAdminPlan(id),
    enabled: Boolean(id),
  });
}

export function planStatusLabel(status) {
  if (typeof status === 'number') {
    return ['DRAFT', 'PENDING', 'APPROVED', 'REJECTED', 'SUPERSEDED'][status] || 'UNKNOWN';
  }
  return String(status || 'UNKNOWN').replaceAll('_', ' ');
}

export function unwrapPlanList(response) {
  return {
    items: response?.items || response?.data || [],
    total: response?.totalCount || response?.pagination?.total || response?.items?.length || 0,
    page: response?.page || response?.pagination?.page || 1,
    totalPages: response?.totalPages || 1,
  };
}
