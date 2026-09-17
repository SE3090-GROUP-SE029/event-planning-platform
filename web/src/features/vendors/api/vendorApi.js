import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import apiClient from '../../../shared/api/apiClient';

export const VENDOR_CATEGORIES = [
  'CATERING',
  'PHOTOGRAPHY',
  'VENUE',
  'MUSIC',
  'FLORIST',
  'TRANSPORTATION',
];

export async function getMyVendorProfile() {
  try {
    const response = await apiClient.get('/api/vendors/me');
    return response.data;
  } catch (error) {
    if (error.message?.toLowerCase().includes('not found')) {
      return null;
    }
    throw error;
  }
}

export function useMyVendorProfile() {
  return useQuery({
    queryKey: ['vendor-profile', 'me'],
    queryFn: getMyVendorProfile,
  });
}

export function useCreateVendorProfile() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (payload) => {
      const response = await apiClient.post('/api/vendors', payload);
      return response.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['vendor-profile', 'me'] });
    },
  });
}

export function useUpdateVendorProfile() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (payload) => {
      const response = await apiClient.put('/api/vendors/me', payload);
      return response.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['vendor-profile', 'me'] });
    },
  });
}
