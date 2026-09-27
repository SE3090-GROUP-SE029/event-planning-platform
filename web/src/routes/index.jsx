import { createBrowserRouter, Navigate } from 'react-router-dom';
import {
  AdminAnalyticsRoute,
  AdminDashboardRoute,
  AdminEventDetailsRoute,
  AdminEventsRoute,
  AdminPlanDetailsRoute,
  AdminPlansRoute,
  AdminSystemRoute,
  AdminUsersRoute,
  AdminVendorsRoute,
  LandingRoute,
  LoginRoute,
} from './routeElements';

export const router = createBrowserRouter([
  {
    path: '/',
    element: <LandingRoute />,
  },
  {
    path: '/login',
    element: <LoginRoute />,
  },
  {
    path: '/register',
    element: <Navigate to="/" replace />,
  },
  {
    path: '/admin/dashboard',
    element: <AdminDashboardRoute />,
  },
  {
    path: '/admin/analytics',
    element: <AdminAnalyticsRoute />,
  },
  {
    path: '/admin/users',
    element: <AdminUsersRoute />,
  },
  {
    path: '/admin/events',
    element: <AdminEventsRoute />,
  },
  {
    path: '/admin/events/:id',
    element: <AdminEventDetailsRoute />,
  },
  {
    path: '/admin/vendors',
    element: <AdminVendorsRoute />,
  },
  {
    path: '/admin/plans',
    element: <AdminPlansRoute />,
  },
  {
    path: '/admin/plans/:id',
    element: <AdminPlanDetailsRoute />,
  },
  {
    path: '/admin/system',
    element: <AdminSystemRoute />,
  },
  {
    path: '*',
    element: <Navigate to="/" replace />,
  },
  
]);
