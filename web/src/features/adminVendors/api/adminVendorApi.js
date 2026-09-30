import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import apiClient from '../../../shared/api/apiClient';

export const VENDOR_STATUSES = ['PENDING', 'APPROVED', 'SUSPEND'];
export const VENDOR_CATEGORIES = [
  'CATERING',
  'PHOTOGRAPHY',
  'VENUE',
  'MUSIC',
  'FLORIST',
  'TRANSPORTATION',
];

export async function getAdminVendors(params) {
  const response = await apiClient.get('/api/admin/vendors', { params });
  return response.data;
}

export async function getAdminVendor(id) {
  const response = await apiClient.get(`/api/admin/vendors/${id}`);
  return response.data;
}

export async function approveAdminVendor(id) {
  const response = await apiClient.post(`/api/admin/vendors/${id}/approve`);
  return response.data;
}

export async function suspendAdminVendor(id) {
  const response = await apiClient.post(`/api/admin/vendors/${id}/suspend`);
  return response.data;
}

export async function restoreAdminVendor(id) {
  const response = await apiClient.post(`/api/admin/vendors/${id}/restore`);
  return response.data;
}

export function useAdminVendors(params) {
  return useQuery({
    queryKey: ['admin-vendors', params],
    queryFn: () => getAdminVendors(params),
    placeholderData: (previous) => previous,
  });
}

export function useAdminVendor(id) {
  return useQuery({
    queryKey: ['admin-vendor', id],
    queryFn: () => getAdminVendor(id),
    enabled: Boolean(id),
  });
}

function useVendorStatusMutation(mutationFn) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin-vendors'] });
      queryClient.invalidateQueries({ queryKey: ['admin-vendor'] });
    },
  });
}

export function useApproveAdminVendor() {
  return useVendorStatusMutation(({ id }) => approveAdminVendor(id));
}

export function useSuspendAdminVendor() {
  return useVendorStatusMutation(({ id }) => suspendAdminVendor(id));
}

export function useRestoreAdminVendor() {
  return useVendorStatusMutation(({ id }) => restoreAdminVendor(id));
}

export function formatVendorStatus(status) {
  return status ? String(status).replaceAll('_', ' ') : '—';
}
