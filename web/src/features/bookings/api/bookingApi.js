import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import apiClient from '../../../shared/api/apiClient';
import { formatDateTime, formatQuotedPrice } from '../../quotations/api/quotationApi';

export { formatDateTime, formatQuotedPrice };

export function formatBookingStatus(status) {
  return status || 'Unknown';
}

export function useMyBookings() {
  return useQuery({
    queryKey: ['bookings', 'mine'],
    queryFn: async () => {
      const response = await apiClient.get('/api/bookings/mine');
      return response.data || [];
    },
  });
}

export function useVendorBookings() {
  return useQuery({
    queryKey: ['bookings', 'vendor'],
    queryFn: async () => {
      const response = await apiClient.get('/api/bookings/vendor');
      return response.data || [];
    },
  });
}

export function useBooking(id, enabled = true) {
  return useQuery({
    queryKey: ['bookings', id],
    queryFn: async () => {
      const response = await apiClient.get(`/api/bookings/${id}`);
      return response.data;
    },
    enabled: Boolean(id) && enabled,
  });
}

export function useAcceptQuotation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (quotationId) => {
      const response = await apiClient.post(`/api/quotations/${quotationId}/accept`);
      return response.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['quotations', 'mine'] });
      queryClient.invalidateQueries({ queryKey: ['bookings', 'mine'] });
      queryClient.invalidateQueries({ queryKey: ['bookings', 'vendor'] });
    },
  });
}

export function useCompleteBooking() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (id) => {
      const response = await apiClient.put(`/api/bookings/${id}/complete`);
      return response.data;
    },
    onSuccess: (_data, id) => {
      queryClient.invalidateQueries({ queryKey: ['bookings', 'vendor'] });
      queryClient.invalidateQueries({ queryKey: ['bookings', 'mine'] });
      queryClient.invalidateQueries({ queryKey: ['bookings', id] });
    },
  });
}

export function useCancelBooking() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({ id, cancellationReason }) => {
      const response = await apiClient.put(`/api/bookings/${id}/cancel`, {
        cancellationReason,
      });
      return response.data;
    },
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: ['bookings', 'vendor'] });
      queryClient.invalidateQueries({ queryKey: ['bookings', 'mine'] });
      queryClient.invalidateQueries({ queryKey: ['bookings', variables.id] });
    },
  });
}
