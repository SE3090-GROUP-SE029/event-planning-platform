import { describe, expect, it } from 'vitest';
import { hasAdminWebAccess } from './roleAccess';

describe('web access policy', () => {
  it('allows Admin accounts', () => {
    expect(hasAdminWebAccess(['ADMIN'])).toBe(true);
  });

  it.each(['EVENT_PLANNER', 'VENDOR', undefined])(
    'rejects non-admin role %s',
    (role) => {
      expect(hasAdminWebAccess(role ? [role] : [])).toBe(false);
    },
  );
});
