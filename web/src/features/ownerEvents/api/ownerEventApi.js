import { useQuery } from '@tanstack/react-query';
import apiClient from '../../../shared/api/apiClient';

export async function getMyEvents() {
  const response = await apiClient.get('/api/events');
  return response.data;
}

export async function getEventPlans(eventId) {
  const response = await apiClient.get(`/api/events/${eventId}/plans`);
  return response.data;
}

export async function getPlan(id) {
  const response = await apiClient.get(`/api/plans/${id}`);
  return response.data?.data || response.data;
}

export async function generatePlan(eventId, regenerationReason) {
  const data = regenerationReason
    ? { eventId, regenerate: true, regenerationReason }
    : undefined;
  const response = await apiClient.post(`/api/events/${eventId}/plans/generate`, data);
  return response.data?.data || response.data;
}

export async function approvePlan(id, approverNotes) {
  const response = await apiClient.post(`/api/plans/${id}/approve`, {
    planId: id,
    approverNotes,
  });
  return response.data?.data || response.data;
}

export async function rejectPlan(id, remarks, severity) {
  const response = await apiClient.post(`/api/plans/${id}/reject`, {
    planId: id,
    remarks,
    severity,
  });
  return response.data?.data || response.data;
}

export function useMyEvents(enabled = true) {
  return useQuery({
    queryKey: ['my-events'],
    queryFn: getMyEvents,
    enabled,
  });
}

export function useEventPlans(eventId) {
  return useQuery({
    queryKey: ['event-plans', eventId],
    queryFn: () => getEventPlans(eventId),
    enabled: Boolean(eventId),
  });
}

export function usePlan(id) {
  return useQuery({
    queryKey: ['owner-plan', id],
    queryFn: () => getPlan(id),
    enabled: Boolean(id),
  });
}

export function unwrapItems(response) {
  return response?.items || response?.data || [];
}

export function statusLabel(status) {
  const statuses = ['DRAFT', 'PENDING PLANNER REVIEW', 'APPROVED', 'REJECTED', 'SUPERSEDED'];
  return typeof status === 'number'
    ? statuses[status] || 'UNKNOWN'
    : String(status || 'UNKNOWN').replaceAll('_', ' ');
}
