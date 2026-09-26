import { useState } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import {
  Alert, Box, Button, Card, CardContent, CircularProgress, Stack, Typography,
} from '@mui/material';
import AutoAwesomeOutlinedIcon from '@mui/icons-material/AutoAwesomeOutlined';
import VisibilityOutlinedIcon from '@mui/icons-material/VisibilityOutlined';
import { useAuthStore } from '../../../shared/store/authStore';
import CollapsibleSidebar from '../../../shared/components/layout/CollapsibleSidebar';
import TopSearchNavbar from '../../../shared/components/layout/TopSearchNavbar';
import SurfaceCard from '../../../shared/components/ui/SurfaceCard';
import StatusBadge from '../../../shared/components/ui/StatusBadge';
import { generatePlan, statusLabel, unwrapItems, useEventPlans, useMyEvents } from '../api/ownerEventApi';

function EventPlanCard({ event }) {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [actionError, setActionError] = useState('');
  const plansQuery = useEventPlans(event.id);
  const plans = unwrapItems(plansQuery.data);
  const latestPlan = plans[0];
  const generateMutation = useMutation({
    mutationFn: () => generatePlan(event.id),
    onSuccess: async (plan) => {
      setActionError('');
      await queryClient.invalidateQueries({ queryKey: ['event-plans', event.id] });
      navigate(`/my-events/plans/${plan.id}`);
    },
    onError: (error) => setActionError(error.message),
  });

  return (
    <Card variant="outlined">
      <CardContent>
        <Stack spacing={1.5}>
          <Box>
            <Typography variant="h6" sx={{ fontWeight: 800 }}>
              {event.preferredVenue || 'Untitled event'}
            </Typography>
            <Typography color="text.secondary">
              {String(event.eventType || 'Event').replaceAll('_', ' ')} · {event.guestCount} guests · {new Date(event.preferredDate).toLocaleDateString()}
            </Typography>
          </Box>
          {plansQuery.isLoading && <CircularProgress size={22} />}
          {plansQuery.isError && <Alert severity="error">{plansQuery.error.message}</Alert>}
          {plansQuery.isSuccess && latestPlan && (
            <Box sx={{ display: 'flex', gap: 1, alignItems: 'center', flexWrap: 'wrap' }}>
              <StatusBadge status={statusLabel(latestPlan.status)} label={`Plan v${latestPlan.version} · ${statusLabel(latestPlan.status)}`} />
              <Button
                variant="outlined"
                startIcon={<VisibilityOutlinedIcon />}
                onClick={() => navigate(`/my-events/plans/${latestPlan.id}`)}
              >
                Review plan
              </Button>
            </Box>
          )}
          {plansQuery.isSuccess && !latestPlan && (
            <Button
              variant="contained"
              startIcon={generateMutation.isPending
                ? <CircularProgress size={18} color="inherit" />
                : <AutoAwesomeOutlinedIcon />}
              disabled={generateMutation.isPending}
              onClick={() => generateMutation.mutate()}
              sx={{ alignSelf: 'flex-start', borderRadius: 9999 }}
            >
              Generate AI plan
            </Button>
          )}
          {actionError && <Alert severity="error">{actionError}</Alert>}
        </Stack>
      </CardContent>
    </Card>
  );
}

export default function MyEventsPage() {
  const navigate = useNavigate();
  const user = useAuthStore((state) => state.user);
  const logout = useAuthStore((state) => state.logout);
  const isAdmin = user?.roles?.includes('ADMIN') || false;
  const eventsQuery = useMyEvents(!isAdmin);
  const events = eventsQuery.data?.items || [];

  return (
    <Box sx={{ display: 'flex', minHeight: '100vh', backgroundColor: '#F7F3E9', p: { xs: 1.5, md: 2.5 }, gap: { xs: 2, md: 3 } }}>
      <CollapsibleSidebar
        activeTab="dashboard"
        onSelectTab={() => navigate('/dashboard')}
        onLogout={() => { logout(); navigate('/login'); }}
      />
      <Box sx={{ flex: 1, minWidth: 0 }}>
        <TopSearchNavbar user={user} title="My events" subtitle="Owner workspace" />
        <Typography variant="h3" sx={{ fontWeight: 800, letterSpacing: '-0.04em', mb: 0.5 }}>
          Event plans
        </Typography>
        <Typography color="text.secondary" sx={{ mb: 2.5 }}>
          Generate and review coordinator plans for events you own.
        </Typography>
        {isAdmin && <Alert severity="warning" sx={{ mb: 2 }}>Administrators can monitor plans but cannot generate or decide on an event owner’s plan.</Alert>}
        {!isAdmin && eventsQuery.isLoading && <Box sx={{ display: 'grid', placeItems: 'center', py: 8 }}><CircularProgress /></Box>}
        {!isAdmin && eventsQuery.isError && <Alert severity="error">{eventsQuery.error.message}</Alert>}
        {!isAdmin && eventsQuery.isSuccess && events.length === 0 && (
          <SurfaceCard sx={{ p: 3 }}>
            <Typography>No events yet. Create an event to generate its AI plan.</Typography>
          </SurfaceCard>
        )}
        {!isAdmin && events.length > 0 && (
          <Stack spacing={2}>
            {events.map((event) => <EventPlanCard key={event.id} event={event} />)}
          </Stack>
        )}
      </Box>
    </Box>
  );
}
