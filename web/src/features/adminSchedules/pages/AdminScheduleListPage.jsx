import { useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Alert,
  Box,
  Button,
  CircularProgress,
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
import AppLayout from '../../../shared/components/layout/AppLayout';
import SurfaceCard from '../../../shared/components/ui/SurfaceCard';
import { formatDate, formatTime, useAdminSchedules } from '../api/adminScheduleApi';

export default function AdminScheduleListPage() {
  const navigate = useNavigate();
  const [page, setPage] = useState(0);
  const [pageSize, setPageSize] = useState(10);
  const [search, setSearch] = useState('');

  const params = useMemo(() => ({
    search,
    page: page + 1,
    pageSize,
  }), [search, page, pageSize]);

  const query = useAdminSchedules(params);
  const items = query.data?.items || [];

  const handleSearch = (value) => {
    setSearch(value);
    setPage(0);
  };

  return (
    <AppLayout
      activeTab="schedules"
      title="Schedule Monitoring"
      subtitle="Read-only schedule activity and conflict oversight"
      onSearch={handleSearch}
    >
      <Stack direction={{ xs: 'column', md: 'row' }} sx={{ mb: 2.5, justifyContent: 'space-between', alignItems: { md: 'center' } }} spacing={2}>
        <Box>
          <Typography variant="h3" sx={{ fontWeight: 800, letterSpacing: '-0.04em', mb: 0.5 }}>
            Schedules
          </Typography>
          <Typography color="text.secondary">
            Monitor event timelines, activity counts, and unresolved scheduling conflicts.
          </Typography>
        </Box>
      </Stack>

      <SurfaceCard sx={{ p: 2.5, mb: 3 }}>
        <TextField
          size="small"
          label="Search by event title"
          value={search}
          onChange={(event) => handleSearch(event.target.value)}
          sx={{ minWidth: { xs: '100%', sm: 320 } }}
        />
      </SurfaceCard>

      {query.isError && (
        <Alert severity="error" sx={{ mb: 2, borderRadius: 3 }}>
          {query.error.message}
        </Alert>
      )}

      <SurfaceCard sx={{ p: 3 }}>
        <TableContainer sx={{ overflowX: 'auto' }}>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>Event title</TableCell>
                <TableCell>Event date</TableCell>
                <TableCell>Event time</TableCell>
                <TableCell>Activities</TableCell>
                <TableCell>Unresolved conflicts</TableCell>
                <TableCell>Action</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {query.isLoading ? (
                <TableRow>
                  <TableCell colSpan={6} align="center">
                    <CircularProgress sx={{ my: 4 }} />
                  </TableCell>
                </TableRow>
              ) : items.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={6} align="center">
                    <Typography sx={{ py: 4 }} color="text.secondary">
                      No schedules found matching your search.
                    </Typography>
                  </TableCell>
                </TableRow>
              ) : (
                items.map((item) => (
                  <TableRow hover key={item.scheduleId}>
                    <TableCell>
                      <Typography sx={{ fontWeight: 700 }}>{item.eventTitle || 'Untitled event'}</Typography>
                      <Typography variant="body2" color="text.secondary">{item.eventId}</Typography>
                    </TableCell>
                    <TableCell>{formatDate(item.eventDate)}</TableCell>
                    <TableCell>{formatTime(item.eventStartTime)} - {formatTime(item.eventEndTime)}</TableCell>
                    <TableCell>{item.activityCount}</TableCell>
                    <TableCell>{item.unresolvedConflictCount}</TableCell>
                    <TableCell>
                      <Button
                        size="small"
                        variant="outlined"
                        startIcon={<VisibilityOutlinedIcon />}
                        onClick={() => navigate(`/admin/schedules/${item.scheduleId}`)}
                        sx={{ borderRadius: 9999, fontSize: '0.78rem' }}
                      >
                        View
                      </Button>
                    </TableCell>
                  </TableRow>
                ))
              )}
            </TableBody>
          </Table>
        </TableContainer>

        <TablePagination
          component="div"
          count={query.data?.totalCount || 0}
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
