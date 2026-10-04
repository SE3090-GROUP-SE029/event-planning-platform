import { beforeEach, describe, expect, it, vi } from 'vitest';
import apiClient from '../../../shared/api/apiClient';
import {
  prepareVendorImage,
  uploadVendorGalleryImage,
  uploadVendorProfileImage,
} from './vendorApi';

vi.mock('../../../shared/api/apiClient', () => ({
  default: { post: vi.fn() },
}));

function createImage(name, type, size = 4) {
  const blob = new Blob([new Uint8Array(size)], { type });
  Object.defineProperty(blob, 'name', { value: name });
  return blob;
}

describe('vendor image uploads', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    apiClient.post.mockResolvedValue({ data: { profileImageUrl: '/uploads/vendors/logo.png' } });
  });

  it('uploads profile images as multipart with the backend field and route', async () => {
    const image = createImage('logo.png', 'image/png');

    await uploadVendorProfileImage(image);

    expect(apiClient.post).toHaveBeenCalledWith(
      '/api/vendors/me/profile-image',
      expect.any(FormData),
    );
    const formData = apiClient.post.mock.calls[0][1];
    expect(formData.get('file').name).toBe('logo.png');
    expect(formData.get('file').type).toBe('image/png');
  });

  it('uploads gallery images to the matching API route', async () => {
    const image = createImage('gallery.webp', 'image/webp');

    await uploadVendorGalleryImage(image);

    expect(apiClient.post).toHaveBeenCalledWith(
      '/api/vendors/me/images',
      expect.any(FormData),
    );
  });

  it('fills in a missing browser MIME type from the supported extension', () => {
    const image = createImage('logo.jpeg', '');

    expect(prepareVendorImage(image).type).toBe('image/jpeg');
  });

  it('rejects oversized and unsupported images before sending a request', async () => {
    await expect(
      uploadVendorProfileImage({
        name: 'large.png',
        size: 2 * 1024 * 1024 + 1,
        type: 'image/png',
      }),
    ).rejects.toThrow('Image must be 2 MB or smaller.');
    await expect(
      uploadVendorGalleryImage(createImage('animation.gif', 'image/gif')),
    ).rejects.toThrow('Image must be a JPEG, PNG, or WebP file.');
    expect(apiClient.post).not.toHaveBeenCalled();
  });
});
