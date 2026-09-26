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
import SurfaceCard from '../../../shared/components/ui/SurfaceCard';
import StatusBadge from '../../../shared/components/ui/StatusBadge';
import VendorWorkspaceLayout from '../../vendors/components/VendorWorkspaceLayout';
import {
  formatDateTime,
  formatQuotedPrice,
  formatQuotationStatus,
  useQuotation,
  useRespondToQuotation,
} from '../api/quotationApi';

function badgeStatus(status) {
  if (status === 'REQUESTED') return 'PENDING';
  if (status === 'QUOTED') return 'APPROVED';
  return status;
}

export default function VendorQuotationDetailPage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const { data: quotation, isLoading, isError, error } = useQuotation(id);
  const respondMutation = useRespondToQuotation();
  const [quotedPrice, setQuotedPrice] = useState('');
  const [vendorTerms, setVendorTerms] = useState('');
  const [formError, setFormError] = useState('');

  const handleRespond = async (e) => {
    e.preventDefault();
    setFormError('');
    const price = Number(quotedPrice);
    if (Number.isNaN(price) || price < 0) {
      setFormError('Enter a valid non-negative quoted price.');
      return;
    }

    try {
      await respondMutation.mutateAsync({
        id,
        payload: {
          quotedPrice: price,
          vendorTerms: vendorTerms.trim() || null,
        },
      });
      navigate('/vendor/quotations');
    } catch (err) {
      setFormError(err?.message || 'Failed to submit response.');
    }
  };

  return (
    <VendorWorkspaceLayout
      activeTab="vendor-quotations"
      title="Quotation request"
      subtitle="Review request details and submit your quote"
    >
      <Button variant="text" sx={{ mb: 2 }} onClick={() => navigate('/vendor/quotations')}>
        ← Back to quotation requests
      </Button>

      {isLoading && (
        <Box sx={{ display: 'flex', justifyContent: 'center', mt: 6 }}>
          <CircularProgress />
        </Box>
      )}

      {isError && (
        <Alert severity="error">{error?.message || 'Quotation not found.'}</Alert>
      )}

      {quotation && (
        <Stack spacing={2} sx={{ maxWidth: 720 }}>
          <SurfaceCard sx={{ p: 3 }}>
            <Box sx={{ display: 'flex', justifyContent: 'space-between', gap: 2, flexWrap: 'wrap' }}>
              <Typography variant="h5" sx={{ fontWeight: 800 }}>
                {quotation.serviceName}
              </Typography>
              <StatusBadge
                label={formatQuotationStatus(quotation.status)}
                status={badgeStatus(quotation.status)}
              />
            </Box>

            <Typography sx={{ mt: 2, fontWeight: 700 }}>Event information</Typography>
            <Typography variant="body2">Type: {quotation.eventType || '—'}</Typography>
            <Typography variant="body2">Guests: {quotation.guestCount ?? '—'}</Typography>
            <Typography variant="body2">
              Preferred date: {formatDateTime(quotation.eventPreferredDate)}
            </Typography>
            <Typography variant="body2">
              Event requirements: {quotation.eventRequirements || '—'}
            </Typography>

            <Typography sx={{ mt: 2, fontWeight: 700 }}>Requested schedule</Typography>
            <Typography variant="body2">
              {formatDateTime(quotation.requestedStartDateTime)} →{' '}
              {formatDateTime(quotation.requestedEndDateTime)}
            </Typography>

            <Typography sx={{ mt: 2, fontWeight: 700 }}>Customer message</Typography>
            <Typography variant="body2">{quotation.customerMessage || '—'}</Typography>

            {quotation.status === 'QUOTED' && (
              <>
                <Typography sx={{ mt: 2, fontWeight: 700 }}>Your response</Typography>
                <Typography variant="body2">
                  Quoted price: {formatQuotedPrice(quotation.quotedPrice)}
                </Typography>
                <Typography variant="body2">Terms: {quotation.vendorTerms || '—'}</Typography>
              </>
            )}
          </SurfaceCard>

          {quotation.status === 'REQUESTED' && (
            <SurfaceCard sx={{ p: 3 }} component="form" onSubmit={handleRespond}>
              <Typography variant="h6" sx={{ fontWeight: 700, mb: 2 }}>
                Respond to quotation
              </Typography>
              {(formError || respondMutation.isError) && (
                <Alert severity="error" sx={{ mb: 2 }}>
                  {formError || respondMutation.error?.message}
                </Alert>
              )}
              <Stack spacing={2}>
                <TextField
                  label="Quoted price (Rs.)"
                  type="number"
                  inputProps={{ min: 0, step: '0.01' }}
                  value={quotedPrice}
                  onChange={(e) => setQuotedPrice(e.target.value)}
                  required
                  fullWidth
                />
                <TextField
                  label="Terms / notes (optional)"
                  value={vendorTerms}
                  onChange={(e) => setVendorTerms(e.target.value)}
                  multiline
                  minRows={3}
                  fullWidth
                />
                <Button
                  type="submit"
                  variant="contained"
                  disabled={respondMutation.isPending}
                  sx={{ alignSelf: 'flex-start', bgcolor: '#19191C' }}
                >
                  {respondMutation.isPending ? 'Submitting…' : 'Submit response'}
                </Button>
              </Stack>
            </SurfaceCard>
          )}
        </Stack>
      )}
    </VendorWorkspaceLayout>
  );
}
