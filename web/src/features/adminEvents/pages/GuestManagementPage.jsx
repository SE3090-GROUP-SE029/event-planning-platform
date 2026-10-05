import { useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useQueryClient, useMutation } from '@tanstack/react-query';
import {
  Alert, Box, Button, Chip, CircularProgress, FormControl, InputLabel, MenuItem, Select,
  Stack, Table, TableBody, TableCell, TableContainer, TableHead, TablePagination, TableRow, Typography, Avatar
} from '@mui/material';
import ArrowBackOutlinedIcon from '@mui/icons-material/ArrowBackOutlined';
import AppLayout from '../../../shared/components/layout/AppLayout';
import SurfaceCard from '../../../shared/components/ui/SurfaceCard';
import { reviewGuestRegistration, useEventGuests } from '../api/guestListApi';
import { useAdminEvent } from '../api/adminEventApi';
import { tokens } from '../../../shared/theme/tokens';

export default function GuestManagementPage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const eventQuery = useAdminEvent(id);
  const queryClient = useQueryClient();
  const reviewMutation = useMutation({
    mutationFn: ({ registrationId, decision }) => reviewGuestRegistration(id, registrationId, decision),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['planner-registrations', id] }),
  });

  const [page, setPage] = useState(0);
  const [pageSize, setPageSize] = useState(10);
  const [statusFilter, setStatusFilter] = useState('ALL');
  const [rsvpFilter, setRsvpFilter] = useState('ALL'); // ACCEPTED, DECLINED, MAYBE
  const [checkInFilter, setCheckInFilter] = useState('ALL'); // Checked-in, Not checked-in

  const guestsQuery = useEventGuests({
    eventId: id,
    page: page + 1,
    pageSize,
    status: statusFilter === 'ALL' ? undefined : statusFilter,
    isWaitlisted: statusFilter === 'WAITLISTED' ? true : undefined,
    rsvpStatus: rsvpFilter === 'ALL' ? undefined : rsvpFilter,
    checkedIn: checkInFilter === 'ALL' ? undefined : checkInFilter === 'CHECKED_IN',
  });

  const guests = guestsQuery.data?.items || [];
  const isLoading = guestsQuery.isLoading;
  const canReview = (guest) => ['PENDING_REVIEW', 'ACCEPTED', 'REJECTED'].includes(guest.status);
  const reviewActions = (guest) => canReview(guest) && (
    <Stack direction="row" spacing={1}>
      <Button size="small" disabled={reviewMutation.isPending || (guest.status === 'REJECTED' && guest.emailDeliveryStatus === 'SENT')}
        onClick={() => reviewMutation.mutate({ registrationId: guest.id, decision: 'ACCEPTED' })}>
        Accept
      </Button>
      <Button size="small" color="error" disabled={reviewMutation.isPending}
        onClick={() => reviewMutation.mutate({ registrationId: guest.id, decision: 'REJECTED' })}>
        Reject
      </Button>
    </Stack>
  );

  return (
    <AppLayout
      activeTab="events"
      title="Guest Management"
      subtitle={eventQuery.data ? `Manage guests for ${eventQuery.data.eventName}` : 'Manage Guests'}
    >
      <Stack direction="row" gap={2} sx={{ mb: 3, flexWrap: 'wrap' }}>
        <Button
          startIcon={<ArrowBackOutlinedIcon />}
          onClick={() => navigate(`/admin/events/${id}`)}
          sx={{ borderRadius: 9999, px: 2.5 }}
        >
          Back to Event Details
        </Button>
      </Stack>

      {/* Filter Surface */}
      <SurfaceCard sx={{ p: 2.5, mb: 3 }}>
        <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} sx={{ alignItems: 'center' }}>
          <FormControl size="small" sx={{ minWidth: 200, flex: 1 }}>
            <InputLabel>Status</InputLabel>
            <Select
              value={statusFilter}
              label="Status"
              onChange={(e) => { setPage(0); setStatusFilter(e.target.value); }}
            >
              <MenuItem value="ALL">All Statuses</MenuItem>
              <MenuItem value="PENDING_REVIEW">Pending review</MenuItem>
              <MenuItem value="ACCEPTED">Accepted (awaiting allocation)</MenuItem>
              <MenuItem value="CONFIRMED">Confirmed</MenuItem>
              <MenuItem value="REJECTED">Rejected</MenuItem>
              <MenuItem value="WAITLISTED">Waitlisted</MenuItem>
            </Select>
          </FormControl>

          <FormControl size="small" sx={{ minWidth: 200, flex: 1 }}>
            <InputLabel>RSVP Status</InputLabel>
            <Select
              value={rsvpFilter}
              label="RSVP Status"
              onChange={(e) => { setPage(0); setRsvpFilter(e.target.value); }}
            >
              <MenuItem value="ALL">All RSVP</MenuItem>
              <MenuItem value="ACCEPTED">Accepted</MenuItem>
              <MenuItem value="DECLINED">Declined</MenuItem>
              <MenuItem value="MAYBE">Maybe</MenuItem>
            </Select>
          </FormControl>

          <FormControl size="small" sx={{ minWidth: 200, flex: 1 }}>
            <InputLabel>Check-In Status</InputLabel>
            <Select
              value={checkInFilter}
              label="Check-In Status"
              onChange={(e) => { setPage(0); setCheckInFilter(e.target.value); }}
            >
              <MenuItem value="ALL">All Check-Ins</MenuItem>
              <MenuItem value="CHECKED_IN">Checked-in</MenuItem>
              <MenuItem value="NOT_CHECKED_IN">Not checked-in</MenuItem>
            </Select>
          </FormControl>

          <Button
            variant="text"
            onClick={() => {
              setStatusFilter('ALL');
              setRsvpFilter('ALL');
              setCheckInFilter('ALL');
              setPage(0);
            }}
          >
            Clear Filters
          </Button>
        </Stack>
      </SurfaceCard>

      {/* Guests Table */}
      <SurfaceCard sx={{ p: 3 }}>
        {reviewMutation.isError && (
          <Alert severity="error" sx={{ mb: 2 }}>
            The review decision could not be saved. {reviewMutation.error.message}
          </Alert>
        )}
        {isLoading ? (
          <Box sx={{ display: 'flex', justifyContent: 'center', py: 6 }}>
            <CircularProgress />
          </Box>
        ) : guestsQuery.isError ? (
          <Alert severity="error">
            Guests could not be loaded. {guestsQuery.error.message}
          </Alert>
        ) : guests.length === 0 ? (
          <Box sx={{ textAlign: 'center', py: 6 }}>
            <Typography color="text.secondary">No guests found matching your filters.</Typography>
          </Box>
        ) : (
          <>
            {/* Desktop Table View */}
            <TableContainer sx={{ display: { xs: 'none', md: 'block' } }}>
              <Table size="small">
                <TableHead>
                  <TableRow>
                    <TableCell>Guest</TableCell>
                    <TableCell>Registration Status</TableCell>
                    <TableCell>RSVP</TableCell>
                    <TableCell>Check-In</TableCell>
                    <TableCell>Registered At</TableCell>
                    <TableCell>Review</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {guests.map((g) => {
                    const statusLabel = g.status.replace('_', ' ');
                    let rsvpLabel = g.rsvpStatus || 'PENDING';
                    let checkInLabel = g.checkedInAt ? 'CHECKED IN' : 'NOT CHECKED IN';
                    
                    return (
                      <TableRow hover key={g.id}>
                        <TableCell>
                          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5 }}>
                            <Avatar sx={{ width: 36, height: 36, bgcolor: tokens.colors.pastelBlue, color: tokens.colors.pastelBlueText, fontWeight: 800 }}>
                              {g.fullName.charAt(0).toUpperCase()}
                            </Avatar>
                            <Box>
                              <Typography sx={{ fontWeight: 700 }}>{g.fullName}</Typography>
                              <Typography variant="body2" color="text.secondary">{g.emailAddress}</Typography>
                            </Box>
                          </Box>
                        </TableCell>
                        <TableCell>
                          <Chip
                            size="small"
                            label={statusLabel}
                            sx={{ fontWeight: 700, fontSize: '0.72rem' }}
                          />
                        </TableCell>
                        <TableCell>
                          <Chip
                            size="small"
                            label={rsvpLabel}
                            sx={{ fontWeight: 700, fontSize: '0.72rem' }}
                          />
                        </TableCell>
                        <TableCell>
                          <Chip
                            size="small"
                            label={checkInLabel}
                            sx={{ fontWeight: 700, fontSize: '0.72rem' }}
                          />
                        </TableCell>
                        <TableCell>{new Date(g.registeredAt).toLocaleDateString()}</TableCell>
                        <TableCell>{reviewActions(g)}</TableCell>
                      </TableRow>
                    );
                  })}
                </TableBody>
              </Table>
            </TableContainer>

            {/* Mobile Card View */}
            <Stack spacing={2} sx={{ display: { xs: 'flex', md: 'none' } }}>
              {guests.map((g) => {
                const statusLabel = g.status.replace('_', ' ');
                let rsvpLabel = g.rsvpStatus || 'PENDING';
                let checkInLabel = g.checkedInAt ? 'CHECKED IN' : 'NOT CHECKED IN';
                
                return (
                  <Box key={g.id} sx={{ p: 2, border: '1px solid', borderColor: 'divider', borderRadius: 2 }}>
                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5, mb: 2 }}>
                      <Avatar sx={{ width: 36, height: 36, bgcolor: tokens.colors.pastelBlue, color: tokens.colors.pastelBlueText, fontWeight: 800 }}>
                        {g.fullName.charAt(0).toUpperCase()}
                      </Avatar>
                      <Box>
                        <Typography sx={{ fontWeight: 700 }}>{g.fullName}</Typography>
                        <Typography variant="body2" color="text.secondary">{g.emailAddress}</Typography>
                      </Box>
                    </Box>
                    
                    <Stack spacing={1}>
                      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                        <Typography variant="body2" color="text.secondary">Status</Typography>
                        <Chip size="small" label={statusLabel} sx={{ fontWeight: 700, fontSize: '0.72rem' }} />
                      </Box>
                      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                        <Typography variant="body2" color="text.secondary">RSVP</Typography>
                        <Chip size="small" label={rsvpLabel} sx={{ fontWeight: 700, fontSize: '0.72rem' }} />
                      </Box>
                      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                        <Typography variant="body2" color="text.secondary">Check-In</Typography>
                        <Chip size="small" label={checkInLabel} sx={{ fontWeight: 700, fontSize: '0.72rem' }} />
                      </Box>
                      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mt: 1 }}>
                        <Typography variant="body2" color="text.secondary">Registered</Typography>
                        <Typography variant="body2" sx={{ fontWeight: 500 }}>{new Date(g.registeredAt).toLocaleDateString()}</Typography>
                      </Box>
                      {reviewActions(g)}
                    </Stack>
                  </Box>
                );
              })}
            </Stack>
          </>
        )}
        {!isLoading && !guestsQuery.isError && (
          <TablePagination
            component="div"
            count={guestsQuery.data?.total ?? guests.length}
            page={page}
            rowsPerPage={pageSize}
            onPageChange={(_, nextPage) => setPage(nextPage)}
            onRowsPerPageChange={(event) => {
              setPage(0);
              setPageSize(Number(event.target.value));
            }}
            rowsPerPageOptions={[10, 25, 50, 100]}
          />
        )}
      </SurfaceCard>
    </AppLayout>
  );
}
