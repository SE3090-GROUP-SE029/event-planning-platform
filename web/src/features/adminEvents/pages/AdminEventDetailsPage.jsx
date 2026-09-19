import { useNavigate, useParams } from 'react-router-dom';
import { Alert, Box, Button, Card, CardContent, CircularProgress, Grid, Stack, Typography } from '@mui/material';
import { EVENT_TYPES, useAdminEvent, enumLabel } from '../api/adminEventApi';

function Detail({ label, value }) {
  return <Box><Typography variant="caption" color="text.secondary">{label}</Typography><Typography sx={{ wordBreak: 'break-word' }}>{value || '—'}</Typography></Box>;
}

export default function AdminEventDetailsPage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const query = useAdminEvent(id);
  if (query.isLoading) return <Box sx={{ display: 'grid', placeItems: 'center', minHeight: '70vh' }}><CircularProgress /></Box>;
  if (query.isError) return <Box sx={{ maxWidth: 800, mx: 'auto', p: 3 }}><Alert severity="error">{query.error.message}</Alert><Button sx={{ mt: 2 }} onClick={() => navigate('/admin/events')}>Back to events</Button></Box>;
  const event = query.data;
  const ownerName = event.owner ? `${event.owner.firstName} ${event.owner.lastName}`.trim() : event.ownerId;
  return (
    <Box sx={{ minHeight: '100vh', backgroundColor: '#F7F3E9', p: { xs: 2, md: 5 } }}>
      <Box sx={{ maxWidth: 1100, mx: 'auto' }}>
        <Button onClick={() => navigate('/admin/events')} sx={{ mb: 2 }}>Back to events</Button>
        <Typography variant="h4" fontWeight={700} sx={{ mb: 3 }}>Event details</Typography>
        <Grid container spacing={2}>
          <Grid size={{ xs: 12, md: 8 }}>
            <Card sx={{ borderRadius: 3 }}><CardContent><Typography variant="h6" fontWeight={700} sx={{ mb: 2 }}>Event information</Typography><Grid container spacing={3}>
              <Grid size={{ xs: 12, sm: 6 }}><Detail label="Event ID" value={event.id} /></Grid>
              <Grid size={{ xs: 12, sm: 6 }}><Detail label="Event type" value={enumLabel(event.eventType, EVENT_TYPES.map((value) => value.replaceAll('_', ' ')))} /></Grid>
              <Grid size={{ xs: 12, sm: 6 }}><Detail label="Guest count" value={event.guestCount} /></Grid>
              <Grid size={{ xs: 12, sm: 6 }}><Detail label="Status" value={enumLabel(event.status)} /></Grid>
              <Grid size={{ xs: 12, sm: 6 }}><Detail label="Preferred venue" value={event.preferredVenue} /></Grid>
              <Grid size={{ xs: 12, sm: 6 }}><Detail label="Event duration" value={event.eventDuration} /></Grid>
              <Grid size={{ xs: 12, sm: 6 }}><Detail label="Preferred date" value={new Date(event.preferredDate).toLocaleString()} /></Grid>
              <Grid size={{ xs: 12, sm: 6 }}><Detail label="Created date" value={new Date(event.createdAt).toLocaleString()} /></Grid>
              <Grid size={{ xs: 12 }}><Detail label="Requirements" value={event.requirements} /></Grid>
            </Grid></CardContent></Card>
          </Grid>
          <Grid size={{ xs: 12, md: 4 }}>
            <Stack spacing={2}>
              <Card sx={{ borderRadius: 3 }}><CardContent><Typography variant="h6" fontWeight={700} sx={{ mb: 2 }}>Owner information</Typography><Stack spacing={2}><Detail label="Name" value={ownerName} /><Detail label="Email" value={event.owner?.email} /><Detail label="Owner ID" value={event.ownerId} /></Stack></CardContent></Card>
              <Card sx={{ borderRadius: 3 }}><CardContent><Typography variant="h6" fontWeight={700} sx={{ mb: 2 }}>Financials</Typography><Detail label="Budget" value={Number(event.budget).toLocaleString(undefined, { style: 'currency', currency: 'USD' })} /></CardContent></Card>
            </Stack>
          </Grid>
        </Grid>
      </Box>
    </Box>
  );
}
