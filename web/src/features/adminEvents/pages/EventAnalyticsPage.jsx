import { useNavigate, useParams } from 'react-router-dom';
import { Alert, Box, Button, CircularProgress, Grid, Stack, Typography } from '@mui/material';
import ArrowBackOutlinedIcon from '@mui/icons-material/ArrowBackOutlined';
import PeopleAltOutlinedIcon from '@mui/icons-material/PeopleAltOutlined';
import AutoAwesomeOutlinedIcon from '@mui/icons-material/AutoAwesomeOutlined';
import RefreshRoundedIcon from '@mui/icons-material/RefreshRounded';
import HowToRegOutlinedIcon from '@mui/icons-material/HowToRegOutlined';
import EventAvailableOutlinedIcon from '@mui/icons-material/EventAvailableOutlined';
import { useEventAnalytics } from '../api/adminEventApi';
import AppLayout from '../../../shared/components/layout/AppLayout';
import SurfaceCard from '../../../shared/components/ui/SurfaceCard';

function MetricCard({ label, value, detail, variant, icon: Icon }) {
  return (
    <SurfaceCard variant={variant} sx={{ p: 2.5, height: '100%' }}>
      <Stack direction="row" justifyContent="space-between" spacing={2}>
        <Box>
          <Typography variant="body2" color="text.secondary" sx={{ fontWeight: 600 }}>
            {label}
          </Typography>
          <Typography variant="h3" sx={{ mt: 0.75, fontWeight: 800 }}>
            {value}
          </Typography>
          {detail && (
            <Typography variant="caption" color="text.secondary">
              {detail}
            </Typography>
          )}
        </Box>
        {Icon && <Icon aria-hidden="true" color="action" />}
      </Stack>
    </SurfaceCard>
  );
}

function SummaryRow({ label, value }) {
  return (
    <Box sx={{ display: 'flex', justifyContent: 'space-between', py: 0.5 }}>
      <Typography variant="body2" color="text.secondary" sx={{ fontWeight: 600 }}>
        {label}
      </Typography>
      <Typography variant="body2" sx={{ fontWeight: 800 }}>
        {value}
      </Typography>
    </Box>
  );
}

export default function EventAnalyticsPage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const query = useEventAnalytics(id);

  if (query.isLoading) {
    return (
      <AppLayout activeTab="events" title="Event Analytics" subtitle="Loading metrics…">
        <Box sx={{ display: 'grid', placeItems: 'center', minHeight: '60vh' }}>
          <CircularProgress />
        </Box>
      </AppLayout>
    );
  }

  if (query.isError) {
    return (
      <AppLayout activeTab="events" title="Event Analytics" subtitle="Error">
        <Box sx={{ maxWidth: 800, p: 2 }}>
          <Alert severity="error">{query.error.message}</Alert>
          <Button sx={{ mt: 2, borderRadius: 9999 }} onClick={() => navigate(`/admin/events/${id}`)}>
            Back to event details
          </Button>
        </Box>
      </AppLayout>
    );
  }

  const data = query.data;

  return (
    <AppLayout
      activeTab="events"
      title="Event Analytics"
      subtitle="Registration and capacity metrics"
    >
      <Stack direction="row" gap={2} sx={{ mb: 3, flexWrap: 'wrap' }}>
        <Button
          startIcon={<ArrowBackOutlinedIcon />}
          onClick={() => navigate(`/admin/events/${id}`)}
          sx={{ borderRadius: 9999, px: 2.5 }}
        >
          Back to Event
        </Button>
        <Button
          startIcon={<RefreshRoundedIcon />}
          variant="outlined"
          onClick={() => query.refetch()}
          disabled={query.isFetching}
          sx={{ borderRadius: 9999, px: 2.5 }}
        >
          Refresh Data
        </Button>
      </Stack>

      <Typography variant="h4" sx={{ fontWeight: 800, letterSpacing: '-0.04em', mb: 3 }}>
        Overview
      </Typography>

      <Grid container spacing={2} sx={{ mb: 4 }}>
        <Grid size={{ xs: 12, sm: 6, lg: 3 }}>
          <MetricCard
            label="Total Registrations"
            value={data.totalRegistrations}
            variant="analytics"
            icon={PeopleAltOutlinedIcon}
          />
        </Grid>
        <Grid size={{ xs: 12, sm: 6, lg: 3 }}>
          <MetricCard
            label="Available Seats"
            value={data.availableSeats}
            detail={`out of ${data.capacity} capacity`}
            variant="task"
            icon={EventAvailableOutlinedIcon}
          />
        </Grid>
        <Grid size={{ xs: 12, sm: 6, lg: 3 }}>
          <MetricCard
            label="Checked-In"
            value={data.checkedIn}
            detail={`${data.notCheckedIn} not checked-in`}
            variant="vendor"
            icon={HowToRegOutlinedIcon}
          />
        </Grid>
        <Grid size={{ xs: 12, sm: 6, lg: 3 }}>
          <MetricCard
            label="Pending Review"
            value={data.pendingReview}
            variant="aiPlan"
            icon={AutoAwesomeOutlinedIcon}
          />
        </Grid>
      </Grid>

      <Grid container spacing={2.5}>
        <Grid size={{ xs: 12, md: 6 }}>
          <SurfaceCard sx={{ p: 3, height: '100%' }}>
            <Typography variant="h6" sx={{ mb: 2, fontWeight: 800 }}>
              Registration Pipeline
            </Typography>
            <Stack spacing={2}>
              <Box>
                <SummaryRow label="Accepted" value={data.accepted} />
                <SummaryRow label="Rejected" value={data.rejected} />
                <SummaryRow label="Waitlisted" value={data.waitlisted} />
              </Box>
              
              <Typography variant="subtitle2" sx={{ mt: 2, mb: 1, fontWeight: 700, color: 'text.secondary' }}>
                RSVP Responses
              </Typography>
              <Box>
                <SummaryRow label="RSVP Accepted" value={data.rsvpAccepted} />
                <SummaryRow label="RSVP Declined" value={data.rsvpDeclined} />
                <SummaryRow label="RSVP Maybe" value={data.rsvpMaybe} />
              </Box>
            </Stack>
          </SurfaceCard>
        </Grid>
      </Grid>
    </AppLayout>
  );
}
