import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import apiClient from '../../../shared/api/apiClient';

export function formatQuotationStatus(status) {
  if (status === 'REQUESTED') return 'Pending';
  if (status === 'QUOTED') return 'Responded';
  return status || 'Unknown';
}

export async function listMyEventsForQuotation() {
  const response = await apiClient.get('/api/events', {
    params: { page: 1, pageSize: 100 },
  });
  return response.data?.items || [];
}

export function useMyEventsForQuotation(enabled = true) {
  return useQuery({
    queryKey: ['events', 'quotation-picker'],
    queryFn: listMyEventsForQuotation,
    enabled,
  });
}

export async function createQuotation(payload) {
  const response = await apiClient.post('/api/quotations', payload);
  return response.data;
}

export function useCreateQuotation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: createQuotation,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['quotations', 'mine'] });
    },
  });
}

export function useMyQuotations() {
  return useQuery({
    queryKey: ['quotations', 'mine'],
    queryFn: async () => {
      const response = await apiClient.get('/api/quotations/mine');
      return response.data || [];
    },
  });
}

export function useVendorQuotations() {
  return useQuery({
    queryKey: ['quotations', 'vendor'],
    queryFn: async () => {
      const response = await apiClient.get('/api/quotations/vendor');
      return response.data || [];
    },
  });
}

export function useQuotation(id, enabled = true) {
  return useQuery({
    queryKey: ['quotations', id],
    queryFn: async () => {
      const response = await apiClient.get(`/api/quotations/${id}`);
      return response.data;
    },
    enabled: Boolean(id) && enabled,
  });
}

export function useRespondToQuotation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({ id, payload }) => {
      const response = await apiClient.put(`/api/quotations/${id}/respond`, payload);
      return response.data;
    },
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: ['quotations', 'vendor'] });
      queryClient.invalidateQueries({ queryKey: ['quotations', variables.id] });
    },
  });
}

export function toApiDateTime(localValue) {
  if (!localValue) return null;
  return new Date(localValue).toISOString();
}

export function formatDateTime(iso) {
  if (!iso) return '—';
  return new Date(iso).toLocaleString(undefined, {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
    hour: 'numeric',
    minute: '2-digit',
  });
}

export function formatQuotedPrice(price) {
  if (price == null) return '—';
  return `Rs. ${Number(price).toLocaleString('en-LK', {
    minimumFractionDigits: 0,
    maximumFractionDigits: 2,
  })}`;
}
