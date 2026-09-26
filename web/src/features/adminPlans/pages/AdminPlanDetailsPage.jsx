import { useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { Alert, Box, Button, CircularProgress, Divider, Stack, Tab, Tabs, Typography } from '@mui/material';
import ArrowBackOutlinedIcon from '@mui/icons-material/ArrowBackOutlined';
import { useAuthStore } from '../../../shared/store/authStore';
import CollapsibleSidebar from '../../../shared/components/layout/CollapsibleSidebar';
import SurfaceCard from '../../../shared/components/ui/SurfaceCard';
import StatusBadge from '../../../shared/components/ui/StatusBadge';
import { planStatusLabel, useAdminPlan } from '../api/adminPlanApi';

export default function AdminPlanDetailsPage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const [tab, setTab] = useState(0);
  const logout = useAuthStore((state) => state.logout);
  const query = useAdminPlan(id);
  const plan = query.data?.data || query.data;
  const name = plan?.eventSnapshot?.eventName || 'Plan details';

  return <Box sx={{ display: 'flex', minHeight: '100vh', backgroundColor: '#F7F3E9', p: { xs: 1.5, md: 2.5 }, gap: { xs: 2, md: 3 } }}>
    <CollapsibleSidebar activeTab="plans" showEvents showPlanMonitoring onSelectTab={(value) => value === 'events' ? navigate('/admin/events') : value === 'dashboard' ? navigate('/dashboard') : navigate('/admin/plans')} onLogout={() => { logout(); navigate('/login'); }} />
    <Box sx={{ flex: 1, minWidth: 0 }}>
      <Button startIcon={<ArrowBackOutlinedIcon />} onClick={() => navigate('/admin/plans')} sx={{ mb: 1 }}>Back to plans</Button>
      {query.isLoading && <Box sx={{ display: 'flex', justifyContent: 'center', py: 8 }}><CircularProgress /></Box>}
      {query.isError && <Alert severity="error">{query.error.message}</Alert>}
      {plan && <Stack spacing={2.5}>
        <Box><Typography variant="h3" sx={{ fontWeight: 800 }}>{name}</Typography><Typography color="text.secondary">Version {plan.version || 1} · Generated {plan.generatedAt ? new Date(plan.generatedAt).toLocaleString() : '—'}</Typography></Box>
        <SurfaceCard sx={{ p: 0 }}>
          <Tabs value={tab} onChange={(_, value) => setTab(value)} variant="scrollable"><Tab label="Summary" /><Tab label="Budget" /><Tab label="Timeline" /><Tab label="Risks" /><Tab label="Missing requirements" /><Tab label="Version history" /></Tabs>
          <Divider />
          <Box sx={{ p: 3 }}>
            {tab === 0 && <Stack spacing={2}><Box><StatusBadge status={planStatusLabel(plan.status)} label={planStatusLabel(plan.status)} /><Typography sx={{ mt: 2 }}>{plan.rationale || 'No rationale provided.'}</Typography></Box><Typography>Completeness: {plan.planCompletenessScore ?? plan.completenessScore ?? 0}%</Typography><Typography>Categories: {(plan.serviceCategories || []).join(', ') || '—'}</Typography></Stack>}
            {tab === 1 && <Stack spacing={1}>{Object.entries(plan.budgetAllocation || {}).map(([key, value]) => <Box key={key} sx={{ display: 'flex', justifyContent: 'space-between' }}><Typography>{key}</Typography><Typography fontWeight={700}>{value}</Typography></Box>)}</Stack>}
            {tab === 2 && <Stack spacing={1}>{Object.entries(plan.proposedTimeline || {}).map(([key, value]) => <Box key={key}><Typography fontWeight={700}>{key}</Typography><Typography color="text.secondary">{value}</Typography></Box>)}</Stack>}
            {tab === 3 && <Stack spacing={2}>{(plan.identifiedRisks || []).map((risk) => <Box key={risk.risk}><Typography fontWeight={700}>{risk.risk} · {risk.severity}</Typography><Typography color="text.secondary">{risk.recommendation}</Typography></Box>)}</Stack>}
            {tab === 4 && <Stack spacing={2}>{(plan.missingRequirements || []).map((item) => <Box key={item.requirement}><Typography fontWeight={700}>{item.requirement}</Typography><Typography color="text.secondary">{item.reason}</Typography></Box>)}</Stack>}
            {tab === 5 && <Typography color="text.secondary">Version history is provided by the admin plans API when available.</Typography>}
          </Box>
        </SurfaceCard>
        <SurfaceCard sx={{ p: 3 }}><Typography variant="h5" sx={{ fontWeight: 800, mb: 1 }}>Planner decision</Typography><Typography>Decision: {planStatusLabel(plan.status)}</Typography><Typography>Decision date: {plan.plannerDecisionAt ? new Date(plan.plannerDecisionAt).toLocaleString() : '—'}</Typography>{plan.plannerRemarks && <Typography sx={{ mt: 1 }}>Remarks: {plan.plannerRemarks}</Typography>}</SurfaceCard>
      </Stack>}
    </Box>
  </Box>;
}
