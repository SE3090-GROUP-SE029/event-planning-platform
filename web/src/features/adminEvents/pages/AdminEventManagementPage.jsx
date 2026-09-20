import { useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Alert,
  Box,
  Button,
  CircularProgress,
  FormControl,
  InputLabel,
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
import { useAuthStore } from '../../../shared/store/authStore';
import CollapsibleSidebar from '../../../shared/components/layout/CollapsibleSidebar';
import TopSearchNavbar from '../../../shared/components/layout/TopSearchNavbar';
import SurfaceCard from '../../../shared/components/ui/SurfaceCard';
import StatusBadge from '../../../shared/components/ui/StatusBadge';
import { EVENT_STATUSES, EVENT_TYPES, enumLabel, useAdminEvents } from '../api/adminEventApi';

const columns = [
  ['id', 'Event Id'],
  ['owner', 'Owner'],
  ['eventType', 'Event Type'],
  ['guestCount', 'Guest Count'],
  ['budget', 'Budget'],
  ['status', 'Status'],
  ['preferredDate', 'Preferred Date'],
  ['createdAt', 'Created Date'],
];

function formatDate(value) {
  return value ? new Date(value).toLocaleDateString() : '—';
}

export default function AdminEventManagementPage() {
  const navigate = useNavigate();
  const user = useAuthStore((state) => state.user);
  const logout = useAuthStore((state) => state.logout);
  const [page, setPage] = useState(0);
  const [pageSize, setPageSize] = useState(10);
  const [filters, setFilters] = useState({ search: '', status: '', eventType: '', ownerId: '', dateFrom: '', dateTo: '' });
  const [sort, setSort] = useState({ sortBy: 'createdAt', sortOrder: 'desc' });

  const params = useMemo(
    () => ({
      ...filters,
      page: page + 1,
      pageSize,
      sortBy: sort.sortBy,
      sortOrder: sort.sortOrder,
    }),
    [filters, page, pageSize, sort]
  );

  const query = useAdminEvents(params);
  const items = query.data?.items || [];

  const setFilter = (name) => (event) => {
    setPage(0);
    setFilters((current) => ({ ...current, [name]: event.target.value }));
  };

  const handleSort = (field) => {
    const nextOrder = sort.sortBy === field && sort.sortOrder === 'asc' ? 'desc' : 'asc';
    setSort({ sortBy: field, sortOrder: nextOrder });
    setPage(0);
  };

  return (
    <Box sx={{ display: 'flex', minHeight: '100vh', backgroundColor: '#F7F3E9', p: { xs: 1.5, md: 2.5 }, gap: { xs: 2, md: 3 } }}>
      <CollapsibleSidebar
        activeTab="events"
        showEvents
        onSelectTab={(tab) => tab === 'dashboard' && navigate('/dashboard')}
        onLogout={() => {
          logout();
          navigate('/login');
        }}
      />

      <Box sx={{ flex: 1, minWidth: 0 }}>
        <TopSearchNavbar user={user} onSearch={(value) => setFilters((current) => ({ ...current, search: value }))} title="Events" subtitle="Overview" />

        <Stack direction={{ xs: 'column', md: 'row' }} justifyContent="space-between" alignItems={{ md: 'center' }} sx={{ mb: 2.5 }}>
          <Box>
            <Typography variant="h3" sx={{ fontWeight: 800, letterSpacing: '-0.04em', mb: 0.5 }}>
              Event management
            </Typography>
            <Typography color="text.secondary">Review and inspect every event across the platform.</Typography>
          </Box>
        </Stack>

        <SurfaceCard sx={{ mb: 2.5, margin: '4px' }}>
          <Box sx={{ p: 2.5 }}>
            <Stack direction={{ xs: 'column', md: 'row' }} flexWrap="wrap" gap={2}>
              <FormControl size="small" sx={{ minWidth: 150 }}>
                <InputLabel>Status</InputLabel>
                <Select value={filters.status} label="Status" onChange={setFilter('status')}>
                  <MenuItem value="">All statuses</MenuItem>
                  {EVENT_STATUSES.map((value) => (
                    <MenuItem key={value} value={value}>
                      {enumLabel(value)}
                    </MenuItem>
                  ))}
                </Select>
              </FormControl>

              <FormControl size="small" sx={{ minWidth: 150, margin: '4px' }}>
                <InputLabel>Event type</InputLabel>
                <Select value={filters.eventType} label="Event type" onChange={setFilter('eventType')}>
                  <MenuItem value="">All types</MenuItem>
                  {EVENT_TYPES.map((value) => (
                    <MenuItem key={value} value={value}>
                      {enumLabel(value, EVENT_TYPES.map((item) => item.replaceAll('_', ' ')))}
                    </MenuItem>
                  ))}
                </Select>
              </FormControl>

              <TextField sx={{ margin: '4px' }} size="small" type="date" label="From date" InputLabelProps={{ shrink: true }} slotProps={{ inputLabel:{ shrink: true}}} value={filters.dateFrom} onChange={setFilter('dateFrom')} />
              <TextField sx={{ margin: '4px' }} size="small" type="date" label="To date" InputLabelProps={{ shrink: true }} slotProps={{ inputLabel:{ shrink: true}}} value={filters.dateTo} onChange={setFilter('dateTo')} />

              <FormControl size="small" sx={{ minWidth: 170, margin: '4px' }}>
                <InputLabel>Sort by</InputLabel>
                <Select
                  value={sort.sortBy}
                  label="Sort by"
                  onChange={(event) => {
                    setSort((current) => ({ ...current, sortBy: event.target.value }));
                    setPage(0);
                  }}
                >
                  <MenuItem value="createdAt">Created date</MenuItem>
                  <MenuItem value="preferredDate">Preferred date</MenuItem>
                  <MenuItem value="budget">Budget</MenuItem>
                </Select>
              </FormControl>

              <Button
                variant="outlined"
                onClick={() => setSort((current) => ({ ...current, sortOrder: current.sortOrder === 'asc' ? 'desc' : 'asc' }))}
                 sx={{
                  margin: '4px',
                  minWidth: 'auto',
                  width: '70px',
                  height: '34px',
                  padding: '4px 10px',
                  fontSize: '12px',
                  fontWeight: 400,
                  textTransform: 'none',
                 }}
              >
                {sort.sortOrder === 'asc' ? 'Asc' : 'Des'}
              </Button>
            </Stack>
          </Box>
        </SurfaceCard>

        {query.isError && (
          <Alert severity="error" sx={{ mb: 2, borderRadius: 3 }}>
            {query.error.message}
          </Alert>
        )}

        <SurfaceCard>
          <TableContainer>
            <Table size="small">
              <TableHead>
                <TableRow>
                  {columns.map(([field, label]) => (
                    <TableCell
                      key={field}
                      onClick={() => ['createdAt', 'preferredDate', 'budget'].includes(field) && handleSort(field)}
                      sx={{ cursor: ['createdAt', 'preferredDate', 'budget'].includes(field) ? 'pointer' : 'default' }}
                    >
                      {label}
                    </TableCell>
                  ))}
                  <TableCell />
                </TableRow>
              </TableHead>
              <TableBody>
                {query.isLoading ? (
                  <TableRow>
                    <TableCell colSpan={9} align="center">
                      <CircularProgress sx={{ my: 4 }} />
                    </TableCell>
                  </TableRow>
                ) : (
                  items.map((item) => (
                    <TableRow hover key={item.id}>
                      <TableCell sx={{ fontFamily: 'monospace' }}>{item.id}</TableCell>
                      <TableCell>{item.owner?.email || item.ownerId}</TableCell>
                      <TableCell>{enumLabel(item.eventType, EVENT_TYPES.map((value) => value.replaceAll('_', ' ')))}</TableCell>
                      <TableCell>{item.guestCount}</TableCell>
                      <TableCell>{Number(item.budget).toLocaleString(undefined, { style: 'currency', currency: 'USD' })}</TableCell>
                      <TableCell>
                        <StatusBadge status={item.status} label={enumLabel(item.status)} />
                      </TableCell>
                      <TableCell>{formatDate(item.preferredDate)}</TableCell>
                      <TableCell>{formatDate(item.createdAt)}</TableCell>
                      <TableCell>
                        <Button size="small" onClick={() => navigate(`/admin/events/${item.id}`)}>
                          View
                        </Button>
                      </TableCell>
                    </TableRow>
                  ))
                )}

                {!query.isLoading && items.length === 0 && (
                  <TableRow>
                    <TableCell colSpan={9} align="center">
                      <Typography sx={{ py: 4 }} color="text.secondary">
                        No events found.
                      </Typography>
                    </TableCell>
                  </TableRow>
                )}
              </TableBody>
            </Table>
          </TableContainer>

          <TablePagination
            component="div"
            count={query.data?.totalCount || 0}
            page={page}
            onPageChange={(_, next) => setPage(next)}
            rowsPerPage={pageSize}
            onRowsPerPageChange={(event) => {
              setPageSize(Number(event.target.value));
              setPage(0);
            }}
            rowsPerPageOptions={[10, 25, 50]}
          />
        </SurfaceCard>
      </Box>
    </Box>
  );
}
