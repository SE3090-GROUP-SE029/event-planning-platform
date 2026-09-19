import { useQuery } from '@tanstack/react-query';
import apiClient from '../../../shared/api/apiClient';

export const EVENT_STATUSES = ['DRAFT', 'PLANNING', 'CONFIRMED', 'COMPLETED', 'CANCELLED'];
export const EVENT_TYPES = ['WEDDING', 'CORPORATE', 'BIRTHDAY'];
const EVENT_STATUS_NAMES = EVENT_STATUSES.map((value) => value.replaceAll('_', ' '));

export async function getAdminEvents(params) {
  const response = await apiClient.get('/api/admin/events', { params });
  return response.data;
}

export async function getAdminEvent(id) {
  const response = await apiClient.get(`/api/admin/events/${id}`);
  return response.data;
}

export function useAdminEvents(params) {
  return useQuery({
    queryKey: ['admin-events', params],
    queryFn: () => getAdminEvents(params),
    placeholderData: (previous) => previous,
  });
}

export function useAdminEvent(id) {
  return useQuery({
    queryKey: ['admin-event', id],
    queryFn: () => getAdminEvent(id),
    enabled: Boolean(id),
  });
}

export function enumLabel(value, names = EVENT_STATUS_NAMES) {
  if (typeof value === 'number') {
    return names[value] || 'Unknown';
  }
  return value ? value.replaceAll('_', ' ') : '—';
}
