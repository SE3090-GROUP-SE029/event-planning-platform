export const hasAdminWebAccess = (roles) =>
  Array.isArray(roles) && roles.includes('ADMIN');
