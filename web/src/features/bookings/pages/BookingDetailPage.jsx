import { useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import {
  Alert,
  Box,
  Button,
  CircularProgress,
  Stack,
  TextField,
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
  useBooking,
  useCancelBooking,
  useCompleteBooking,
} from '../api/bookingApi';

export default function BookingDetailPage({ vendorMode = false }) {
  const { id } = useParams();
  const navigate = useNavigate();
  const user = useAuthStore((state) => state.user);
  const logout = useAuthStore((state) => state.logout);
  const isEventPlanner = useAuthStore((state) => state.hasRole('EVENT_PLANNER'));
  const isAdmin = useAuthStore((state) => state.hasRole('ADMIN'));
  const isVendor = useAuthStore((state) => state.hasRole('VENDOR'));
  const { data: booking, isLoading, isError, error } = useBooking(id);
  const cancelMutation = useCancelBooking();
  const completeMutation = useCompleteBooking();
  const [reason, setReason] = useState('');
  const [actionError, setActionError] = useState('');
  const [showCancel, setShowCancel] = useState(false);

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  const listPath = vendorMode ? '/vendor/bookings' : '/bookings/mine';

  const handleCancel = async () => {
    setActionError('');
    try {
      await cancelMutation.mutateAsync({ id, cancellationReason: reason });
      setShowCancel(false);
      setReason('');
    } catch (err) {
      setActionError(err?.response?.data?.message || err?.message || 'Failed to cancel booking.');
    }
  };

  const handleComplete = async () => {
    setActionError('');
    try {
      await completeMutation.mutateAsync(id);
    } catch (err) {
      setActionError(err?.response?.data?.message || err?.message || 'Failed to complete booking.');
    }
  };

  return (
    <Box sx={{ minHeight: '100vh', backgroundColor: '#F7F3E9', p: { xs: 1.5, md: 2.5 } }}>
      <Box sx={{ display: 'flex', gap: { xs: 2, md: 3 }, minHeight: 'calc(100vh - 32px)' }}>
        <CollapsibleSidebar
          activeTab={vendorMode ? 'vendor-bookings' : 'my-bookings'}
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
            title="Booking details"
            subtitle={booking?.vendorBusinessName || 'Booking'}
          />

          <Button variant="text" sx={{ mb: 2 }} onClick={() => navigate(listPath)}>
            Back to bookings
          </Button>

          {isLoading && (
            <Box sx={{ display: 'flex', justifyContent: 'center', mt: 6 }}>
              <CircularProgress />
            </Box>
          )}

          {isError && (
            <Alert severity="error">{error?.message || 'Failed to load booking.'}</Alert>
          )}

          {actionError && (
            <Alert severity="error" sx={{ mb: 2 }} onClose={() => setActionError('')}>
              {actionError}
            </Alert>
          )}

          {booking && (
            <SurfaceCard sx={{ p: 3, maxWidth: 720 }}>
              <Box sx={{ display: 'flex', justifyContent: 'space-between', gap: 2, flexWrap: 'wrap' }}>
                <Box>
                  <Typography sx={{ fontWeight: 800, fontSize: 20 }}>{booking.vendorBusinessName}</Typography>
                  <Typography color="text.secondary">{booking.serviceName}</Typography>
                </Box>
                <StatusBadge label={formatBookingStatus(booking.status)} status={booking.status} />
              </Box>
              <Typography variant="body2" sx={{ mt: 2 }}>
                Event: {booking.eventType || '—'} · Guests: {booking.guestCount ?? '—'}
              </Typography>
              <Typography variant="body2">
                Schedule: {formatDateTime(booking.startDateTime)} → {formatDateTime(booking.endDateTime)}
              </Typography>
              <Typography variant="body2" sx={{ mt: 1, fontWeight: 700 }}>
                Agreed price: {formatQuotedPrice(booking.agreedPrice)}
              </Typography>
              <Typography variant="body2">Vendor terms: {booking.vendorTerms || '—'}</Typography>
              {booking.cancellationReason && (
                <Typography variant="body2" sx={{ mt: 1 }}>
                  Cancellation reason: {booking.cancellationReason}
                </Typography>
              )}

              <Stack direction="row" spacing={1} sx={{ mt: 3 }} flexWrap="wrap" useFlexGap>
                {vendorMode && booking.status === 'CONFIRMED' && (
                  <Button
                    variant="contained"
                    sx={{ borderRadius: 9999 }}
                    disabled={completeMutation.isPending}
                    onClick={handleComplete}
                  >
                    Mark completed
                  </Button>
                )}
                {booking.status === 'CONFIRMED' && (
                  <Button
                    variant="outlined"
                    color="error"
                    sx={{ borderRadius: 9999 }}
                    onClick={() => setShowCancel((v) => !v)}
                  >
                    Cancel booking
                  </Button>
                )}
              </Stack>

              {showCancel && booking.status === 'CONFIRMED' && (
                <Box sx={{ mt: 2 }}>
                  <TextField
                    label="Cancellation reason"
                    fullWidth
                    multiline
                    minRows={2}
                    value={reason}
                    onChange={(e) => setReason(e.target.value)}
                  />
                  <Button
                    variant="contained"
                    color="error"
                    sx={{ mt: 1.5, borderRadius: 9999 }}
                    disabled={cancelMutation.isPending || !reason.trim()}
                    onClick={handleCancel}
                  >
                    Confirm cancel
                  </Button>
                </Box>
              )}
            </SurfaceCard>
          )}
        </Box>
      </Box>
    </Box>
  );
}
