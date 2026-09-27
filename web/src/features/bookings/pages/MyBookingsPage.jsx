import { useNavigate } from 'react-router-dom';
import {
  Alert,
  Box,
  Button,
  CircularProgress,
  Stack,
  Typography,
} from '@mui/material';
import { useAuthStore } from '../../../shared/store/authStore';
import CollapsibleSidebar from '../../../shared/components/layout/CollapsibleSidebar';
import TopSearchNavbar from '../../../shared/components/layout/TopSearchNavbar';
import SurfaceCard from '../../../shared/components/ui/SurfaceCard';
import StatusBadge from '../../../shared/components/ui/StatusBadge';
import {
  formatBookingStatus,
  formatDateTime,
  formatQuotedPrice,
  useMyBookings,
} from '../api/bookingApi';

export default function MyBookingsPage() {
  const navigate = useNavigate();
  const user = useAuthStore((state) => state.user);
  const logout = useAuthStore((state) => state.logout);
  const isEventPlanner = useAuthStore((state) => state.hasRole('EVENT_PLANNER'));
  const isAdmin = useAuthStore((state) => state.hasRole('ADMIN'));
  const isVendor = useAuthStore((state) => state.hasRole('VENDOR'));
  const { data: bookings = [], isLoading, isError, error } = useMyBookings();

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  return (
    <Box sx={{ minHeight: '100vh', backgroundColor: '#F7F3E9', p: { xs: 1.5, md: 2.5 } }}>
      <Box sx={{ display: 'flex', gap: { xs: 2, md: 3 }, minHeight: 'calc(100vh - 32px)' }}>
        <CollapsibleSidebar
          activeTab="my-bookings"
          showEvents={isAdmin}
          showMarketplace={isEventPlanner || isAdmin}
          showMyQuotations={isEventPlanner}
          showMyBookings={isEventPlanner}
          showVendorProfile={isVendor}
          showVendorServices={isVendor}
          showVendorAvailability={isVendor}
          showVendorQuotations={isVendor}
          showVendorBookings={isVendor}
          onSelectTab={(tab) => {
            if (tab === 'dashboard') navigate('/dashboard');
            if (tab === 'events') navigate('/admin/events');
            if (tab === 'marketplace') navigate('/marketplace');
            if (tab === 'my-quotations') navigate('/quotations/mine');
            if (tab === 'my-bookings') navigate('/bookings/mine');
            if (tab === 'vendor-profile') navigate('/vendor/profile');
            if (tab === 'vendor-services') navigate('/vendor/services');
            if (tab === 'vendor-availability') navigate('/vendor/availability');
            if (tab === 'vendor-quotations') navigate('/vendor/quotations');
            if (tab === 'vendor-bookings') navigate('/vendor/bookings');
          }}
          onLogout={handleLogout}
        />

        <Box sx={{ flex: 1, minWidth: 0 }}>
          <TopSearchNavbar
            user={user}
            title="My bookings"
            subtitle="Confirmed vendor bookings for your events"
          />

          <Button variant="text" sx={{ mb: 2 }} onClick={() => navigate('/quotations/mine')}>
            Back to quotations
          </Button>

          {isLoading && (
            <Box sx={{ display: 'flex', justifyContent: 'center', mt: 6 }}>
              <CircularProgress />
            </Box>
          )}

          {isError && (
            <Alert severity="error">{error?.message || 'Failed to load bookings.'}</Alert>
          )}

          {!isLoading && !isError && bookings.length === 0 && (
            <SurfaceCard sx={{ p: 3 }}>
              <Typography color="text.secondary">No bookings yet. Accept a quotation to create one.</Typography>
            </SurfaceCard>
          )}

          <Stack spacing={2} sx={{ maxWidth: 920 }}>
            {bookings.map((b) => (
              <SurfaceCard key={b.id} sx={{ p: 3 }}>
                <Box sx={{ display: 'flex', justifyContent: 'space-between', gap: 2, flexWrap: 'wrap' }}>
                  <Box>
                    <Typography sx={{ fontWeight: 800, fontSize: 18 }}>{b.vendorBusinessName}</Typography>
                    <Typography color="text.secondary">{b.serviceName}</Typography>
                  </Box>
                  <StatusBadge label={formatBookingStatus(b.status)} status={b.status} />
                </Box>
                <Typography variant="body2" sx={{ mt: 1.5 }}>
                  Event: {b.eventType || '—'} · Guests: {b.guestCount ?? '—'}
                </Typography>
                <Typography variant="body2">
                  Schedule: {formatDateTime(b.startDateTime)} → {formatDateTime(b.endDateTime)}
                </Typography>
                <Typography variant="body2" sx={{ mt: 1, fontWeight: 700 }}>
                  Agreed price: {formatQuotedPrice(b.agreedPrice)}
                </Typography>
                <Button sx={{ mt: 1.5 }} onClick={() => navigate(`/bookings/${b.id}`)}>
                  View details
                </Button>
              </SurfaceCard>
            ))}
          </Stack>
        </Box>
      </Box>
    </Box>
  );
}
