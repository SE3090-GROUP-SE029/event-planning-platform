import { createBrowserRouter, Navigate } from 'react-router-dom';
import LoginPage from '../features/auth/pages/LoginPage';
import RegisterPage from '../features/auth/pages/RegisterPage';
import DashboardPage from '../features/dashboard/pages/DashboardPage';
import ProtectedRoute from '../shared/components/ProtectedRoute';
import AdminEventManagementPage from '../features/adminEvents/pages/AdminEventManagementPage';
import AdminEventDetailsPage from '../features/adminEvents/pages/AdminEventDetailsPage';
import VendorProfilePage from '../features/vendors/pages/VendorProfilePage';
import VendorServicesPage from '../features/vendors/pages/VendorServicesPage';
import VendorAvailabilityPage from '../features/vendors/pages/VendorAvailabilityPage';
import VendorMarketplacePage from '../features/vendors/pages/VendorMarketplacePage';
import VendorMarketplaceDetailPage from '../features/vendors/pages/VendorMarketplaceDetailPage';
import AdminPlanDashboardPage from '../features/adminPlans/pages/AdminPlanDashboardPage';
import AdminPlanDetailsPage from '../features/adminPlans/pages/AdminPlanDetailsPage';
import MyEventsPage from '../features/ownerEvents/pages/MyEventsPage';
import OwnerPlanReviewPage from '../features/ownerEvents/pages/OwnerPlanReviewPage';

export const router = createBrowserRouter([
  {
    path: '/',
    element: <Navigate to="/dashboard" replace />,
  },
  {
    path: '/login',
    element: <LoginPage />,
  },
  {
    path: '/register',
    element: <RegisterPage />,
  },
  {
    path: '/dashboard',
    element: (
      <ProtectedRoute>
        <DashboardPage />
      </ProtectedRoute>
    ),
  },
  {
    path: '/admin/events',
    element: <ProtectedRoute requiredRole="ADMIN"><AdminEventManagementPage /></ProtectedRoute>,
  },
  {
    path: '/admin/events/:id',
    element: <ProtectedRoute requiredRole="ADMIN"><AdminEventDetailsPage /></ProtectedRoute>,
  },
  {
    path: '/admin/plans',
    element: <ProtectedRoute requiredRole="ADMIN"><AdminPlanDashboardPage /></ProtectedRoute>,
  },
  {
    path: '/admin/plans/:id',
    element: <ProtectedRoute requiredRole="ADMIN"><AdminPlanDetailsPage /></ProtectedRoute>,
  },
  {
    path: '/my-events',
    element: <ProtectedRoute requiredRole="EVENT_PLANNER"><MyEventsPage /></ProtectedRoute>,
  },
  {
    path: '/my-events/plans/:id',
    element: <ProtectedRoute requiredRole="EVENT_PLANNER"><OwnerPlanReviewPage /></ProtectedRoute>,
  },
  {
    path: '/marketplace',
    element: <ProtectedRoute><VendorMarketplacePage /></ProtectedRoute>,
  },
  {
    path: '/marketplace/:vendorId',
    element: <ProtectedRoute><VendorMarketplaceDetailPage /></ProtectedRoute>,
  },
  {
    path: '/vendor/profile',
    element: <ProtectedRoute requiredRole="VENDOR"><VendorProfilePage /></ProtectedRoute>,
  },
  {
    path: '/vendor/services',
    element: <ProtectedRoute requiredRole="VENDOR"><VendorServicesPage /></ProtectedRoute>,
  },
  {
    path: '/vendor/availability',
    element: <ProtectedRoute requiredRole="VENDOR"><VendorAvailabilityPage /></ProtectedRoute>,
  },
]);
