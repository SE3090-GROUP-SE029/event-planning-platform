import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import apiClient from '../../../shared/api/apiClient';

/**
 * @typedef {{ id: string, userId: string, businessName: string, category: string, contactEmail: string, contactPhone: string, address: string, description: (string|null), profileImageUrl: (string|null), websiteUrl: (string|null), status: string, createdAt: string, updatedAt: (string|null) }} AdminVendor
 * @typedef {{ items: AdminVendor[], page: number, pageSize: number, totalCount: number, totalPages: number }} AdminVendorList
 * @typedef {{ page?: number, pageSize?: number, search?: string, status?: string, category?: string }} AdminVendorQuery
 */

export async function getAdminAnalytics() {
  const response = await apiClient.get('/api/admin/analytics');
  return response.data;
}

export async function getAdminUsers(params) {
  const response = await apiClient.get('/api/admin/users', { params });
  return response.data;
}

/** @param {AdminVendorQuery} params
 *  @returns {Promise<AdminVendorList>}
 */
export async function getAdminVendors(params) {
  const response = await apiClient.get('/api/admin/vendors', { params });
  return response.data;
}

/**
 * @param {string} vendorId
 * @returns {Promise<void>}
 * Approves a pending vendor. The endpoint returns no response body.
 */
export async function approveAdminVendor(vendorId) {
  await apiClient.post(`/api/admin/vendors/${encodeURIComponent(vendorId)}/approve`);
}

export function getVendorApprovalErrorMessage(error) {
  return error?.message || 'Vendor approval failed. Please try again.';
}

/**
 * @param {import('@tanstack/react-query').QueryClient} queryClient
 */
export function createApproveAdminVendorMutationOptions(queryClient) {
  return {
    mutationFn: approveAdminVendor,
    onSuccess: async (_, vendorId) => {
      queryClient.setQueriesData({ queryKey: ['admin-vendors'] }, (data) => {
        if (!data || !Array.isArray(data.items)) return data;
        return {
          ...data,
          items: data.items.map((vendor) =>
            vendor.id === vendorId ? { ...vendor, status: 'APPROVED' } : vendor
          ),
        };
      });
      await queryClient.invalidateQueries({ queryKey: ['admin-vendors'] });
    },
  };
}

export function useApproveAdminVendor() {
  const queryClient = useQueryClient();
  return useMutation(createApproveAdminVendorMutationOptions(queryClient));
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
