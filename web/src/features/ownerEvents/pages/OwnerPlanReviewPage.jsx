import { useState } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useNavigate, useParams } from 'react-router-dom';
import {
  Alert,
  Box,
  Button,
  Chip,
  CircularProgress,
  Grid,
  LinearProgress,
  MenuItem,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import ArrowBackOutlinedIcon from '@mui/icons-material/ArrowBackOutlined';
import AutoAwesomeOutlinedIcon from '@mui/icons-material/AutoAwesomeOutlined';
import CheckCircleOutlineRoundedIcon from '@mui/icons-material/CheckCircleOutlineRounded';
import AccessTimeRoundedIcon from '@mui/icons-material/AccessTimeRounded';
import AttachMoneyRoundedIcon from '@mui/icons-material/AttachMoneyRounded';
import WarningAmberRoundedIcon from '@mui/icons-material/WarningAmberRounded';
import AppLayout from '../../../shared/components/layout/AppLayout';
import SurfaceCard from '../../../shared/components/ui/SurfaceCard';
import StatusBadge from '../../../shared/components/ui/StatusBadge';
import { approvePlan, generatePlan, rejectPlan, statusLabel, usePlan } from '../api/ownerEventApi';

export default function OwnerPlanReviewPage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const query = usePlan(id);
  const plan = query.data;

  const [remarks, setRemarks] = useState('');
  const [severity, setSeverity] = useState('Moderate');
  const [regenerationReason, setRegenerationReason] = useState('');
  const [actionError, setActionError] = useState('');

  const refresh = async (updatedPlan) => {
    setActionError('');
    await queryClient.invalidateQueries({ queryKey: ['owner-plan', id] });
    await queryClient.invalidateQueries({ queryKey: ['event-plans', plan?.eventId] });
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
    <AppLayout
      activeTab="events"
      title="Plan Review"
      subtitle={plan?.eventSnapshot?.eventName || 'AI Coordinator Plan'}
    >
      <Button
        startIcon={<ArrowBackOutlinedIcon />}
        onClick={() => navigate('/my-events')}
        sx={{ mb: 2, borderRadius: 9999, px: 2.5 }}
      >
        Back to my events
      </Button>

      {query.isLoading && (
        <Box sx={{ display: 'grid', placeItems: 'center', py: 8 }}>
          <CircularProgress />
        </Box>
      )}

      {query.isError && <Alert severity="error" sx={{ mb: 3 }}>{query.error.message}</Alert>}

      {plan && (
        <Stack spacing={3}>
          {/* Header Title Section */}
          <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', flexWrap: 'wrap', gap: 2 }}>
            <Box>
              <Typography variant="h3" sx={{ fontWeight: 800, letterSpacing: '-0.04em' }}>
                {plan.eventSnapshot?.eventName || 'Event Plan Draft'}
              </Typography>
              <Typography color="text.secondary" sx={{ mt: 0.5 }}>
                Version {plan.version} · {plan.eventSnapshot?.guestCount || 0} Expected Guests · Budget: ${Number(plan.eventSnapshot?.budget || 0).toLocaleString()}
              </Typography>
            </Box>
            <StatusBadge status={statusLabel(plan.status)} label={statusLabel(plan.status)} />
          </Box>

          {/* 1. Recommendations & Overview Card (pastelBlueLight) */}
          <SurfaceCard variant="analytics" sx={{ p: 3.5 }}>
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5, mb: 2 }}>
              <Box sx={{ p: 1, borderRadius: 2, bgcolor: '#BCD8F0', color: '#153251' }}>
                <AutoAwesomeOutlinedIcon />
              </Box>
              <Typography variant="h5" sx={{ fontWeight: 800 }}>
                AI Recommendations &amp; Executive Summary
              </Typography>
            </Box>
            <Typography sx={{ lineHeight: 1.7, mb: 2 }}>
              {plan.rationale || 'No automated rationale provided for this version.'}
            </Typography>
            <Box sx={{ maxWidth: 420 }}>
              <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 0.5 }}>
                <Typography variant="caption" sx={{ fontWeight: 700, color: '#153251' }}>
                  Plan Completeness Score
                </Typography>
                <Typography variant="caption" sx={{ fontWeight: 800 }}>
                  {plan.planCompletenessScore ?? 0}%
                </Typography>
              </Box>
              <LinearProgress
                variant="determinate"
                value={plan.planCompletenessScore ?? 0}
                sx={{ height: 8, borderRadius: 4, bgcolor: 'rgba(255,255,255,0.6)', '& .MuiLinearProgress-bar': { bgcolor: '#153251' } }}
              />
            </Box>
          </SurfaceCard>

          <Grid container spacing={3}>
            {/* 2. Timeline Card (pastelLavenderLight) */}
            <Grid size={{ xs: 12, md: 6 }}>
              <SurfaceCard variant="aiPlan" sx={{ p: 3.5, height: '100%' }}>
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5, mb: 2.5 }}>
                  <Box sx={{ p: 1, borderRadius: 2, bgcolor: '#CAADEA', color: '#391652' }}>
                    <AccessTimeRoundedIcon />
                  </Box>
                  <Typography variant="h5" sx={{ fontWeight: 800 }}>
                    Proposed Timeline
                  </Typography>
                </Box>
                {Object.keys(plan.proposedTimeline || {}).length === 0 ? (
                  <Typography color="text.secondary">No timeline phases generated.</Typography>
                ) : (
                  <Stack spacing={2}>
                    {Object.entries(plan.proposedTimeline || {}).map(([phase, description]) => (
                      <Box key={phase} sx={{ p: 2, bgcolor: '#FFFFFF', borderRadius: 2.5, border: '1px solid rgba(0,0,0,0.06)' }}>
                        <Typography sx={{ fontWeight: 700, color: '#391652', mb: 0.5 }}>
                          {phase}
                        </Typography>
                        <Typography variant="body2" color="text.secondary" sx={{ lineHeight: 1.6 }}>
                          {description}
                        </Typography>
                      </Box>
                    ))}
                  </Stack>
                )}
              </SurfaceCard>
            </Grid>

            {/* 3. Tasks & Required Services Card (pastelGreenLight) */}
            <Grid size={{ xs: 12, md: 6 }}>
              <SurfaceCard variant="task" sx={{ p: 3.5, height: '100%' }}>
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5, mb: 2.5 }}>
                  <Box sx={{ p: 1, borderRadius: 2, bgcolor: '#C4DDB8', color: '#20361A' }}>
                    <CheckCircleOutlineRoundedIcon />
                  </Box>
                  <Typography variant="h5" sx={{ fontWeight: 800 }}>
                    Service Tasks &amp; Requirements
                  </Typography>
                </Box>
                <Box sx={{ mb: 2.5 }}>
                  <Typography variant="caption" sx={{ fontWeight: 700, color: '#20361A', display: 'block', mb: 1 }}>
                    Required Service Categories
                  </Typography>
                  <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1 }}>
                    {(plan.serviceCategories || []).map((cat) => (
                      <Chip key={cat} label={cat} sx={{ bgcolor: '#FFFFFF', color: '#20361A', fontWeight: 700 }} />
                    ))}
                    {!plan.serviceCategories?.length && (
                      <Typography variant="body2" color="text.secondary">No categories mapped.</Typography>
                    )}
                  </Box>
                </Box>

                <Typography variant="caption" sx={{ fontWeight: 700, color: '#20361A', display: 'block', mb: 1 }}>
                  Missing Requirements Checklist
                </Typography>
                <Stack spacing={1.5}>
                  {(plan.missingRequirements || []).map((item) => (
                    <Box key={item.requirement} sx={{ p: 1.5, bgcolor: '#FFFFFF', borderRadius: 2, border: '1px solid rgba(0,0,0,0.06)' }}>
                      <Typography sx={{ fontWeight: 700, fontSize: '0.9rem' }}>{item.requirement}</Typography>
                      <Typography variant="caption" color="text.secondary">{item.reason}</Typography>
                    </Box>
                  ))}
                  {!plan.missingRequirements?.length && (
                    <Typography variant="body2" color="text.secondary">All critical requirements fulfilled.</Typography>
                  )}
                </Stack>
              </SurfaceCard>
            </Grid>

            {/* 4. Budget Allocation Card (pastelPeachLight) */}
            <Grid size={{ xs: 12, md: 6 }}>
              <SurfaceCard variant="budget" sx={{ p: 3.5, height: '100%' }}>
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5, mb: 2.5 }}>
                  <Box sx={{ p: 1, borderRadius: 2, bgcolor: '#FCD8C1', color: '#4D260D' }}>
                    <AttachMoneyRoundedIcon />
                  </Box>
                  <Typography variant="h5" sx={{ fontWeight: 800 }}>
                    Budget Allocation
                  </Typography>
                </Box>
                {Object.keys(plan.budgetAllocation || {}).length === 0 ? (
                  <Typography color="text.secondary">No budget items allocated.</Typography>
                ) : (
                  <Stack spacing={1.5}>
                    {Object.entries(plan.budgetAllocation || {}).map(([category, amount]) => (
                      <Box key={category} sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', p: 1.5, bgcolor: '#FFFFFF', borderRadius: 2, border: '1px solid rgba(0,0,0,0.06)' }}>
                        <Typography sx={{ fontWeight: 600 }}>{category}</Typography>
                        <Typography sx={{ fontWeight: 800, color: '#4D260D' }}>
                          ${Number(amount).toLocaleString()}
                        </Typography>
                      </Box>
                    ))}
                  </Stack>
                )}
              </SurfaceCard>
            </Grid>

            {/* 5. Vendor Suggestions & Identified Risks Card (pastelPinkLight) */}
            <Grid size={{ xs: 12, md: 6 }}>
              <SurfaceCard variant="vendor" sx={{ p: 3.5, height: '100%' }}>
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5, mb: 2.5 }}>
                  <Box sx={{ p: 1, borderRadius: 2, bgcolor: '#F9BFD8', color: '#481931' }}>
                    <WarningAmberRoundedIcon />
                  </Box>
                  <Typography variant="h5" sx={{ fontWeight: 800 }}>
                    Risks &amp; Vendor Guidance
                  </Typography>
                </Box>
                <Stack spacing={1.5}>
                  {(plan.identifiedRisks || []).map((risk) => {
                    const isCrit = String(risk.severity).toLowerCase() === 'critical';
                    return (
                      <Box key={risk.risk} sx={{ p: 2, bgcolor: '#FFFFFF', borderRadius: 2, border: '1px solid rgba(0,0,0,0.06)' }}>
                        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 0.5 }}>
                          <Typography sx={{ fontWeight: 700 }}>{risk.risk}</Typography>
                          <Chip
                            size="small"
                            label={risk.severity}
                            sx={{ bgcolor: isCrit ? '#FDEEF5' : '#FEF8E4', color: isCrit ? '#481931' : '#3E340B', fontWeight: 800, fontSize: '0.7rem' }}
                          />
                        </Box>
                        <Typography variant="body2" color="text.secondary">
                          {risk.recommendation}
                        </Typography>
                      </Box>
                    );
                  })}
                  {!plan.identifiedRisks?.length && (
                    <Typography color="text.secondary">No operational risks identified.</Typography>
                  )}
                </Stack>
              </SurfaceCard>
            </Grid>
          </Grid>

          {/* Action & Planner Decision Card */}
          <SurfaceCard sx={{ p: 3.5 }}>
            <Typography variant="h5" sx={{ fontWeight: 800, mb: 1.5 }}>
              Planner Decision &amp; Feedback
            </Typography>

            {actionError && <Alert severity="error" sx={{ mb: 2.5 }}>{actionError}</Alert>}

            {statusLabel(plan.status) === 'PENDING PLANNER REVIEW' && (
              <Stack spacing={2.5}>
                <Typography variant="body2" color="text.secondary">
                  Review the recommendations above. You may approve the draft to lock in vendor requirements or reject it with notes to trigger revision.
                </Typography>
                <Box sx={{ display: 'flex', gap: 2, flexWrap: 'wrap' }}>
                  <Button
                    variant="contained"
                    size="large"
                    disabled={isPending}
                    onClick={() => approveMutation.mutate()}
                    sx={{ borderRadius: 9999, px: 4, bgcolor: '#19191C', color: '#FFFFFF' }}
                  >
                    {approveMutation.isPending ? 'Approving…' : 'Approve Plan'}
                  </Button>
                </Box>

                <Box sx={{ pt: 2, borderTop: '1px solid rgba(0,0,0,0.06)' }}>
                  <Typography variant="subtitle2" sx={{ fontWeight: 700, mb: 1 }}>
                    Or Reject with Guidance:
                  </Typography>
                  <Stack spacing={2}>
                    <TextField
                      fullWidth
                      label="Rejection remarks (minimum 20 characters)"
                      value={remarks}
                      onChange={(event) => setRemarks(event.target.value)}
                      multiline
                      minRows={3}
                      inputProps={{ maxLength: 1000 }}
                    />
                    <Box sx={{ display: 'flex', gap: 2, alignItems: 'center', flexWrap: 'wrap' }}>
                      <TextField
                        select
                        size="small"
                        label="Severity"
                        value={severity}
                        onChange={(event) => setSeverity(event.target.value)}
                        sx={{ minWidth: 160 }}
                      >
                        {['Minor', 'Moderate', 'Critical'].map((val) => (
                          <MenuItem key={val} value={val}>{val}</MenuItem>
                        ))}
                      </TextField>
                      <Button
                        color="error"
                        variant="outlined"
                        disabled={isPending || remarks.trim().length < 20}
                        onClick={() => rejectMutation.mutate()}
                        sx={{ borderRadius: 9999, px: 3 }}
                      >
                        Reject Plan
                      </Button>
                    </Box>
                  </Stack>
                </Box>
              </Stack>
            )}

            {statusLabel(plan.status) === 'REJECTED' && (
              <Stack spacing={2}>
                <Alert severity="info">This plan was rejected. Enter feedback below to generate a revised version.</Alert>
                <TextField
                  fullWidth
                  label="Reason for regeneration"
                  value={regenerationReason}
                  onChange={(event) => setRegenerationReason(event.target.value)}
                  multiline
                  minRows={2}
                  inputProps={{ maxLength: 500 }}
                />
                <Button
                  variant="contained"
                  disabled={isPending || !regenerationReason.trim()}
                  onClick={() => regenerateMutation.mutate()}
                  sx={{ alignSelf: 'flex-start', borderRadius: 9999, px: 3, bgcolor: '#19191C', color: '#FFFFFF' }}
                >
                  Regenerate Plan
                </Button>
              </Stack>
            )}

            {statusLabel(plan.status) === 'APPROVED' && (
              <Alert severity="success" sx={{ borderRadius: 2.5 }}>
                Plan v{plan.version} has been approved by the event coordinator. You can now request vendor quotes or schedule timeline activities.
              </Alert>
            )}
          </SurfaceCard>
        </Stack>
      )}
    </AppLayout>
  );
}
