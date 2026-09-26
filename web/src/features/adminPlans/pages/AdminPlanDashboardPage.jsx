import { useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Alert, Box, Button, CircularProgress, FormControl, Grid, IconButton, InputLabel,
  LinearProgress, MenuItem, Select, Stack, Table, TableBody, TableCell,
  TableContainer, TableHead, TablePagination, TableRow, TextField, Typography,
} from '@mui/material';
import VisibilityOutlinedIcon from '@mui/icons-material/VisibilityOutlined';
import FileDownloadOutlinedIcon from '@mui/icons-material/FileDownloadOutlined';
import RefreshOutlinedIcon from '@mui/icons-material/RefreshOutlined';
import { useAuthStore } from '../../../shared/store/authStore';
import CollapsibleSidebar from '../../../shared/components/layout/CollapsibleSidebar';
import TopSearchNavbar from '../../../shared/components/layout/TopSearchNavbar';
import SurfaceCard from '../../../shared/components/ui/SurfaceCard';
import StatusBadge from '../../../shared/components/ui/StatusBadge';
import { PLAN_STATUSES, planStatusLabel, unwrapPlanList, useAdminPlans } from '../api/adminPlanApi';

const emptyFilters = {
  status: '', dateFrom: '', dateTo: '', plannerId: '', eventType: '',
  scoreMin: '', scoreMax: '', search: '',
};

function dateLabel(value) {
  return value ? new Date(value).toLocaleDateString() : '—';
}

function scoreOf(plan) {
  return Number(plan.planCompletenessScore ?? plan.completenessScore ?? 0);
}

function eventNameOf(plan) {
  return plan.eventSnapshot?.eventName || plan.eventName || 'Unnamed event';
}

export default function AdminPlanDashboardPage() {
  const navigate = useNavigate();
  const user = useAuthStore((state) => state.user);
  const logout = useAuthStore((state) => state.logout);
  const [page, setPage] = useState(0);
  const [pageSize, setPageSize] = useState(10);
  const [filters, setFilters] = useState(emptyFilters);
  const [sort, setSort] = useState({ sortBy: 'generatedAt', sortOrder: 'desc' });
  const params = useMemo(() => ({
    ...filters,
    page: page + 1,
    pageSize,
    sortBy: sort.sortBy,
    sortOrder: sort.sortOrder,
  }), [filters, page, pageSize, sort]);
  const query = useAdminPlans(params);
  const result = unwrapPlanList(query.data);
  const items = result.items;

  const setFilter = (name) => (event) => {
    setPage(0);
    setFilters((current) => ({ ...current, [name]: event.target.value }));
  };
  const handleLogout = () => {
    logout();
    navigate('/login');
  };
  const kpis = useMemo(() => {
    const scores = items.map(scoreOf);
    return [
      ['Total plans generated', result.total, '#BCD8F0'],
      ['Pending review', items.filter((item) => planStatusLabel(item.status) === 'PENDING').length, '#FEE388'],
      ['Approved', items.filter((item) => planStatusLabel(item.status) === 'APPROVED').length, '#C4DDB8'],
      ['Rejected', items.filter((item) => planStatusLabel(item.status) === 'REJECTED').length, '#F9BFD8'],
      ['Average completeness', scores.length ? `${Math.round(scores.reduce((a, b) => a + b, 0) / scores.length)}%` : '—', '#E5D4F7'],
    ];
  }, [items, result.total]);

  return (
    <Box sx={{ display: 'flex', minHeight: '100vh', backgroundColor: '#F7F3E9', p: { xs: 1.5, md: 2.5 }, gap: { xs: 2, md: 3 } }}>
      <CollapsibleSidebar
        activeTab="plans"
        showEvents
        showPlanMonitoring
        onSelectTab={(tab) => {
          if (tab === 'dashboard') navigate('/dashboard');
          if (tab === 'events') navigate('/admin/events');
          if (tab === 'plans') navigate('/admin/plans');
        }}
        onLogout={handleLogout}
      />
      <Box sx={{ flex: 1, minWidth: 0 }}>
        <TopSearchNavbar user={user} title="Plan monitoring" subtitle="Admin workspace" onSearch={(value) => setFilters((current) => ({ ...current, search: value }))} />
        <Stack direction={{ xs: 'column', md: 'row' }} justifyContent="space-between" alignItems={{ md: 'center' }} sx={{ mb: 2.5 }}>
          <Box>
            <Typography variant="h3" sx={{ fontWeight: 800, letterSpacing: '-0.04em', mb: 0.5 }}>Coordinator plans</Typography>
            <Typography color="text.secondary">Monitor quality, decisions, and generation activity.</Typography>
          </Box>
          <Button variant="outlined" startIcon={<RefreshOutlinedIcon />} onClick={() => query.refetch()}>Refresh</Button>
        </Stack>
        <Grid container spacing={1.5} sx={{ mb: 2.5 }}>
          {kpis.map(([label, value, color]) => (
            <Grid key={label} size={{ xs: 12, sm: 6, lg: 2.4 }}>
              <SurfaceCard sx={{ p: 2, background: color }}>
                <Typography variant="body2">{label}</Typography>
                <Typography variant="h4" sx={{ fontWeight: 800, mt: 0.5 }}>{value}</Typography>
              </SurfaceCard>
            </Grid>
          ))}
        </Grid>
        <SurfaceCard sx={{ p: 2, mb: 2.5 }}>
          <Stack direction={{ xs: 'column', md: 'row' }} flexWrap="wrap" gap={1.5}>
            <FormControl size="small" sx={{ minWidth: 180 }}>
              <InputLabel>Status</InputLabel>
              <Select value={filters.status} label="Status" onChange={setFilter('status')}>
                <MenuItem value="">All statuses</MenuItem>
                {PLAN_STATUSES.map((status) => <MenuItem key={status} value={status}>{planStatusLabel(status)}</MenuItem>)}
              </Select>
            </FormControl>
            <FormControl size="small" sx={{ minWidth: 170 }}>
              <InputLabel>Sort by</InputLabel>
              <Select value={sort.sortBy} label="Sort by" onChange={(event) => setSort((current) => ({ ...current, sortBy: event.target.value }))}>
                <MenuItem value="generatedAt">Generated date</MenuItem>
                <MenuItem value="status">Status</MenuItem>
                <MenuItem value="completeness">Completeness</MenuItem>
                <MenuItem value="eventName">Event name</MenuItem>
              </Select>
            </FormControl>
            <TextField size="small" type="date" label="Generated from" InputLabelProps={{ shrink: true }} value={filters.dateFrom} onChange={setFilter('dateFrom')} />
            <TextField size="small" type="date" label="Generated to" InputLabelProps={{ shrink: true }} value={filters.dateTo} onChange={setFilter('dateTo')} />
            <TextField size="small" type="number" label="Min score" value={filters.scoreMin} onChange={setFilter('scoreMin')} />
            <TextField size="small" type="number" label="Max score" value={filters.scoreMax} onChange={setFilter('scoreMax')} />
            <Button variant="text" onClick={() => { setFilters(emptyFilters); setPage(0); }}>Clear filters</Button>
          </Stack>
        </SurfaceCard>
        {query.isError && (
          <Alert severity="error" sx={{ mb: 2, borderRadius: 3 }}>
            {query.error.message}. The API must expose <code>GET /api/admin/plans</code> for this dashboard.
          </Alert>
        )}
        <SurfaceCard>
          <TableContainer sx={{ overflowX: 'auto' }}>
            <Table size="small">
              <TableHead><TableRow>
                {['Event name', 'Version', 'Status', 'Completeness', 'Generated', 'Decision', 'Actions'].map((label) => <TableCell key={label}>{label}</TableCell>)}
              </TableRow></TableHead>
              <TableBody>
                {query.isLoading ? <TableRow><TableCell colSpan={7} align="center"><CircularProgress sx={{ my: 4 }} /></TableCell></TableRow> :
                  items.length === 0 ? <TableRow><TableCell colSpan={7} align="center"><Typography sx={{ py: 4 }} color="text.secondary">No plans match these filters.</Typography></TableCell></TableRow> :
                    items.map((plan) => {
                      const score = scoreOf(plan);
                      return <TableRow hover key={plan.id}>
                        <TableCell><Typography sx={{ fontWeight: 700 }}>{eventNameOf(plan)}</Typography><Typography variant="body2">{plan.eventSnapshot?.location || '—'}</Typography></TableCell>
                        <TableCell>v{plan.version || 1}</TableCell>
                        <TableCell><StatusBadge status={planStatusLabel(plan.status)} label={planStatusLabel(plan.status)} /></TableCell>
                        <TableCell sx={{ minWidth: 150 }}><Typography variant="body2">{score}%</Typography><LinearProgress variant="determinate" value={Math.max(0, Math.min(100, score))} /></TableCell>
                        <TableCell>{dateLabel(plan.generatedAt)}</TableCell>
                        <TableCell>{dateLabel(plan.plannerDecisionAt)}</TableCell>
                        <TableCell><IconButton aria-label={`View ${eventNameOf(plan)}`} onClick={() => navigate(`/admin/plans/${plan.id}`)}><VisibilityOutlinedIcon /></IconButton><IconButton aria-label="Export plan"><FileDownloadOutlinedIcon /></IconButton></TableCell>
                      </TableRow>;
                    })}
              </TableBody>
            </Table>
          </TableContainer>
          <TablePagination component="div" count={result.total} page={page} rowsPerPage={pageSize} onPageChange={(_, value) => setPage(value)} onRowsPerPageChange={(event) => { setPageSize(Number(event.target.value)); setPage(0); }} rowsPerPageOptions={[10, 25, 50]} />
        </SurfaceCard>
      </Box>
    </Box>
  );
}
