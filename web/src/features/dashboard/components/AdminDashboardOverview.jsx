import {
  Alert,
  Box,
  Button,
  CircularProgress,
  Grid,
  Stack,
  Typography,
} from '@mui/material';
import PeopleAltOutlinedIcon from '@mui/icons-material/PeopleAltOutlined';
import EventAvailableIcon from '@mui/icons-material/EventAvailable';
import AutoAwesomeOutlinedIcon from '@mui/icons-material/AutoAwesomeOutlined';
import StorefrontOutlinedIcon from '@mui/icons-material/StorefrontOutlined';
import RefreshRoundedIcon from '@mui/icons-material/RefreshRounded';
import { useNavigate } from 'react-router-dom';
import SurfaceCard from '../../../shared/components/ui/SurfaceCard';
import { useAdminAnalytics } from '../api/adminDashboardApi';
import { tokens } from '../../../shared/theme/tokens';

const metricCards = [
  {
    label: 'Total users',
    value: (data) => data.users.total,
    detail: (data) => `${data.users.active} active accounts`,
    variant: 'analytics',
    icon: PeopleAltOutlinedIcon,
  },
  {
    label: 'Total events',
    value: (data) => data.events.total,
    detail: (data) => `${data.events.createdThisMonth} created this month`,
    variant: 'task',
    icon: EventAvailableIcon,
  },
  {
    label: 'Total vendors',
    value: (data) => data.vendors.total,
    detail: (data) => `${data.vendors.pending} pending`,
    variant: 'vendor',
    icon: StorefrontOutlinedIcon,
  },
  {
    label: 'AI plans generated',
    value: (data) => data.aiPlans.generated,
    detail: (data) => `${data.aiPlans.generatedThisMonth} generated this month`,
    variant: 'aiPlan',
    icon: AutoAwesomeOutlinedIcon,
  },
];

function SummaryCard({ label, value, variant }) {
  return (
    <SurfaceCard variant={variant} sx={{ p: 2.5, height: '100%' }}>
      <Typography variant="body2" color="text.secondary" sx={{ fontWeight: 600 }}>
        {label}
      </Typography>
      <Typography variant="h4" sx={{ mt: 0.75, fontWeight: 800 }}>
        {value}
      </Typography>
    </SurfaceCard>
  );
}

function TrendCard({ label, field, trends = [], variant }) {
  const values = trends.map((point) => Number(point[field] || 0));
  const maxValue = Math.max(...values, 1);

  return (
    <SurfaceCard variant={variant} sx={{ p: 2.5, height: '100%' }}>
      <Typography variant="h6" sx={{ mb: 2, fontWeight: 800 }}>
        {label}
      </Typography>
      {trends.length === 0 ? (
        <Typography variant="body2" color="text.secondary">
          Trend history is not available yet.
        </Typography>
      ) : (
        <Stack direction="row" spacing={1} sx={{ height: 132, alignItems: 'flex-end' }}>
          {trends.map((point, index) => {
            const value = values[index];
            return (
              <Stack
                key={point.month}
                spacing={0.5}
                sx={{ flex: 1, alignItems: 'center', justifyContent: 'flex-end', minWidth: 0, height: '100%' }}
              >
                <Typography variant="caption" color="text.secondary">{value}</Typography>
                <Box
                  role="img"
                  aria-label={`${point.month}: ${value}`}
                  sx={{
                    width: '100%',
                    maxWidth: 34,
                    height: `${Math.max(value === 0 ? 2 : (value / maxValue) * 84, 2)}px`,
                    minHeight: 2,
                    borderRadius: 1,
                    bgcolor: variant === 'analytics'
                      ? tokens.colors.pastelBlue
                      : variant === 'aiPlan'
                        ? tokens.colors.pastelLavender
                        : tokens.colors.pastelPink,
                  }}
                />
                <Typography variant="caption" color="text.secondary" noWrap>
                  {new Date(`${point.month}-01T00:00:00`).toLocaleString(undefined, { month: 'short' })}
                </Typography>
              </Stack>
            );
          })}
        </Stack>
      )}
    </SurfaceCard>
  );
}

export default function AdminDashboardOverview() {
  const navigate = useNavigate();
  const query = useAdminAnalytics();
  const data = query.data;

  if (query.isLoading) {
    return (
      <Box role="status" sx={{ display: 'flex', justifyContent: 'center', py: 8 }}>
        <CircularProgress aria-label="Loading administrator analytics" />
      </Box>
    );
  }

  if (query.isError) {
    return (
      <Alert
        severity="error"
        action={
          <Button color="inherit" size="small" startIcon={<RefreshRoundedIcon />} onClick={() => query.refetch()}>
            Retry
          </Button>
        }
      >
        Analytics could not be loaded. {query.error.message}
      </Alert>
    );
  }

  if (!data) {
    return <Alert severity="info">Analytics are not available yet.</Alert>;
  }

  const isEmpty = [data.users, data.events, data.aiPlans, data.vendors].every((group) =>
    Object.values(group).every((value) => typeof value === 'number' && value === 0),
  );

  return (
    <Stack spacing={3}>
      {isEmpty && (
        <Alert severity="info">
          No platform activity has been recorded yet. Metrics will appear as accounts, events, vendors, and plans are created.
        </Alert>
      )}

      <Grid container spacing={2}>
        {metricCards.map((metric) => {
          const Icon = metric.icon;
          return (
            <Grid key={metric.label} size={{ xs: 12, sm: 6, lg: 3 }}>
              <SurfaceCard variant={metric.variant} sx={{ p: 2.5, height: '100%' }}>
                <Stack direction="row" justifyContent="space-between" spacing={2}>
                  <Box>
                    <Typography variant="body2" color="text.secondary" sx={{ fontWeight: 600 }}>
                      {metric.label}
                    </Typography>
                    <Typography variant="h3" sx={{ mt: 0.75, fontWeight: 800 }}>
                      {metric.value(data)}
                    </Typography>
                    <Typography variant="caption" color="text.secondary">
                      {metric.detail(data)}
                    </Typography>
                  </Box>
                  <Icon aria-hidden="true" color="action" />
                </Stack>
              </SurfaceCard>
            </Grid>
          );
        })}
      </Grid>

      <Grid container spacing={2.5}>
        <Grid size={{ xs: 12, md: 4 }}>
          <TrendCard label="Event growth" field="events" trends={data.trends} variant="analytics" />
        </Grid>
        <Grid size={{ xs: 12, md: 4 }}>
          <TrendCard label="AI plan generation" field="plans" trends={data.trends} variant="aiPlan" />
        </Grid>
        <Grid size={{ xs: 12, md: 4 }}>
          <TrendCard label="Vendor activity" field="vendors" trends={data.trends} variant="vendor" />
        </Grid>
        <Grid size={{ xs: 12, md: 6 }}>
          <SurfaceCard sx={{ p: 3, height: '100%' }}>
            <Typography variant="h5" sx={{ mb: 2, fontWeight: 800 }}>
              User accounts
            </Typography>
            <Stack spacing={1}>
              <SummaryCard label="Event planners" value={data.users.eventPlanners} variant="analytics" />
              <SummaryCard label="Vendors" value={data.users.vendors} variant="vendor" />
              <SummaryCard label="Administrators" value={data.users.administrators} variant="aiPlan" />
              <SummaryCard label="Registrations this month" value={data.users.registrationsThisMonth} variant="task" />
            </Stack>
            <Button sx={{ mt: 2 }} onClick={() => navigate('/admin/users')}>
              Manage users
            </Button>
          </SurfaceCard>
        </Grid>

        <Grid size={{ xs: 12, md: 6 }}>
          <SurfaceCard sx={{ p: 3, height: '100%' }}>
            <Typography variant="h5" sx={{ mb: 2, fontWeight: 800 }}>
              Event operations
            </Typography>
            <Stack spacing={1}>
              <SummaryCard label="Active events" value={data.events.active} variant="task" />
              <SummaryCard label="Completed events" value={data.events.completed} variant="analytics" />
              <SummaryCard label="Cancelled events" value={data.events.cancelled} variant="budget" />
              <SummaryCard label="Created this month" value={data.events.createdThisMonth} variant="vendor" />
            </Stack>
            <Button sx={{ mt: 2 }} onClick={() => navigate('/admin/events')}>
              Monitor events
            </Button>
          </SurfaceCard>
        </Grid>

        <Grid size={{ xs: 12, md: 6 }}>
          <SurfaceCard sx={{ p: 3, height: '100%' }}>
            <Typography variant="h5" sx={{ mb: 2, fontWeight: 800 }}>
              AI plan reviews
            </Typography>
            <Stack spacing={1}>
              <SummaryCard label="Pending approval" value={data.aiPlans.pending} variant="budget" />
              <SummaryCard label="Approved" value={data.aiPlans.approved} variant="task" />
              <SummaryCard label="Rejected" value={data.aiPlans.rejected} variant="vendor" />
              <SummaryCard label="Approval rate" value={`${data.aiPlans.approvalRate}%`} variant="aiPlan" />
            </Stack>
            <Button sx={{ mt: 2 }} onClick={() => navigate('/admin/plans')}>
              Review AI plans
            </Button>
          </SurfaceCard>
        </Grid>

        <Grid size={{ xs: 12, md: 6 }}>
          <SurfaceCard sx={{ p: 3, height: '100%' }}>
            <Typography variant="h5" sx={{ mb: 1, fontWeight: 800 }}>
              Monitoring &amp; activity
            </Typography>
            <Typography color="text.secondary" sx={{ mb: 2 }}>
              User and vendor approval queues are based on stored platform records. General audit activity and service uptime are not currently exposed by the backend.
            </Typography>
            <Typography variant="body2" sx={{ mb: 0.5 }}>
              Pending vendor requests: <strong>{data.vendors.pending}</strong>
            </Typography>
            <Typography variant="body2">
              Pending AI plan reviews: <strong>{data.aiPlans.pending}</strong>
            </Typography>
            <Button
              startIcon={<RefreshRoundedIcon />}
              sx={{ mt: 2 }}
              onClick={() => query.refetch()}
              disabled={query.isFetching}
            >
              Refresh metrics
            </Button>
          </SurfaceCard>
        </Grid>
      </Grid>
    </Stack>
  );
}
