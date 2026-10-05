import { useQuery } from '@tanstack/react-query';
import apiClient from '../../../shared/api/apiClient';

export async function getAdminSchedules(params) {
  const response = await apiClient.get('/api/admin/schedules', { params });
  return response.data;
}

export async function getAdminSchedule(id) {
  const response = await apiClient.get(`/api/admin/schedules/${id}`);
  return response.data;
}

export function useAdminSchedules(params) {
  return useQuery({
    queryKey: ['admin-schedules', params],
    queryFn: () => getAdminSchedules(params),
    placeholderData: (previous) => previous,
  });
}

export function useAdminSchedule(id) {
  return useQuery({
    queryKey: ['admin-schedule', id],
    queryFn: () => getAdminSchedule(id),
    enabled: Boolean(id),
  });
}

const ISO_DATE_TIME_PARTS =
  /^(\d{4})-(\d{2})-(\d{2})[T\s](\d{2}):(\d{2})(?::\d{2}(?:\.\d+)?)?(?:Z|[+-]\d{2}:?\d{2})?$/;
const ISO_DATE_PARTS = /^(\d{4})-(\d{2})-(\d{2})/;
const TIME_PARTS = /^(\d{1,2}):(\d{2})/;

function to12HourTime(hour, minute) {
  const period = hour >= 12 ? 'PM' : 'AM';
  const displayHour = hour % 12 || 12;
  return `${displayHour}:${String(minute).padStart(2, '0')} ${period}`;
}

function parseWallClockDateTime(value) {
  if (!value) return null;
  const text = String(value);
  const match = text.match(ISO_DATE_TIME_PARTS);
  if (!match) return null;

  return {
    year: match[1],
    month: match[2],
    day: match[3],
    hour: Number(match[4]),
    minute: Number(match[5]),
  };
}

function parseWallClockTime(value) {
  if (!value) return null;
  const dateTime = parseWallClockDateTime(value);
  if (dateTime) return dateTime;

  const match = String(value).match(TIME_PARTS);
  if (!match) return null;

  return {
    hour: Number(match[1]),
    minute: Number(match[2]),
  };
}

export function formatDate(value) {
  if (!value) return '-';
  const match = String(value).match(ISO_DATE_PARTS);
  if (!match) return '-';
  return `${match[2]}/${match[3]}/${match[1]}`;
}

export function formatDateTime(value) {
  return value ? new Date(value).toLocaleString() : '-';
}

export function formatWallClockTime(value) {
  const parts = parseWallClockTime(value);
  if (!parts) return '-';
  return to12HourTime(parts.hour, parts.minute);
}

export function formatTime(value) {
  return formatWallClockTime(value);
}

export function conflictLabel(value) {
  if (value === 'ActivityOverlap') return 'Activity overlap';
  if (value === 'VendorDoubleBooked') return 'Vendor double booked';
  return value ? String(value).replaceAll('_', ' ') : '-';
}
