import { useNavigate, useParams } from 'react-router-dom';
import { Alert, Box, Button, CircularProgress, Grid, Stack, Typography } from '@mui/material';
import ArrowBackOutlinedIcon from '@mui/icons-material/ArrowBackOutlined';
import { EVENT_TYPES, useAdminEvent, enumLabel } from '../api/adminEventApi';
import AppLayout from '../../../shared/components/layout/AppLayout';
import SurfaceCard from '../../../shared/components/ui/SurfaceCard';
import StatusBadge from '../../../shared/components/ui/StatusBadge';

function Detail({ label, value }) {
  return (
    <Box>
      <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mb: 0.5, fontWeight: 600 }}>
        {label}
      </Typography>
      <Typography sx={{ wordBreak: 'break-word', fontWeight: 600, fontSize: '0.95rem' }}>
        {value || '—'}
      </Typography>
    </Box>
  );
}

function formatTimeRange(event) {
  if (!event.startTime || !event.endTime) return '-';
  return `${event.startTime} - ${event.endTime}`;
}

export default function AdminEventDetailsPage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const query = useAdminEvent(id);

  if (query.isLoading) {
    return (
      <AppLayout activeTab="events" title="Event Details" subtitle="Loading…">
        <Box sx={{ display: 'grid', placeItems: 'center', minHeight: '60vh' }}>
          <CircularProgress />
        </Box>
      </AppLayout>
    );
  }

  if (query.isError) {
    return (
      <AppLayout activeTab="events" title="Event Details" subtitle="Error">
        <Box sx={{ maxWidth: 800, p: 2 }}>
          <Alert severity="error">{query.error.message}</Alert>
          <Button sx={{ mt: 2, borderRadius: 9999 }} onClick={() => navigate('/admin/events')}>
            Back to events
          </Button>
        </Box>
      </AppLayout>
    );
  }

  const event = query.data;
  const ownerName = event.owner ? `${event.owner.firstName || ''} ${event.owner.lastName || ''}`.trim() : event.ownerId;

  return (
    <AppLayout
      activeTab="events"
      title="Event Inspection"
      subtitle={event.eventName || `Event #${event.id}`}
    >
      <Button
        startIcon={<ArrowBackOutlinedIcon />}
        onClick={() => navigate('/admin/events')}
        sx={{ mb: 2, borderRadius: 9999, px: 2.5 }}
      >
        Back to events
      </Button>

      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: 2, mb: 3 }}>
        <Box>
          <Typography variant="h3" sx={{ fontWeight: 800, letterSpacing: '-0.04em' }}>
            {event.eventName || 'Untitled Event'}
          </Typography>
          <Typography color="text.secondary" sx={{ mt: 0.5 }}>
            ID: {event.id}
          </Typography>
        </Box>
        <StatusBadge status={event.status} label={enumLabel(event.status)} />
      </Box>

      <Grid container spacing={2.5}>
        <Grid size={{ xs: 12, md: 8 }}>
          <SurfaceCard variant="task" sx={{ p: 3.5 }}>
            <Typography variant="h5" sx={{ fontWeight: 800, mb: 2.5 }}>
              Event Information
            </Typography>
            <Grid container spacing={2.5}>
              <Grid size={{ xs: 12, sm: 6 }}><Detail label="Event ID" value={event.id} /></Grid>
              <Grid size={{ xs: 12, sm: 6 }}><Detail label="Event name" value={event.eventName} /></Grid>
              <Grid size={{ xs: 12, sm: 6 }}><Detail label="Event type" value={enumLabel(event.eventType, EVENT_TYPES.map((value) => value.replaceAll('_', ' ')))} /></Grid>
              <Grid size={{ xs: 12, sm: 6 }}><Detail label="Guest count" value={event.guestCount} /></Grid>
              <Grid size={{ xs: 12, sm: 6 }}><Detail label="Status" value={enumLabel(event.status)} /></Grid>
              <Grid size={{ xs: 12, sm: 6 }}><Detail label="Preferred venue" value={event.preferredVenue} /></Grid>
              <Grid size={{ xs: 12, sm: 6 }}><Detail label="Start / end time" value={formatTimeRange(event)} /></Grid>
              <Grid size={{ xs: 12, sm: 6 }}><Detail label="Event duration" value={event.eventDuration} /></Grid>
              <Grid size={{ xs: 12, sm: 6 }}><Detail label="Preferred date" value={new Date(event.preferredDate).toLocaleString()} /></Grid>
              <Grid size={{ xs: 12, sm: 6 }}><Detail label="Created date" value={new Date(event.createdAt).toLocaleString()} /></Grid>
              <Grid size={{ xs: 12 }}><Detail label="Requirements" value={event.requirements} /></Grid>
            </Grid>
          </SurfaceCard>
        </Grid>

        <Grid size={{ xs: 12, md: 4 }}>
          <Stack spacing={2.5}>
            <SurfaceCard variant="analytics" sx={{ p: 3 }}>
              <Typography variant="h6" sx={{ fontWeight: 800, mb: 2 }}>
                Owner &amp; Planner
              </Typography>
              <Stack spacing={2}>
                <Detail label="Name" value={ownerName} />
                <Detail label="Email" value={event.owner?.email} />
                <Detail label="Owner ID" value={event.ownerId} />
              </Stack>
            </SurfaceCard>

            <SurfaceCard variant="budget" sx={{ p: 3 }}>
              <Typography variant="h6" sx={{ fontWeight: 800, mb: 2 }}>
                Financial Budget
              </Typography>
              <Detail
                label="Allocated Budget"
                value={Number(event.budget).toLocaleString(undefined, { style: 'currency', currency: 'USD' })}
              />
            </SurfaceCard>
          </Stack>
        </Grid>
      </Grid>
    </AppLayout>
  );
}
