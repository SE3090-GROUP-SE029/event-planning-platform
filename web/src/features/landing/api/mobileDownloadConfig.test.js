import { describe, expect, it, vi } from 'vitest';
import {
  fetchMobileAppDownloadUrl,
  validateMobileAppDownloadUrl,
} from './mobileDownloadConfig';

describe('mobile app download configuration', () => {
  it('accepts a secure configured download URL', () => {
    expect(validateMobileAppDownloadUrl('https://example.com/app')).toBe(
      'https://example.com/app',
    );
  });

  it('treats an absent URL as unavailable', () => {
    expect(validateMobileAppDownloadUrl('')).toBeNull();
    expect(validateMobileAppDownloadUrl(null)).toBeNull();
  });

  it.each(['not a URL', 'http://example.com/app', 'https://user:pass@example.com/app'])(
    'rejects unsafe or malformed URL %s',
    (url) => {
      expect(() => validateMobileAppDownloadUrl(url)).toThrow();
    },
  );

  it('loads download settings without a browser cache', async () => {
    const fetcher = vi.fn().mockResolvedValue({
      ok: true,
      json: async () => ({
        mobileAppDownloadUrl: 'https://example.com/mobile-app',
      }),
    });

    await expect(fetchMobileAppDownloadUrl(fetcher)).resolves.toBe(
      'https://example.com/mobile-app',
    );
    expect(fetcher).toHaveBeenCalledWith(
      '/app-config.json',
      expect.objectContaining({ cache: 'no-store' }),
    );
  });

  it('reports a failed configuration request', async () => {
    const fetcher = vi.fn().mockResolvedValue({ ok: false, status: 503 });

    await expect(fetchMobileAppDownloadUrl(fetcher)).rejects.toThrow(
      'configuration could not be loaded (503)',
    );
  });
});
