import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import apiClient from '../../../shared/api/apiClient';

const API_BASE = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5207';

export const VENDOR_CATEGORIES = [
  'CATERING',
  'PHOTOGRAPHY',
  'VENUE',
  'MUSIC',
  'FLORIST',
  'TRANSPORTATION',
];

const VENDOR_IMAGE_TYPES = {
  jpg: 'image/jpeg',
  jpeg: 'image/jpeg',
  png: 'image/png',
  webp: 'image/webp',
};

export function prepareVendorImage(file) {
  if (!file || file.size <= 0) {
    throw new Error('Select a non-empty image file.');
  }
  if (file.size > 2 * 1024 * 1024) {
    throw new Error('Image must be 2 MB or smaller.');
  }

  const extension = file.name?.split('.').pop()?.toLowerCase();
  const expectedType = VENDOR_IMAGE_TYPES[extension];
  const rawType = file.type?.toLowerCase();
  const declaredType = rawType === 'image/jpg' ? 'image/jpeg' : rawType;
  if (!expectedType || (declaredType && declaredType !== expectedType)) {
    throw new Error('Image must be a JPEG, PNG, or WebP file.');
  }

  return rawType === expectedType ? file : file.slice(0, file.size, expectedType);
}

export async function uploadVendorProfileImage(file) {
  const upload = prepareVendorImage(file);
  const formData = new FormData();
  formData.append('file', upload, file.name);
  const response = await apiClient.post('/api/vendors/me/profile-image', formData);
  return response.data;
}

export async function uploadVendorGalleryImage(file) {
  const upload = prepareVendorImage(file);
  const formData = new FormData();
  formData.append('file', upload, file.name);
  const response = await apiClient.post('/api/vendors/me/images', formData);
  return response.data;
}

export function resolveVendorImageUrl(profileImageUrl) {
  if (!profileImageUrl) return null;
  if (/^https?:\/\//i.test(profileImageUrl)) return profileImageUrl;
  return `${API_BASE}${profileImageUrl.startsWith('/') ? '' : '/'}${profileImageUrl}`;
}

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

export function useUploadVendorProfileImage() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: uploadVendorProfileImage,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['vendor-profile', 'me'] });
    },
  });
}

export function useMyVendorGallery(enabled = true) {
  return useQuery({
    queryKey: ['vendor-gallery', 'me'],
    queryFn: async () => {
      const response = await apiClient.get('/api/vendors/me/images');
      return response.data;
    },
    enabled,
  });
}

export function useUploadVendorGalleryImage() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: uploadVendorGalleryImage,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['vendor-gallery', 'me'] });
    },
  });
}

export function useDeleteVendorGalleryImage() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (id) => {
      await apiClient.delete(`/api/vendors/me/images/${id}`);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['vendor-gallery', 'me'] });
    },
  });
}

export async function listMyVendorServices() {
  const response = await apiClient.get('/api/vendors/me/services');
  return response.data;
}

export function useMyVendorServices(enabled = true) {
  return useQuery({
    queryKey: ['vendor-services', 'me'],
    queryFn: listMyVendorServices,
    enabled,
  });
}

export function useCreateVendorService() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (payload) => {
      const response = await apiClient.post('/api/vendors/me/services', payload);
      return response.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['vendor-services', 'me'] });
    },
  });
}

export function useUpdateVendorService() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({ id, ...payload }) => {
      const response = await apiClient.put(`/api/vendors/me/services/${id}`, payload);
      return response.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['vendor-services', 'me'] });
    },
  });
}

export function useDeleteVendorService() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (id) => {
      await apiClient.delete(`/api/vendors/me/services/${id}`);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['vendor-services', 'me'] });
    },
  });
}

export async function listMyVendorAvailability() {
  const response = await apiClient.get('/api/vendors/me/availability');
  return response.data;
}

export function useMyVendorAvailability(enabled = true) {
  return useQuery({
    queryKey: ['vendor-availability', 'me'],
    queryFn: listMyVendorAvailability,
    enabled,
  });
}

export function useCreateVendorAvailability() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (payload) => {
      const response = await apiClient.post('/api/vendors/me/availability', payload);
      return response.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['vendor-availability', 'me'] });
    },
  });
}

export function useUpdateVendorAvailability() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({ id, ...payload }) => {
      const response = await apiClient.put(`/api/vendors/me/availability/${id}`, payload);
      return response.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['vendor-availability', 'me'] });
    },
  });
}

export function useDeleteVendorAvailability() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (id) => {
      await apiClient.delete(`/api/vendors/me/availability/${id}`);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['vendor-availability', 'me'] });
    },
  });
}

export async function listVendorMarketplace(params = {}) {
  const response = await apiClient.get('/api/vendors/marketplace', {
    params: {
      search: params.search || undefined,
      category: params.category || undefined,
      sortBy: params.sortBy || 'businessName',
      sortOrder: params.sortOrder || 'asc',
      page: params.page || 1,
      pageSize: params.pageSize || 10,
    },
  });
  return response.data;
}

export function useVendorMarketplace(params) {
  return useQuery({
    queryKey: ['vendor-marketplace', params],
    queryFn: () => listVendorMarketplace(params),
  });
}

export function useVendorMarketplaceDetail(vendorId) {
  return useQuery({
    queryKey: ['vendor-marketplace', vendorId],
    queryFn: async () => {
      const response = await apiClient.get(`/api/vendors/marketplace/${vendorId}`);
      return response.data;
    },
    enabled: Boolean(vendorId),
  });
}
