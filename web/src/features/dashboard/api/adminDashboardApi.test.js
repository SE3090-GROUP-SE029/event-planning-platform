import { QueryClient } from '@tanstack/react-query';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import apiClient from '../../../shared/api/apiClient';
import {
  approveAdminVendor,
  createApproveAdminVendorMutationOptions,
  getVendorApprovalErrorMessage,
  getAdminVendors,
} from './adminDashboardApi';

vi.mock('../../../shared/api/apiClient', () => ({
  default: {
    get: vi.fn(),
    post: vi.fn(),
  },
}));

describe('admin vendor API', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('loads admin vendors through the shared API client', async () => {
    const response = { items: [{ id: 'vendor-1', status: 'PENDING' }] };
    apiClient.get.mockResolvedValue({ data: response });

    await expect(getAdminVendors({ page: 1, pageSize: 10 })).resolves.toEqual(response);
    expect(apiClient.get).toHaveBeenCalledWith('/api/admin/vendors', {
      params: { page: 1, pageSize: 10 },
    });
  });

  it('posts approval through the shared API client', async () => {
    apiClient.post.mockResolvedValue({ status: 204 });

    await approveAdminVendor('vendor-1');

    expect(apiClient.post).toHaveBeenCalledWith('/api/admin/vendors/vendor-1/approve');
  });

  it('preserves backend errors for meaningful approval feedback', async () => {
    const error = new Error('Vendor is not pending.');
    apiClient.post.mockRejectedValue(error);

    await expect(approveAdminVendor('vendor-1')).rejects.toBe(error);
  });

  it.each([
    ['Vendor not found.', 'Vendor not found.'],
    ['Only pending vendors can be approved.', 'Only pending vendors can be approved.'],
    ['Session expired. Please log in again.', 'Session expired. Please log in again.'],
    ['Network error — check your connection.', 'Network error — check your connection.'],
  ])('surfaces the API error message: %s', (message, expected) => {
    expect(getVendorApprovalErrorMessage(new Error(message))).toBe(expected);
  });

  it('provides a fallback when an approval failure has no message', () => {
    expect(getVendorApprovalErrorMessage(null)).toBe(
      'Vendor approval failed. Please try again.'
    );
  });

  it('updates cached status and refreshes admin vendor queries after approval', async () => {
    const queryClient = new QueryClient();
    const queryKey = ['admin-vendors', { page: 1 }];
    queryClient.setQueryData(queryKey, {
      items: [
        { id: 'vendor-1', status: 'PENDING' },
        { id: 'vendor-2', status: 'PENDING' },
      ],
      totalCount: 2,
    });
    const invalidateQueries = vi.spyOn(queryClient, 'invalidateQueries').mockResolvedValue([]);
    const options = createApproveAdminVendorMutationOptions(queryClient);

    await options.onSuccess(undefined, 'vendor-1');

    expect(queryClient.getQueryData(queryKey).items).toEqual([
      { id: 'vendor-1', status: 'APPROVED' },
      { id: 'vendor-2', status: 'PENDING' },
    ]);
    expect(invalidateQueries).toHaveBeenCalledWith({ queryKey: ['admin-vendors'] });
  });
});
