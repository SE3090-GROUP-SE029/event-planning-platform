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
  formatDateTime,
  formatQuotedPrice,
  formatQuotationStatus,
  useVendorQuotations,
} from '../api/quotationApi';

function badgeStatus(status) {
  if (status === 'REQUESTED') return 'PENDING';
  if (status === 'QUOTED') return 'APPROVED';
  return status;
}

export default function VendorQuotationsPage() {
  const navigate = useNavigate();
  const { data: quotations = [], isLoading, isError, error } = useVendorQuotations();

  return (
    <VendorWorkspaceLayout
      activeTab="vendor-quotations"
      title="Quotation requests"
      subtitle="Review and respond to planner quotation requests"
    >
      {isLoading && (
        <Box sx={{ display: 'flex', justifyContent: 'center', mt: 6 }}>
          <CircularProgress />
        </Box>
      )}

      {isError && (
        <Alert severity="error">{error?.message || 'Failed to load quotation requests.'}</Alert>
      )}

      {!isLoading && !isError && quotations.length === 0 && (
        <SurfaceCard sx={{ p: 3 }}>
          <Typography color="text.secondary">No quotation requests yet.</Typography>
        </SurfaceCard>
      )}

      <Stack spacing={2} sx={{ maxWidth: 920 }}>
        {quotations.map((q) => (
          <SurfaceCard key={q.id} sx={{ p: 3 }}>
            <Box sx={{ display: 'flex', justifyContent: 'space-between', gap: 2, flexWrap: 'wrap' }}>
              <Box>
                <Typography sx={{ fontWeight: 800 }}>{q.serviceName}</Typography>
                <Typography color="text.secondary">
                  Event: {q.eventType || '—'} · {q.guestCount ?? '—'} guests
                </Typography>
              </Box>
              <StatusBadge
                label={formatQuotationStatus(q.status)}
                status={badgeStatus(q.status)}
              />
            </Box>
            <Typography variant="body2" sx={{ mt: 1.5 }}>
              Requested: {formatDateTime(q.requestedStartDateTime)} →{' '}
              {formatDateTime(q.requestedEndDateTime)}
            </Typography>
            <Typography variant="body2">Message: {q.customerMessage || '—'}</Typography>
            {q.status === 'QUOTED' && (
              <Typography variant="body2" sx={{ mt: 1, fontWeight: 700 }}>
                Quote: {formatQuotedPrice(q.quotedPrice)}
              </Typography>
            )}
            <Button
              sx={{ mt: 1.5 }}
              onClick={() => navigate(`/vendor/quotations/${q.id}`)}
            >
              {q.status === 'REQUESTED' ? 'View & respond' : 'View details'}
            </Button>
          </SurfaceCard>
        ))}
      </Stack>
    </VendorWorkspaceLayout>
  );
}
