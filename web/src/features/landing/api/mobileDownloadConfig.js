export function validateMobileAppDownloadUrl(value) {
  if (typeof value !== 'string' || value.trim() === '') {
    return null;
  }

  let url;
  try {
    url = new URL(value.trim());
  } catch {
    throw new Error('The configured mobile app download URL is invalid.');
  }

  if (url.protocol !== 'https:' || url.username || url.password) {
    throw new Error('The configured mobile app download URL must be a secure HTTPS URL.');
  }

  return url.toString();
}

export async function fetchMobileAppDownloadUrl(fetcher = fetch) {
  const response = await fetcher(`${import.meta.env.BASE_URL}app-config.json`, {
    cache: 'no-store',
    headers: { Accept: 'application/json' },
  });

  if (!response.ok) {
    throw new Error(`Mobile app configuration could not be loaded (${response.status}).`);
  }

  const config = await response.json();
  if (!config || typeof config !== 'object' || Array.isArray(config)) {
    throw new Error('Mobile app configuration has an invalid format.');
  }

  return validateMobileAppDownloadUrl(config.mobileAppDownloadUrl);
}
