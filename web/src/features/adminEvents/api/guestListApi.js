import { useMutation, useQueryClient } from "@tanstack/react-query";
import apiClient from "../../../shared/api/apiClient";

/**
 * Uploads a CSV guest list for the given event.
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
