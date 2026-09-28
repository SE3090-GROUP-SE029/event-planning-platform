import { useMemo, useState } from 'react';
import { useNavigate, useParams, useSearchParams } from 'react-router-dom';
import {
  Alert,
  Box,
  Button,
  CircularProgress,
  FormControl,
  InputLabel,
  MenuItem,
  Select,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import { useAuthStore } from '../../../shared/store/authStore';
import AppLayout from '../../../shared/components/layout/AppLayout';
import SurfaceCard from '../../../shared/components/ui/SurfaceCard';
import { useVendorMarketplaceDetail } from '../../vendors/api/vendorApi';
import {
  toApiDateTime,
  useCreateQuotation,
  useMyEventsForQuotation,
} from '../api/quotationApi';

export default function RequestQuotationPage() {
  const { vendorId } = useParams();
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const isEventPlanner = useAuthStore((state) => state.hasRole('EVENT_PLANNER'));

  const { data: vendor, isLoading: vendorLoading, isError: vendorError, error: vendorErr } =
    useVendorMarketplaceDetail(vendorId);
  const { data: events = [], isLoading: eventsLoading } = useMyEventsForQuotation(isEventPlanner);
  const createMutation = useCreateQuotation();

  const preselectedService = searchParams.get('serviceId') || '';
  const [vendorServiceId, setVendorServiceId] = useState(preselectedService);
  const [eventId, setEventId] = useState('');
  const [startDateTime, setStartDateTime] = useState('');
  const [endDateTime, setEndDateTime] = useState('');
  const [customerMessage, setCustomerMessage] = useState('');
  const [formError, setFormError] = useState('');

  const services = useMemo(() => vendor?.services || [], [vendor]);

  const handleSubmit = async (e) => {
    e.preventDefault();
    setFormError('');

    if (!isEventPlanner) {
      setFormError('Only event planners can request quotations.');
      return;
    }
    if (!vendorServiceId || !eventId || !startDateTime || !endDateTime) {
      setFormError('Service, event, start, and end date/time are required.');
      return;
    }

    try {
      await createMutation.mutateAsync({
        vendorId,
        vendorServiceId,
        eventId,
        requestedStartDateTime: toApiDateTime(startDateTime),
        requestedEndDateTime: toApiDateTime(endDateTime),
        customerMessage: customerMessage.trim() || null,
      });
      navigate('/quotations/mine');
    } catch (err) {
      setFormError(err?.message || 'Failed to submit quotation request.');
    }
  };

  return (
    <AppLayout
      activeTab="marketplace"
      title="Request quotation"
      subtitle="Send a request to this vendor"
    >
      <Box sx={{ maxWidth: 1200, mx: 'auto', mt: 1 }}>
        <Button variant="text" sx={{ mb: 2 }} onClick={() => navigate(`/marketplace/${vendorId}`)}>
          ← Back to vendor details
        </Button>

          {(vendorLoading || eventsLoading) && (
            <Box sx={{ display: 'flex', justifyContent: 'center', mt: 6 }}>
              <CircularProgress />
            </Box>
          )}

          {vendorError && (
            <Alert severity="error">{vendorErr?.message || 'Vendor not found.'}</Alert>
          )}

          {vendor && (
            <SurfaceCard sx={{ p: 3, maxWidth: 720 }}>
              <Typography variant="h5" sx={{ fontWeight: 800, mb: 0.5 }}>
                {vendor.businessName}
              </Typography>
              <Typography color="text.secondary" sx={{ mb: 2.5 }}>
                Choose a service and one of your events, then submit the request.
              </Typography>

              {(formError || createMutation.isError) && (
                <Alert severity="error" sx={{ mb: 2 }}>
                  {formError || createMutation.error?.message}
                </Alert>
              )}

              {!isEventPlanner && (
                <Alert severity="info" sx={{ mb: 2 }}>
                  Sign in as an Event Planner to request quotations.
                </Alert>
              )}

              <Box component="form" onSubmit={handleSubmit}>
                <Stack spacing={2}>
                  <FormControl fullWidth required>
                    <InputLabel id="service-label">Service</InputLabel>
                    <Select
                      labelId="service-label"
                      label="Service"
                      value={vendorServiceId}
                      onChange={(e) => setVendorServiceId(e.target.value)}
                    >
                      {services.map((service) => (
                        <MenuItem key={service.id} value={service.id}>
                          {service.serviceName}
                        </MenuItem>
                      ))}
                    </Select>
                  </FormControl>

                  <FormControl fullWidth required>
                    <InputLabel id="event-label">Your event</InputLabel>
                    <Select
                      labelId="event-label"
                      label="Your event"
                      value={eventId}
                      onChange={(e) => setEventId(e.target.value)}
                    >
                      {events.map((event) => (
                        <MenuItem key={event.id} value={event.id}>
                          {event.eventType} — {new Date(event.preferredDate).toLocaleDateString()} (
                          {event.guestCount} guests)
                        </MenuItem>
                      ))}
                    </Select>
                  </FormControl>

                  {events.length === 0 && isEventPlanner && (
                    <Alert severity="warning">
                      You need at least one event before requesting a quotation. Create an event first
                      (mobile Events flow or API).
                    </Alert>
                  )}

                  <TextField
                    label="Requested start"
                    type="datetime-local"
                    value={startDateTime}
                    onChange={(e) => setStartDateTime(e.target.value)}
                    InputLabelProps={{ shrink: true }}
                    required
                    fullWidth
                  />
                  <TextField
                    label="Requested end"
                    type="datetime-local"
                    value={endDateTime}
                    onChange={(e) => setEndDateTime(e.target.value)}
                    InputLabelProps={{ shrink: true }}
                    required
                    fullWidth
                  />
                  <TextField
                    label="Requirements / message (optional)"
                    value={customerMessage}
                    onChange={(e) => setCustomerMessage(e.target.value)}
                    multiline
                    minRows={3}
                    fullWidth
                  />

                  <Button
                    type="submit"
                    variant="contained"
                    disabled={!isEventPlanner || createMutation.isPending || events.length === 0}
                    sx={{ alignSelf: 'flex-start', bgcolor: '#19191C' }}
                  >
                    {createMutation.isPending ? 'Submitting…' : 'Submit quotation request'}
                  </Button>
                </Stack>
              </Box>
            </SurfaceCard>
          )}
        </Box>
    </AppLayout>
  );
}
