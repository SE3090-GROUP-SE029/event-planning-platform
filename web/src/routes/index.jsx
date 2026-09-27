import { createBrowserRouter, Navigate } from 'react-router-dom';
import LoginPage from '../features/auth/pages/LoginPage';
import DashboardPage from '../features/dashboard/pages/DashboardPage';
import ProtectedRoute from '../shared/components/ProtectedRoute';
import AdminEventManagementPage from '../features/adminEvents/pages/AdminEventManagementPage';
import AdminEventDetailsPage from '../features/adminEvents/pages/AdminEventDetailsPage';
import AdminPlanDashboardPage from '../features/adminPlans/pages/AdminPlanDashboardPage';
import AdminPlanDetailsPage from '../features/adminPlans/pages/AdminPlanDetailsPage';

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
    element: <Navigate to="/login" replace />,
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
]);
