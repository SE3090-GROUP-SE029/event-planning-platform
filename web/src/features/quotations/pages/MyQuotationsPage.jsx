import { useNavigate } from 'react-router-dom';
import {
  Alert,
  Box,
  Button,
  CircularProgress,
  Stack,
  Typography,
} from '@mui/material';
import AppLayout from '../../../shared/components/layout/AppLayout';
import SurfaceCard from '../../../shared/components/ui/SurfaceCard';
import StatusBadge from '../../../shared/components/ui/StatusBadge';
import {
  formatDateTime,
  formatQuotedPrice,
  formatQuotationStatus,
  useMyQuotations,
} from '../api/quotationApi';

export default function MyQuotationsPage() {
  const navigate = useNavigate();
  const { data: quotations = [], isLoading, isError, error } = useMyQuotations();

  return (
    <AppLayout
      activeTab="my-quotations"
      title="My quotations"
      subtitle="View-only status of your vendor quotation requests"
    >
      <Box sx={{ maxWidth: 1200, mx: 'auto', mt: 1 }}>
        <Button variant="text" sx={{ mb: 2 }} onClick={() => navigate('/marketplace')}>
          Browse marketplace
        </Button>

          {isLoading && (
            <Box sx={{ display: 'flex', justifyContent: 'center', mt: 6 }}>
              <CircularProgress />
            </Box>
          )}

          {isError && (
            <Alert severity="error">{error?.message || 'Failed to load quotations.'}</Alert>
          )}

          {!isLoading && !isError && quotations.length === 0 && (
            <SurfaceCard sx={{ p: 3 }}>
              <Typography color="text.secondary">
                You have not requested any quotations yet.
              </Typography>
            </SurfaceCard>
          )}

          <Stack spacing={2} sx={{ maxWidth: 920 }}>
            {quotations.map((q) => (
              <SurfaceCard key={q.id} sx={{ p: 3 }}>
                <Box sx={{ display: 'flex', justifyContent: 'space-between', gap: 2, flexWrap: 'wrap' }}>
                  <Box>
                    <Typography sx={{ fontWeight: 800, fontSize: 18 }}>{q.vendorBusinessName}</Typography>
                    <Typography color="text.secondary">{q.serviceName}</Typography>
                  </Box>
                  <StatusBadge
                    label={formatQuotationStatus(q.status)}
                    status={q.status === 'REQUESTED' ? 'PENDING' : q.status === 'QUOTED' ? 'APPROVED' : q.status}
                  />
                </Box>
                <Typography variant="body2" sx={{ mt: 1.5 }}>
                  Event: {q.eventType || '—'} · Guests: {q.guestCount ?? '—'}
                </Typography>
                <Typography variant="body2">
                  Requested: {formatDateTime(q.requestedStartDateTime)} →{' '}
                  {formatDateTime(q.requestedEndDateTime)}
                </Typography>
                <Typography variant="body2" sx={{ mt: 1 }}>
                  Message: {q.customerMessage || '—'}
                </Typography>
                <Typography variant="body2" sx={{ mt: 1, fontWeight: 700 }}>
                  Quoted price: {formatQuotedPrice(q.quotedPrice)}
                </Typography>
                <Typography variant="body2">Vendor terms: {q.vendorTerms || '—'}</Typography>
              </SurfaceCard>
            ))}
          </Stack>
        </Box>
    </AppLayout>
  );
}
