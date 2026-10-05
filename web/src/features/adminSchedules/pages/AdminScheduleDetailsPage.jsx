import { useNavigate, useParams } from 'react-router-dom';
import {
  Alert,
  Box,
  Button,
  Chip,
  CircularProgress,
  Grid,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Typography,
} from '@mui/material';
import ArrowBackOutlinedIcon from '@mui/icons-material/ArrowBackOutlined';
import AppLayout from '../../../shared/components/layout/AppLayout';
import SurfaceCard from '../../../shared/components/ui/SurfaceCard';
import StatusBadge from '../../../shared/components/ui/StatusBadge';
import {
  conflictLabel,
  formatDate,
  formatDateTime,
  formatTime,
  formatWallClockTime,
  useAdminSchedule,
} from '../api/adminScheduleApi';
import { tokens } from '../../../shared/theme/tokens';

function Detail({ label, value }) {
  return (
    <Box>
      <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mb: 0.5, fontWeight: 600 }}>
        {label}
      </Typography>
      <Typography sx={{ wordBreak: 'break-word', fontWeight: 600, fontSize: '0.95rem' }}>
        {value || '-'}
      </Typography>
    </Box>
  );
}

function plannerName(planner) {
  if (!planner) return '-';
  return `${planner.firstName || ''} ${planner.lastName || ''}`.trim() || planner.email;
}

function conflictSx(type, isResolved) {
  if (isResolved) {
    return { bgcolor: tokens.colors.pastelGreenLight, color: tokens.colors.pastelGreenText };
  }
  if (type === 'VendorDoubleBooked') {
    return { bgcolor: tokens.colors.pastelPinkLight, color: tokens.colors.pastelPinkText };
  }
  return { bgcolor: tokens.colors.pastelPeachLight, color: tokens.colors.pastelPeachText };
}

export default function AdminScheduleDetailsPage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const query = useAdminSchedule(id);
  const schedule = query.data;
  const event = schedule?.event;

  return (
    <AppLayout
      activeTab="schedules"
      title="Schedule Inspection"
      subtitle={event?.eventTitle || 'Schedule details'}
    >
      <Button
        startIcon={<ArrowBackOutlinedIcon />}
        onClick={() => navigate('/admin/schedules')}
        sx={{ mb: 2, borderRadius: 9999, px: 2.5 }}
      >
        Back to schedules
      </Button>

      {query.isLoading && (
        <Box sx={{ display: 'grid', placeItems: 'center', minHeight: '60vh' }}>
          <CircularProgress />
        </Box>
      )}

      {query.isError && (
        <Alert severity="error" sx={{ mb: 3 }}>
          {query.error.message}
        </Alert>
      )}

      {schedule && event && (
        <Stack spacing={3}>
          <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', flexWrap: 'wrap', gap: 2 }}>
            <Box>
              <Typography variant="h3" sx={{ fontWeight: 800, letterSpacing: '-0.04em' }}>
                {event.eventTitle || 'Untitled event'}
              </Typography>
              <Typography color="text.secondary" sx={{ mt: 0.5 }}>
                Schedule ID: {schedule.scheduleId}
              </Typography>
            </Box>
            <StatusBadge status={event.eventStatus} label={event.eventStatus} />
          </Box>

          <Grid container spacing={2.5}>
            <Grid size={{ xs: 12, md: 8 }}>
              <SurfaceCard sx={{ p: 3 }}>
                <Typography variant="h5" sx={{ fontWeight: 800, mb: 2.5 }}>
                  Event Information
                </Typography>
                <Grid container spacing={2.5}>
                  <Grid size={{ xs: 12, sm: 6 }}><Detail label="Event title" value={event.eventTitle} /></Grid>
                  <Grid size={{ xs: 12, sm: 6 }}><Detail label="Event ID" value={event.eventId} /></Grid>
                  <Grid size={{ xs: 12, sm: 6 }}><Detail label="Date" value={formatDate(event.eventDate)} /></Grid>
                  <Grid size={{ xs: 12, sm: 6 }}><Detail label="Start / end time" value={`${formatTime(event.eventStartTime)} - ${formatTime(event.eventEndTime)}`} /></Grid>
                </Grid>
              </SurfaceCard>
            </Grid>

            <Grid size={{ xs: 12, md: 4 }}>
              <SurfaceCard variant="analytics" sx={{ p: 3, height: '100%' }}>
                <Typography variant="h6" sx={{ fontWeight: 800, mb: 2 }}>
                  Planner
                </Typography>
                <Stack spacing={2}>
                  <Detail label="Name" value={plannerName(event.planner)} />
                  <Detail label="Email" value={event.planner?.email} />
                  <Detail label="Planner ID" value={event.planner?.id} />
                </Stack>
              </SurfaceCard>
            </Grid>
          </Grid>

          <SurfaceCard sx={{ p: 3 }}>
            <Typography variant="h5" sx={{ fontWeight: 800, mb: 2 }}>
              Activities
            </Typography>
            <TableContainer sx={{ overflowX: 'auto' }}>
              <Table size="small">
                <TableHead>
                  <TableRow>
                    <TableCell>Activity</TableCell>
                    <TableCell>Start</TableCell>
                    <TableCell>End</TableCell>
                    <TableCell>Assigned vendor</TableCell>
                    <TableCell>Status</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {schedule.activities.length === 0 ? (
                    <TableRow>
                      <TableCell colSpan={5} align="center">
                        <Typography sx={{ py: 3 }} color="text.secondary">
                          No activities have been added to this schedule.
                        </Typography>
                      </TableCell>
                    </TableRow>
                  ) : (
                    schedule.activities.map((activity) => (
                      <TableRow hover key={activity.id}>
                        <TableCell>
                          <Typography sx={{ fontWeight: 700 }}>{activity.title}</Typography>
                          {activity.description && (
                            <Typography variant="body2" color="text.secondary">{activity.description}</Typography>
                          )}
                        </TableCell>
                        <TableCell>{formatWallClockTime(activity.startTime)}</TableCell>
                        <TableCell>{formatWallClockTime(activity.endTime)}</TableCell>
                        <TableCell>
                          {activity.assignedVendor ? (
                            <Box>
                              <Typography sx={{ fontWeight: 700 }}>{activity.assignedVendor.businessName}</Typography>
                              <Typography variant="body2" color="text.secondary">
                                {activity.assignedVendor.category} - {activity.assignedVendor.contactEmail}
                              </Typography>
                            </Box>
                          ) : '-'}
                        </TableCell>
                        <TableCell>
                          <StatusBadge status={activity.status} label={activity.status?.replaceAll('_', ' ')} />
                        </TableCell>
                      </TableRow>
                    ))
                  )}
                </TableBody>
              </Table>
            </TableContainer>
          </SurfaceCard>

          <SurfaceCard sx={{ p: 3 }}>
            <Stack direction={{ xs: 'column', sm: 'row' }} sx={{ justifyContent: 'space-between', alignItems: { sm: 'center' }, mb: 2 }} spacing={1}>
              <Typography variant="h5" sx={{ fontWeight: 800 }}>
                Conflicts
              </Typography>
              <Typography variant="body2" color="text.secondary">
                {schedule.unresolvedConflictCount} unresolved / {schedule.resolvedConflictCount} resolved
              </Typography>
            </Stack>
            <TableContainer sx={{ overflowX: 'auto' }}>
              <Table size="small">
                <TableHead>
                  <TableRow>
                    <TableCell>Type</TableCell>
                    <TableCell>Description</TableCell>
                    <TableCell>Detected</TableCell>
                    <TableCell>Status</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {schedule.conflicts.length === 0 ? (
                    <TableRow>
                      <TableCell colSpan={4} align="center">
                        <Typography sx={{ py: 3 }} color="text.secondary">
                          No schedule conflicts have been detected.
                        </Typography>
                      </TableCell>
                    </TableRow>
                  ) : (
                    schedule.conflicts.map((conflict) => (
                      <TableRow hover key={conflict.id}>
                        <TableCell>
                          <Chip
                            size="small"
                            label={conflictLabel(conflict.conflictType)}
                            sx={{ ...conflictSx(conflict.conflictType, conflict.isResolved), fontWeight: 800 }}
                          />
                        </TableCell>
                        <TableCell>{conflict.description}</TableCell>
                        <TableCell>{formatDateTime(conflict.detectedAt)}</TableCell>
                        <TableCell>{conflict.isResolved ? 'Resolved' : 'Unresolved'}</TableCell>
                      </TableRow>
                    ))
                  )}
                </TableBody>
              </Table>
            </TableContainer>
          </SurfaceCard>
        </Stack>
      )}
    </AppLayout>
  );
}
