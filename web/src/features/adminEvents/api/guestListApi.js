import { useMutation, useQueryClient } from "@tanstack/react-query";
import apiClient from "../../../shared/api/apiClient";

/**
 * Uploads a CSV, PDF, or DOCX guest list for the given event.
 * Sends a multipart/form-data POST to:
 *   POST /api/events/{eventId}/registration-form/upload
 *
 * Returns a BulkGuestUploadResult object.
 */
export async function uploadGuestList(eventId, file) {
  const formData = new FormData();
  formData.append("file", file);
  const response = await apiClient.post(
    `/api/events/${eventId}/registration-form/upload`,
    formData
  );
  return response.data;
}

/**
 * React Query mutation hook for guest list CSV upload.
 * Invalidates the guest list query on success so the planner
 * immediately sees the newly created registrations.
 */
export function useUploadGuestList(eventId) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (file) => uploadGuestList(eventId, file),
    onSuccess: () => {
      // Invalidate the planner registration list so new guests appear.
      queryClient.invalidateQueries({ queryKey: ["planner-registrations", eventId] });
    },
  });
}

export async function fetchEventGuests({ eventId, page, pageSize, status, rsvpStatus, isWaitlisted, checkedIn }) {
  const params = { page, pageSize };
  if (status && status !== 'ALL') params.status = status;
  if (rsvpStatus && rsvpStatus !== 'ALL') params.rsvpStatus = rsvpStatus;
  if (isWaitlisted !== undefined && isWaitlisted !== null) params.isWaitlisted = isWaitlisted;
  if (checkedIn !== undefined && checkedIn !== null) params.checkedIn = checkedIn;

  const response = await apiClient.get(`/api/events/${eventId}/registration-form/registrations`, { params });
  return response.data;
}

import { useQuery } from '@tanstack/react-query';

export function useEventGuests(params) {
  return useQuery({
    queryKey: ['planner-registrations', params.eventId, params],
    queryFn: () => fetchEventGuests(params),
    enabled: !!params.eventId,
  });
}
