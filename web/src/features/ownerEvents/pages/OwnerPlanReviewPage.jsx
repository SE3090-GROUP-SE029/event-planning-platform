import { useState } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useNavigate, useParams } from 'react-router-dom';
import {
  Alert, Box, Button, CircularProgress, Divider, MenuItem, Stack, TextField, Typography,
} from '@mui/material';
import ArrowBackOutlinedIcon from '@mui/icons-material/ArrowBackOutlined';
import { useAuthStore } from '../../../shared/store/authStore';
import CollapsibleSidebar from '../../../shared/components/layout/CollapsibleSidebar';
import SurfaceCard from '../../../shared/components/ui/SurfaceCard';
import StatusBadge from '../../../shared/components/ui/StatusBadge';
import { approvePlan, generatePlan, rejectPlan, statusLabel, usePlan } from '../api/ownerEventApi';

export default function OwnerPlanReviewPage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const user = useAuthStore((state) => state.user);
  const logout = useAuthStore((state) => state.logout);
  const isAdmin = user?.roles?.includes('ADMIN') || false;
  const query = usePlan(id);
  const plan = query.data;
  const [remarks, setRemarks] = useState('');
  const [severity, setSeverity] = useState('Moderate');
  const [regenerationReason, setRegenerationReason] = useState('');
  const [actionError, setActionError] = useState('');
  const refresh = async (updatedPlan) => {
    setActionError('');
    await queryClient.invalidateQueries({ queryKey: ['owner-plan', id] });
    await queryClient.invalidateQueries({ queryKey: ['event-plans', plan.eventId] });
    return updatedPlan;
  };
  const approveMutation = useMutation({
    mutationFn: () => approvePlan(id),
    onSuccess: refresh,
    onError: (error) => setActionError(error.message),
  });
  const rejectMutation = useMutation({
    mutationFn: () => rejectPlan(id, remarks.trim(), severity),
    onSuccess: refresh,
    onError: (error) => setActionError(error.message),
  });
  const regenerateMutation = useMutation({
    mutationFn: () => generatePlan(plan.eventId, regenerationReason.trim()),
    onSuccess: async (newPlan) => {
      await refresh(newPlan);
      navigate(`/my-events/plans/${newPlan.id}`, { replace: true });
    },
    onError: (error) => setActionError(error.message),
  });
  const isPending = approveMutation.isPending || rejectMutation.isPending || regenerateMutation.isPending;

  return (
    <Box sx={{ display: 'flex', minHeight: '100vh', backgroundColor: '#F7F3E9', p: { xs: 1.5, md: 2.5 }, gap: { xs: 2, md: 3 } }}>
      <CollapsibleSidebar
        activeTab="dashboard"
        onSelectTab={() => navigate('/dashboard')}
        onLogout={() => { logout(); navigate('/login'); }}
      />
      <Box sx={{ flex: 1, minWidth: 0 }}>
        <Button startIcon={<ArrowBackOutlinedIcon />} onClick={() => navigate('/my-events')} sx={{ mb: 1 }}>
          Back to my events
        </Button>
        {query.isLoading && <Box sx={{ display: 'grid', placeItems: 'center', py: 8 }}><CircularProgress /></Box>}
        {query.isError && <Alert severity="error">{query.error.message}</Alert>}
        {isAdmin && <Alert severity="warning">Administrators can monitor plans but cannot review or regenerate them.</Alert>}
        {plan && (
          <Stack spacing={2.5}>
            <Box>
              <Typography variant="h3" sx={{ fontWeight: 800, letterSpacing: '-0.04em' }}>
                {plan.eventSnapshot?.eventName || 'Event plan'}
              </Typography>
              <Typography color="text.secondary">
                Version {plan.version} · {plan.eventSnapshot?.guestCount || 0} guests · Budget {Number(plan.eventSnapshot?.budget || 0).toLocaleString()}
              </Typography>
              <StatusBadge sx={{ mt: 1 }} status={statusLabel(plan.status)} label={statusLabel(plan.status)} />
            </Box>
            <SurfaceCard sx={{ p: 3 }}>
              <Stack spacing={2}>
                <Box>
                  <Typography variant="h6" sx={{ fontWeight: 800 }}>Summary</Typography>
                  <Typography sx={{ mt: 1 }}>{plan.rationale || 'No rationale provided.'}</Typography>
                  <Typography color="text.secondary" sx={{ mt: 1 }}>
                    Completeness: {plan.planCompletenessScore ?? 0}%
                  </Typography>
                </Box>
                <Divider />
                <Box>
                  <Typography variant="h6" sx={{ fontWeight: 800 }}>Service categories</Typography>
                  <Typography>{(plan.serviceCategories || []).join(', ') || '—'}</Typography>
                </Box>
                <Box>
                  <Typography variant="h6" sx={{ fontWeight: 800 }}>Budget</Typography>
                  {Object.entries(plan.budgetAllocation || {}).map(([category, amount]) => (
                    <Box key={category} sx={{ display: 'flex', justifyContent: 'space-between' }}>
                      <Typography>{category}</Typography>
                      <Typography>{Number(amount).toLocaleString()}</Typography>
                    </Box>
                  ))}
                </Box>
                <Box>
                  <Typography variant="h6" sx={{ fontWeight: 800 }}>Timeline</Typography>
                  {Object.entries(plan.proposedTimeline || {}).map(([phase, description]) => (
                    <Box key={phase} sx={{ mb: 1 }}>
                      <Typography fontWeight={700}>{phase}</Typography>
                      <Typography color="text.secondary">{description}</Typography>
                    </Box>
                  ))}
                </Box>
                <Box>
                  <Typography variant="h6" sx={{ fontWeight: 800 }}>Risks</Typography>
                  {(plan.identifiedRisks || []).map((risk) => (
                    <Box key={risk.risk} sx={{ mb: 1 }}>
                      <Typography fontWeight={700}>{risk.risk} · {risk.severity}</Typography>
                      <Typography color="text.secondary">{risk.recommendation}</Typography>
                    </Box>
                  ))}
                  {!plan.identifiedRisks?.length && <Typography color="text.secondary">No risks identified.</Typography>}
                </Box>
                <Box>
                  <Typography variant="h6" sx={{ fontWeight: 800 }}>Missing requirements</Typography>
                  {(plan.missingRequirements || []).map((item) => (
                    <Box key={item.requirement} sx={{ mb: 1 }}>
                      <Typography fontWeight={700}>{item.requirement}</Typography>
                      <Typography color="text.secondary">{item.reason}</Typography>
                    </Box>
                  ))}
                  {!plan.missingRequirements?.length && <Typography color="text.secondary">None identified.</Typography>}
                </Box>
                {plan.plannerRemarks && <Alert severity="info">Previous review: {plan.plannerRemarks}</Alert>}
                {actionError && <Alert severity="error">{actionError}</Alert>}
                {!isAdmin && statusLabel(plan.status) === 'PENDING PLANNER REVIEW' && (
                  <Stack spacing={1.5}>
                    <Button
                      variant="contained"
                      disabled={isPending}
                      onClick={() => approveMutation.mutate()}
                    >
                      Approve plan
                    </Button>
                    <TextField
                      label="Rejection remarks (minimum 20 characters)"
                      value={remarks}
                      onChange={(event) => setRemarks(event.target.value)}
                      multiline
                      minRows={3}
                      inputProps={{ maxLength: 1000 }}
                    />
                    <TextField
                      select
                      label="Rejection severity"
                      value={severity}
                      onChange={(event) => setSeverity(event.target.value)}
                    >
                      {['Minor', 'Moderate', 'Critical'].map((value) => (
                        <MenuItem key={value} value={value}>{value}</MenuItem>
                      ))}
                    </TextField>
                    <Button
                      color="error"
                      variant="outlined"
                      disabled={isPending || remarks.trim().length < 20}
                      onClick={() => rejectMutation.mutate()}
                    >
                      Reject plan
                    </Button>
                  </Stack>
                )}
                {!isAdmin && statusLabel(plan.status) === 'REJECTED' && (
                  <Stack spacing={1.5}>
                    <TextField
                      label="Reason for regeneration"
                      value={regenerationReason}
                      onChange={(event) => setRegenerationReason(event.target.value)}
                      inputProps={{ maxLength: 500 }}
                    />
                    <Button
                      variant="contained"
                      disabled={isPending || !regenerationReason.trim()}
                      onClick={() => regenerateMutation.mutate()}
                    >
                      Regenerate plan
                    </Button>
                  </Stack>
                )}
              </Stack>
            </SurfaceCard>
          </Stack>
        )}
      </Box>
    </Box>
  );
}
