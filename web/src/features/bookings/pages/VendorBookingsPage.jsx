import { useNavigate } from 'react-router-dom';
import {
  Alert,
  Box,
  Button,
  CircularProgress,
  Stack,
  Typography,
} from '@mui/material';
import SurfaceCard from '../../../shared/components/ui/SurfaceCard';
import StatusBadge from '../../../shared/components/ui/StatusBadge';
import VendorWorkspaceLayout from '../../vendors/components/VendorWorkspaceLayout';
import {
  formatBookingStatus,
  formatDateTime,
  formatQuotedPrice,
  useVendorBookings,
} from '../api/bookingApi';

export default function VendorBookingsPage() {
  const navigate = useNavigate();
  const { data: bookings = [], isLoading, isError, error } = useVendorBookings();

  return (
    <VendorWorkspaceLayout
      activeTab="vendor-bookings"
      title="Bookings"
      subtitle="Manage confirmed bookings for your business"
    >
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
          <Typography color="text.secondary">No bookings yet.</Typography>
        </SurfaceCard>
      )}

      <Stack spacing={2} sx={{ maxWidth: 920 }}>
        {bookings.map((b) => (
          <SurfaceCard key={b.id} sx={{ p: 3 }}>
            <Box sx={{ display: 'flex', justifyContent: 'space-between', gap: 2, flexWrap: 'wrap' }}>
              <Box>
                <Typography sx={{ fontWeight: 800 }}>{b.serviceName}</Typography>
                <Typography color="text.secondary">
                  Event: {b.eventType || '—'} · {b.guestCount ?? '—'} guests
                </Typography>
              </Box>
              <StatusBadge label={formatBookingStatus(b.status)} status={b.status} />
            </Box>
            <Typography variant="body2" sx={{ mt: 1.5 }}>
              Schedule: {formatDateTime(b.startDateTime)} → {formatDateTime(b.endDateTime)}
            </Typography>
            <Typography variant="body2" sx={{ fontWeight: 700 }}>
              Agreed price: {formatQuotedPrice(b.agreedPrice)}
            </Typography>
            <Button sx={{ mt: 1.5 }} onClick={() => navigate(`/vendor/bookings/${b.id}`)}>
              Manage booking
            </Button>
          </SurfaceCard>
        ))}
      </Stack>
    </VendorWorkspaceLayout>
  );
}
