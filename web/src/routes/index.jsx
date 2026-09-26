import { createBrowserRouter, Navigate } from 'react-router-dom';
import LoginPage from '../features/auth/pages/LoginPage';
import RegisterPage from '../features/auth/pages/RegisterPage';
import DashboardPage from '../features/dashboard/pages/DashboardPage';
import ProtectedRoute from '../shared/components/ProtectedRoute';
import AdminEventManagementPage from '../features/adminEvents/pages/AdminEventManagementPage';
import AdminEventDetailsPage from '../features/adminEvents/pages/AdminEventDetailsPage';
import VendorProfilePage from '../features/vendors/pages/VendorProfilePage';
import VendorServicesPage from '../features/vendors/pages/VendorServicesPage';
import { ScheduleBuilderPage } from '../features/scheduling/pages/ScheduleBuilderPage';

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
    path: '/vendor/profile',
    element: <ProtectedRoute requiredRole="VENDOR"><VendorProfilePage /></ProtectedRoute>,
  },
  {
    path: '/vendor/services',
    element: <ProtectedRoute requiredRole="VENDOR"><VendorServicesPage /></ProtectedRoute>,
  },
  {
  path: '/events/:eventId/schedule',
  element: <ScheduleBuilderPage />
  }
]);
