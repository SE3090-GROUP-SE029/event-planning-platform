import { useNavigate, useParams } from 'react-router-dom';
import {
  Alert,
  Box,
  Button,
  Chip,
  CircularProgress,
  Grid,
  LinearProgress,
  Stack,
  Typography,
} from '@mui/material';
import ArrowBackOutlinedIcon from '@mui/icons-material/ArrowBackOutlined';
import AutoAwesomeOutlinedIcon from '@mui/icons-material/AutoAwesomeOutlined';
import AccessTimeRoundedIcon from '@mui/icons-material/AccessTimeRounded';
import AttachMoneyRoundedIcon from '@mui/icons-material/AttachMoneyRounded';
import CheckCircleOutlineRoundedIcon from '@mui/icons-material/CheckCircleOutlineRounded';
import WarningAmberRoundedIcon from '@mui/icons-material/WarningAmberRounded';
import AppLayout from '../../../shared/components/layout/AppLayout';
import SurfaceCard from '../../../shared/components/ui/SurfaceCard';
import StatusBadge from '../../../shared/components/ui/StatusBadge';
import { planStatusLabel, useAdminPlan } from '../api/adminPlanApi';
import { tokens } from '../../../shared/theme/tokens';

export default function AdminPlanDetailsPage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const query = useAdminPlan(id);
  const plan = query.data?.data || query.data;
  const name = plan?.eventSnapshot?.eventName || 'Plan Details';

  return (
    <AppLayout
      activeTab="plans"
      title="Plan Inspection"
      subtitle={name}
    >
      <Button
        startIcon={<ArrowBackOutlinedIcon />}
        onClick={() => navigate('/admin/plans')}
        sx={{ mb: 2, borderRadius: 9999, px: 2.5 }}
      >
        Back to plans
      </Button>

      {query.isLoading && (
        <Box sx={{ display: 'flex', justifyContent: 'center', py: 8 }}>
          <CircularProgress />
        </Box>
      )}

      {query.isError && (
        <Alert severity="error" sx={{ mb: 3 }}>
          {query.error.message}
        </Alert>
      )}

      {plan && (
        <Stack spacing={3}>
          {/* Header Title */}
          <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', flexWrap: 'wrap', gap: 2 }}>
            <Box>
              <Typography variant="h3" sx={{ fontWeight: 800, letterSpacing: '-0.04em' }}>
                {name}
              </Typography>
              <Typography color="text.secondary" sx={{ mt: 0.5 }}>
                Version {plan.version || 1} · Generated {plan.generatedAt ? new Date(plan.generatedAt).toLocaleString() : '—'}
              </Typography>
            </Box>
            <StatusBadge status={planStatusLabel(plan.status)} label={planStatusLabel(plan.status)} />
          </Box>

          {/* 1. Recommendations Overview (pastelBlueLight) */}
          <SurfaceCard variant="analytics" sx={{ p: 3.5 }}>
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5, mb: 1.5 }}>
              <Box sx={{ p: 1, borderRadius: 2, bgcolor: tokens.colors.pastelBlue, color: tokens.colors.pastelBlueText }}>
                <AutoAwesomeOutlinedIcon />
              </Box>
              <Typography variant="h5" sx={{ fontWeight: 800 }}>
                AI Recommendations &amp; Completeness
              </Typography>
            </Box>
            <Typography sx={{ lineHeight: 1.7, mb: 2.5 }}>
              {plan.rationale || 'No automated rationale generated for this plan.'}
            </Typography>
            <Box sx={{ maxWidth: 400 }}>
              <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 0.5 }}>
                <Typography variant="caption" sx={{ fontWeight: 700, color: tokens.colors.pastelBlueText }}>
                  Completeness Score
                </Typography>
                <Typography variant="caption" sx={{ fontWeight: 800 }}>
                  {plan.planCompletenessScore ?? plan.completenessScore ?? 0}%
                </Typography>
              </Box>
              <LinearProgress
                variant="determinate"
                value={plan.planCompletenessScore ?? plan.completenessScore ?? 0}
                sx={{ height: 8, borderRadius: 4, bgcolor: tokens.colors.surfaceMuted, '& .MuiLinearProgress-bar': { bgcolor: tokens.colors.pastelBlueText } }}
              />
            </Box>
          </SurfaceCard>

          {/* Sectional Pastel Cards Grid */}
          <Grid container spacing={3}>
            {/* 2. Timeline (pastelLavenderLight) */}
            <Grid size={{ xs: 12, md: 6 }}>
              <SurfaceCard variant="aiPlan" sx={{ p: 3.5, height: '100%' }}>
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5, mb: 2.5 }}>
                  <Box sx={{ p: 1, borderRadius: 2, bgcolor: tokens.colors.pastelLavender, color: tokens.colors.pastelLavenderText }}>
                    <AccessTimeRoundedIcon />
                  </Box>
                  <Typography variant="h5" sx={{ fontWeight: 800 }}>
                    Proposed Timeline
                  </Typography>
                </Box>
                {Object.keys(plan.proposedTimeline || {}).length === 0 ? (
                  <Typography color="text.secondary">No timeline phases generated.</Typography>
                ) : (
                  <Stack spacing={1.5}>
                    {Object.entries(plan.proposedTimeline || {}).map(([key, value]) => (
                      <Box key={key} sx={{ p: 1.5, bgcolor: tokens.colors.surface, borderRadius: 2, border: `1px solid ${tokens.colors.borderLight}` }}>
                        <Typography sx={{ fontWeight: 700, color: tokens.colors.pastelLavenderText }}>{key}</Typography>
                        <Typography variant="body2" color="text.secondary">{value}</Typography>
                      </Box>
                    ))}
                  </Stack>
                )}
              </SurfaceCard>
            </Grid>

            {/* 3. Budget (pastelPeachLight) */}
            <Grid size={{ xs: 12, md: 6 }}>
              <SurfaceCard variant="budget" sx={{ p: 3.5, height: '100%' }}>
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5, mb: 2.5 }}>
                  <Box sx={{ p: 1, borderRadius: 2, bgcolor: tokens.colors.pastelPeach, color: tokens.colors.pastelPeachText }}>
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
                    {Object.entries(plan.budgetAllocation || {}).map(([key, value]) => (
                      <Box key={key} sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', p: 1.5, bgcolor: tokens.colors.surface, borderRadius: 2, border: `1px solid ${tokens.colors.borderLight}` }}>
                        <Typography sx={{ fontWeight: 600 }}>{key}</Typography>
                        <Typography sx={{ fontWeight: 800, color: tokens.colors.pastelPeachText }}>
                          ${Number(value).toLocaleString()}
                        </Typography>
                      </Box>
                    ))}
                  </Stack>
                )}
              </SurfaceCard>
            </Grid>

            {/* 4. Tasks & Services (pastelGreenLight) */}
            <Grid size={{ xs: 12, md: 6 }}>
              <SurfaceCard variant="task" sx={{ p: 3.5, height: '100%' }}>
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5, mb: 2.5 }}>
                  <Box sx={{ p: 1, borderRadius: 2, bgcolor: tokens.colors.pastelGreen, color: tokens.colors.pastelGreenText }}>
                    <CheckCircleOutlineRoundedIcon />
                  </Box>
                  <Typography variant="h5" sx={{ fontWeight: 800 }}>
                    Service Tasks &amp; Requirements
                  </Typography>
                </Box>
                <Typography variant="caption" sx={{ fontWeight: 700, color: tokens.colors.pastelGreenText, display: 'block', mb: 1 }}>
                  Categories: {(plan.serviceCategories || []).join(', ') || 'None specified'}
                </Typography>
                <Stack spacing={1.5}>
                  {(plan.missingRequirements || []).map((item) => (
                    <Box key={item.requirement} sx={{ p: 1.5, bgcolor: tokens.colors.surface, borderRadius: 2, border: `1px solid ${tokens.colors.borderLight}` }}>
                      <Typography sx={{ fontWeight: 700, fontSize: '0.9rem' }}>{item.requirement}</Typography>
                      <Typography variant="caption" color="text.secondary">{item.reason}</Typography>
                    </Box>
                  ))}
                  {!plan.missingRequirements?.length && (
                    <Typography color="text.secondary">All requirements captured.</Typography>
                  )}
                </Stack>
              </SurfaceCard>
            </Grid>

            {/* 5. Identified Risks (pastelPinkLight) */}
            <Grid size={{ xs: 12, md: 6 }}>
              <SurfaceCard variant="vendor" sx={{ p: 3.5, height: '100%' }}>
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5, mb: 2.5 }}>
                  <Box sx={{ p: 1, borderRadius: 2, bgcolor: tokens.colors.pastelPink, color: tokens.colors.pastelPinkText }}>
                    <WarningAmberRoundedIcon />
                  </Box>
                  <Typography variant="h5" sx={{ fontWeight: 800 }}>
                    Identified Risks &amp; Mitigation
                  </Typography>
                </Box>
                <Stack spacing={1.5}>
                  {(plan.identifiedRisks || []).map((risk) => (
                    <Box key={risk.risk} sx={{ p: 1.5, bgcolor: tokens.colors.surface, borderRadius: 2, border: `1px solid ${tokens.colors.borderLight}` }}>
                      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 0.5 }}>
                        <Typography sx={{ fontWeight: 700 }}>{risk.risk}</Typography>
                        <Chip size="small" label={risk.severity} sx={{ bgcolor: tokens.colors.pastelYellowLight, color: tokens.colors.pastelYellowText, fontWeight: 800, fontSize: '0.7rem' }} />
                      </Box>
                      <Typography variant="body2" color="text.secondary">{risk.recommendation}</Typography>
                    </Box>
                  ))}
                  {!plan.identifiedRisks?.length && (
                    <Typography color="text.secondary">No operational risks identified.</Typography>
                  )}
                </Stack>
              </SurfaceCard>
            </Grid>
          </Grid>

          {/* Decision Record */}
          <SurfaceCard sx={{ p: 3.5 }}>
            <Typography variant="h5" sx={{ fontWeight: 800, mb: 1 }}>
              Coordinator Decision Audit
            </Typography>
            <Typography variant="body2" color="text.secondary" sx={{ mb: 1.5 }}>
              Status: <strong>{planStatusLabel(plan.status)}</strong> · Decided: {plan.plannerDecisionAt ? new Date(plan.plannerDecisionAt).toLocaleString() : 'Pending review'}
            </Typography>
            {plan.plannerRemarks && (
              <Alert severity="info" sx={{ borderRadius: 2.5 }}>
                Coordinator Remarks: {plan.plannerRemarks}
              </Alert>
            )}
          </SurfaceCard>
        </Stack>
      )}
    </AppLayout>
  );
}
