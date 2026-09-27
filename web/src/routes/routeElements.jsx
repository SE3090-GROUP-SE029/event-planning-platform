import { lazy, Suspense } from 'react';
import { Box, CircularProgress } from '@mui/material';
import ProtectedRoute from '../shared/components/ProtectedRoute';

const LandingPage = lazy(() => import('../features/landing/pages/LandingPage'));
const LoginPage = lazy(() => import('../features/auth/pages/LoginPage'));
const DashboardPage = lazy(() => import('../features/dashboard/pages/DashboardPage'));
const AdminEventManagementPage = lazy(() => import('../features/adminEvents/pages/AdminEventManagementPage'));
const AdminEventDetailsPage = lazy(() => import('../features/adminEvents/pages/AdminEventDetailsPage'));
const AdminPlanDashboardPage = lazy(() => import('../features/adminPlans/pages/AdminPlanDashboardPage'));
const AdminPlanDetailsPage = lazy(() => import('../features/adminPlans/pages/AdminPlanDetailsPage'));
const AdminAnalyticsPage = lazy(() => import('../features/dashboard/pages/AdminAnalyticsPage'));
const AdminUsersPage = lazy(() => import('../features/dashboard/pages/AdminUsersPage'));
const AdminVendorsPage = lazy(() => import('../features/dashboard/pages/AdminVendorsPage'));
const AdminSystemPage = lazy(() => import('../features/dashboard/pages/AdminSystemPage'));

function SuspendedPage({ children }) {
  return (
    <Suspense
      fallback={
        <Box role="status" sx={{ minHeight: '50vh', display: 'grid', placeItems: 'center' }}>
          <CircularProgress aria-label="Loading page" />
        </Box>
      }
    >
      {children}
    </Suspense>
  );
}

function AdminPage({ page: Page }) {
  return (
    <SuspendedPage>
      <ProtectedRoute>
        <Page />
      </ProtectedRoute>
    </SuspendedPage>
  );
}

export function LandingRoute() {
  return <SuspendedPage><LandingPage /></SuspendedPage>;
}

export function LoginRoute() {
  return <SuspendedPage><LoginPage /></SuspendedPage>;
}

export function AdminDashboardRoute() {
  return <AdminPage page={DashboardPage} />;
}

export function AdminAnalyticsRoute() {
  return <AdminPage page={AdminAnalyticsPage} />;
}

export function AdminUsersRoute() {
  return <AdminPage page={AdminUsersPage} />;
}

export function AdminEventsRoute() {
  return <AdminPage page={AdminEventManagementPage} />;
}

export function AdminEventDetailsRoute() {
  return <AdminPage page={AdminEventDetailsPage} />;
}

export function AdminVendorsRoute() {
  return <AdminPage page={AdminVendorsPage} />;
}

export function AdminPlansRoute() {
  return <AdminPage page={AdminPlanDashboardPage} />;
}

export function AdminPlanDetailsRoute() {
  return <AdminPage page={AdminPlanDetailsPage} />;
}

export function AdminSystemRoute() {
  return <AdminPage page={AdminSystemPage} />;
}
