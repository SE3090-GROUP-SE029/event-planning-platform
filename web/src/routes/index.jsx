import { createBrowserRouter, Navigate } from 'react-router-dom';
import LoginPage from '../features/auth/pages/LoginPage';
import DashboardPage from '../features/dashboard/pages/DashboardPage';
import ProtectedRoute from '../shared/components/ProtectedRoute';
import AdminEventManagementPage from '../features/adminEvents/pages/AdminEventManagementPage';
import AdminEventDetailsPage from '../features/adminEvents/pages/AdminEventDetailsPage';
import AdminPlanDashboardPage from '../features/adminPlans/pages/AdminPlanDashboardPage';
import AdminPlanDetailsPage from '../features/adminPlans/pages/AdminPlanDetailsPage';
import AdminVendorDirectoryPage from '../features/adminVendors/pages/AdminVendorDirectoryPage';
import AdminVendorDetailsPage from '../features/adminVendors/pages/AdminVendorDetailsPage';
import VendorProfilePage from '../features/vendors/pages/VendorProfilePage';
import VendorServicesPage from '../features/vendors/pages/VendorServicesPage';
import VendorAvailabilityPage from '../features/vendors/pages/VendorAvailabilityPage';
import VendorMarketplacePage from '../features/vendors/pages/VendorMarketplacePage';
import VendorMarketplaceDetailPage from '../features/vendors/pages/VendorMarketplaceDetailPage';
import RequestQuotationPage from '../features/quotations/pages/RequestQuotationPage';
import MyQuotationsPage from '../features/quotations/pages/MyQuotationsPage';
import VendorQuotationsPage from '../features/quotations/pages/VendorQuotationsPage';
import VendorQuotationDetailPage from '../features/quotations/pages/VendorQuotationDetailPage';
import MyBookingsPage from '../features/bookings/pages/MyBookingsPage';
import VendorBookingsPage from '../features/bookings/pages/VendorBookingsPage';
import BookingDetailPage from '../features/bookings/pages/BookingDetailPage';

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
  {
    path: '/admin/vendors',
    element: <ProtectedRoute requiredRole="ADMIN"><AdminVendorDirectoryPage /></ProtectedRoute>,
  },
  {
    path: '/admin/vendors/:id',
    element: <ProtectedRoute requiredRole="ADMIN"><AdminVendorDetailsPage /></ProtectedRoute>,
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
    path: '/marketplace/:vendorId/request-quotation',
    element: <ProtectedRoute requiredRole="EVENT_PLANNER"><RequestQuotationPage /></ProtectedRoute>,
  },
  {
    path: '/quotations/mine',
    element: <ProtectedRoute requiredRole="EVENT_PLANNER"><MyQuotationsPage /></ProtectedRoute>,
  },
  {
    path: '/bookings/mine',
    element: <ProtectedRoute requiredRole="EVENT_PLANNER"><MyBookingsPage /></ProtectedRoute>,
  },
  {
    path: '/bookings/:id',
    element: <ProtectedRoute><BookingDetailPage /></ProtectedRoute>,
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
  {
    path: '/vendor/quotations',
    element: <ProtectedRoute requiredRole="VENDOR"><VendorQuotationsPage /></ProtectedRoute>,
  },
  {
    path: '/vendor/quotations/:id',
    element: <ProtectedRoute requiredRole="VENDOR"><VendorQuotationDetailPage /></ProtectedRoute>,
  },
  {
    path: '/vendor/bookings',
    element: <ProtectedRoute requiredRole="VENDOR"><VendorBookingsPage /></ProtectedRoute>,
  },
  {
    path: '/vendor/bookings/:id',
    element: <ProtectedRoute requiredRole="VENDOR"><BookingDetailPage vendorMode /></ProtectedRoute>,
  },
]);
