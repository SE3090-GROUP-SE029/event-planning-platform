import { beforeEach, describe, expect, it, vi } from 'vitest';
import apiClient from '../../../shared/api/apiClient';
import { getAdminEvents } from './adminEventApi';

vi.mock('../../../shared/api/apiClient', () => ({
  default: { get: vi.fn() },
}));

describe('getAdminEvents', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('sends search and date filter parameters to the admin events endpoint', async () => {
    const params = {
      search: 'Autumn Celebration',
      dateFrom: '2026-10-01',
      dateTo: '2026-10-31',
      page: 1,
      pageSize: 10,
    };
    apiClient.get.mockResolvedValue({ data: { items: [], totalCount: 0 } });

    await getAdminEvents(params);

    expect(apiClient.get).toHaveBeenCalledWith('/api/admin/events', { params });
  });
});
