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
import ArrowUpwardRoundedIcon from '@mui/icons-material/ArrowUpwardRounded';
import ArrowDownwardRoundedIcon from '@mui/icons-material/ArrowDownwardRounded';
import AppLayout from '../../../shared/components/layout/AppLayout';
import SurfaceCard from '../../../shared/components/ui/SurfaceCard';
import StatusBadge from '../../../shared/components/ui/StatusBadge';
import { EVENT_STATUSES, EVENT_TYPES, enumLabel, useAdminEvents } from '../api/adminEventApi';

const columns = [
  ['id', 'Event ID'],
  ['owner', 'Owner'],
  ['eventType', 'Event Type'],
  ['guestCount', 'Guests'],
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
    <AppLayout
      activeTab="events"
      title="Event Governance"
      subtitle="Review and manage all platform events"
      searchValue={filters.search}
      onSearch={(value) => {
        setPage(0);
        setFilters((current) => ({ ...current, search: value }));
      }}
    >
      <Box sx={{ mb: 3 }}>
        <Typography variant="h3" sx={{ fontWeight: 800, letterSpacing: '-0.04em', mb: 0.5 }}>
          Event Management
        </Typography>
        <Typography color="text.secondary">Review and inspect every event across the platform.</Typography>
      </Box>

      <SurfaceCard sx={{ mb: 3, p: 2.5 }}>
        <Stack direction={{ xs: 'column', md: 'row' }} flexWrap="wrap" gap={2} sx={{ alignItems: 'center' }}>
          <FormControl size="small" sx={{ minWidth: 160 }}>
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

          <FormControl size="small" sx={{ minWidth: 160 }}>
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

          <TextField size="small" type="date" label="From date" InputLabelProps={{ shrink: true }} value={filters.dateFrom} onChange={setFilter('dateFrom')} />
          <TextField size="small" type="date" label="To date" InputLabelProps={{ shrink: true }} value={filters.dateTo} onChange={setFilter('dateTo')} />

          <FormControl size="small" sx={{ minWidth: 160 }}>
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
            startIcon={sort.sortOrder === 'asc' ? <ArrowUpwardRoundedIcon /> : <ArrowDownwardRoundedIcon />}
            sx={{ borderRadius: 9999, px: 2, height: 40 }}
          >
            {sort.sortOrder === 'asc' ? 'Ascending' : 'Descending'}
          </Button>

          <Button
            variant="text"
            onClick={() => {
              setFilters({ search: '', status: '', eventType: '', ownerId: '', dateFrom: '', dateTo: '' });
              setPage(0);
            }}
          >
            Clear Filters
          </Button>
        </Stack>
      </SurfaceCard>

      {query.isError && (
        <Alert severity="error" sx={{ mb: 2, borderRadius: 3 }}>
          {query.error.message}
        </Alert>
      )}

      <SurfaceCard sx={{ p: 3 }}>
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
                <TableCell>Action</TableCell>
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
                    <TableCell sx={{ fontFamily: 'monospace', fontSize: '0.8rem' }}>{item.id.slice(0, 12)}…</TableCell>
                    <TableCell>{item.owner?.email || item.ownerId}</TableCell>
                    <TableCell>{enumLabel(item.eventType, EVENT_TYPES.map((value) => value.replaceAll('_', ' ')))}</TableCell>
                    <TableCell>{item.guestCount}</TableCell>
                    <TableCell>${Number(item.budget).toLocaleString()}</TableCell>
                    <TableCell>
                      <StatusBadge status={item.status} label={enumLabel(item.status)} />
                    </TableCell>
                    <TableCell>{formatDate(item.preferredDate)}</TableCell>
                    <TableCell>{formatDate(item.createdAt)}</TableCell>
                    <TableCell>
                      <Button
                        size="small"
                        variant="outlined"
                        onClick={() => navigate(`/admin/events/${item.id}`)}
                        sx={{ borderRadius: 9999, px: 2, fontSize: '0.78rem' }}
                      >
                        Inspect
                      </Button>
                    </TableCell>
                  </TableRow>
                ))
              )}

              {!query.isLoading && items.length === 0 && (
                <TableRow>
                  <TableCell colSpan={9} align="center">
                    <Typography sx={{ py: 4 }} color="text.secondary">
                      No events found matching your filter criteria.
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
    </AppLayout>
  );
}
