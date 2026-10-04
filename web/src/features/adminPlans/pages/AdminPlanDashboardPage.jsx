import { useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Alert,
  Box,
  Button,
  CircularProgress,
  FormControl,
  Grid,
  InputLabel,
  LinearProgress,
  MenuItem,
  Select,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TablePagination,
  TableRow,
  TextField,
  Typography,
} from '@mui/material';
import VisibilityOutlinedIcon from '@mui/icons-material/VisibilityOutlined';
import RefreshOutlinedIcon from '@mui/icons-material/RefreshOutlined';
import AppLayout from '../../../shared/components/layout/AppLayout';
import SurfaceCard from '../../../shared/components/ui/SurfaceCard';
import StatusBadge from '../../../shared/components/ui/StatusBadge';
import { PLAN_STATUSES, planStatusLabel, unwrapPlanList, useAdminPlans } from '../api/adminPlanApi';
import { tokens } from '../../../shared/theme/tokens';

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

  const kpis = useMemo(() => {
    const scores = items.map(scoreOf);
    return [
      { label: 'Total plans generated', value: result.total, variant: 'analytics' },
      { label: 'Pending review', value: items.filter((item) => planStatusLabel(item.status) === 'PENDING').length, variant: 'budget' },
      { label: 'Approved', value: items.filter((item) => planStatusLabel(item.status) === 'APPROVED').length, variant: 'task' },
      { label: 'Rejected', value: items.filter((item) => planStatusLabel(item.status) === 'REJECTED').length, variant: 'vendor' },
      { label: 'Average completeness', value: scores.length ? `${Math.round(scores.reduce((a, b) => a + b, 0) / scores.length)}%` : '—', variant: 'aiPlan' },
    ];
  }, [items, result.total]);

  return (
    <AppLayout
      activeTab="plans"
      title="Plan Governance"
      subtitle="Monitor AI generation quality and coordinator review decisions"
      searchValue={filters.search}
      onSearch={(value) => {
        setPage(0);
        setFilters((current) => ({ ...current, search: value }));
      }}
    >
      <Stack direction={{ xs: 'column', md: 'row' }} sx={{ mb: 2.5, justifyContent: 'space-between', alignItems: { md: 'center' } }} spacing={2}>
        <Box>
          <Typography variant="h3" sx={{ fontWeight: 800, letterSpacing: '-0.04em', mb: 0.5 }}>
            AI Coordinator Plans
          </Typography>
          <Typography color="text.secondary">
            Monitor quality, review decisions, and generation telemetry across events.
          </Typography>
        </Box>
        <Button
          variant="outlined"
          startIcon={<RefreshOutlinedIcon />}
          onClick={() => query.refetch()}
          sx={{ borderRadius: 9999, px: 2.5 }}
        >
          Refresh
        </Button>
      </Stack>

      {/* KPI Cards using pastel variants */}
      <Grid container spacing={2} sx={{ mb: 3 }}>
        {kpis.map((kpi) => (
          <Grid key={kpi.label} size={{ xs: 12, sm: 6, lg: 2.4 }}>
            <SurfaceCard variant={kpi.variant} sx={{ p: 2.5, height: '100%' }}>
              <Typography variant="body2" color="text.secondary" sx={{ fontWeight: 600 }}>
                {kpi.label}
              </Typography>
              <Typography variant="h4" sx={{ fontWeight: 800, mt: 0.5 }}>
                {kpi.value}
              </Typography>
            </SurfaceCard>
          </Grid>
        ))}
      </Grid>

      {/* Filter Surface */}
      <SurfaceCard sx={{ p: 2.5, mb: 3 }}>
        <Stack direction={{ xs: 'column', md: 'row' }} flexWrap="wrap" gap={1.5} sx={{ alignItems: 'center' }}>
          <FormControl size="small" sx={{ minWidth: 180 }}>
            <InputLabel>Status</InputLabel>
            <Select value={filters.status} label="Status" onChange={setFilter('status')}>
              <MenuItem value="">All statuses</MenuItem>
              {PLAN_STATUSES.map((status) => (
                <MenuItem key={status} value={status}>{planStatusLabel(status)}</MenuItem>
              ))}
            </Select>
          </FormControl>

          <FormControl size="small" sx={{ minWidth: 170 }}>
            <InputLabel>Sort by</InputLabel>
            <Select
              value={sort.sortBy}
              label="Sort by"
              onChange={(event) => setSort((current) => ({ ...current, sortBy: event.target.value }))}
            >
              <MenuItem value="generatedAt">Generated date</MenuItem>
              <MenuItem value="status">Status</MenuItem>
              <MenuItem value="completeness">Completeness</MenuItem>
              <MenuItem value="eventName">Event name</MenuItem>
            </Select>
          </FormControl>

          <TextField size="small" type="date" label="Generated from" InputLabelProps={{ shrink: true }} value={filters.dateFrom} onChange={setFilter('dateFrom')} />
          <TextField size="small" type="date" label="Generated to" InputLabelProps={{ shrink: true }} value={filters.dateTo} onChange={setFilter('dateTo')} />
          <TextField size="small" type="number" label="Min score" value={filters.scoreMin} onChange={setFilter('scoreMin')} sx={{ width: 110 }} />
          <TextField size="small" type="number" label="Max score" value={filters.scoreMax} onChange={setFilter('scoreMax')} sx={{ width: 110 }} />

          <Button
            variant="text"
            onClick={() => {
              setFilters(emptyFilters);
              setPage(0);
            }}
          >
            Clear Filters
          </Button>
        </Stack>
      </SurfaceCard>

      {query.isError && (
        <Alert severity="warning" sx={{ mb: 2, borderRadius: 3 }}>
          Plans endpoint notification: {query.error.message}. Displaying available drafts and event plans.
        </Alert>
      )}

      {/* Plans Table */}
      <SurfaceCard sx={{ p: 3 }}>
        <TableContainer sx={{ overflowX: 'auto' }}>
          <Table size="small">
            <TableHead>
              <TableRow>
                {['Event Name', 'Version', 'Status', 'Completeness', 'Generated', 'Decision', 'Actions'].map((label) => (
                  <TableCell key={label}>{label}</TableCell>
                ))}
              </TableRow>
            </TableHead>
            <TableBody>
              {query.isLoading ? (
                <TableRow>
                  <TableCell colSpan={7} align="center">
                    <CircularProgress sx={{ my: 4 }} />
                  </TableCell>
                </TableRow>
              ) : items.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={7} align="center">
                    <Typography sx={{ py: 4 }} color="text.secondary">
                      No plans match these filters.
                    </Typography>
                  </TableCell>
                </TableRow>
              ) : (
                items.map((plan) => {
                  const score = scoreOf(plan);
                  return (
                    <TableRow hover key={plan.id}>
                      <TableCell>
                        <Typography sx={{ fontWeight: 700 }}>{eventNameOf(plan)}</Typography>
                        <Typography variant="body2" color="text.secondary">{plan.eventSnapshot?.preferredVenue || '—'}</Typography>
                      </TableCell>
                      <TableCell>v{plan.version || 1}</TableCell>
                      <TableCell>
                        <StatusBadge status={planStatusLabel(plan.status)} label={planStatusLabel(plan.status)} />
                      </TableCell>
                      <TableCell sx={{ minWidth: 150 }}>
                        <Typography variant="caption" sx={{ fontWeight: 700 }}>{score}%</Typography>
                        <LinearProgress
                          variant="determinate"
                          value={Math.max(0, Math.min(100, score))}
                          sx={{ height: 6, borderRadius: 3, bgcolor: tokens.colors.pastelBlueLight, '& .MuiLinearProgress-bar': { bgcolor: tokens.colors.pastelBlueBorder } }}
                        />
                      </TableCell>
                      <TableCell>{dateLabel(plan.generatedAt)}</TableCell>
                      <TableCell>{dateLabel(plan.plannerDecisionAt)}</TableCell>
                      <TableCell>
                        <Button
                          size="small"
                          variant="outlined"
                          startIcon={<VisibilityOutlinedIcon />}
                          onClick={() => navigate(`/admin/plans/${plan.id}`)}
                          sx={{ borderRadius: 9999, fontSize: '0.78rem' }}
                        >
                          View
                        </Button>
                      </TableCell>
                    </TableRow>
                  );
                })
              )}
            </TableBody>
          </Table>
        </TableContainer>

        <TablePagination
          component="div"
          count={result.total}
          page={page}
          rowsPerPage={pageSize}
          onPageChange={(_, value) => setPage(value)}
          onRowsPerPageChange={(event) => {
            setPageSize(Number(event.target.value));
            setPage(0);
          }}
          rowsPerPageOptions={[10, 25, 50]}
        />
      </SurfaceCard>
    </AppLayout>
  );
}
