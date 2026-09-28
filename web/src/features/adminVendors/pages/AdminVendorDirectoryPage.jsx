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
import {
  VENDOR_CATEGORIES,
  VENDOR_STATUSES,
  formatVendorStatus,
  useAdminVendors,
  useApproveAdminVendor,
  useRestoreAdminVendor,
  useSuspendAdminVendor,
} from '../api/adminVendorApi';

function formatDate(value) {
  return value ? new Date(value).toLocaleDateString() : '—';
}

export default function AdminVendorDirectoryPage() {
  const navigate = useNavigate();
  const user = useAuthStore((state) => state.user);
  const logout = useAuthStore((state) => state.logout);
  const [page, setPage] = useState(0);
  const [pageSize, setPageSize] = useState(10);
  const [filters, setFilters] = useState({ search: '', status: '', category: '' });
  const [actionError, setActionError] = useState('');
  const [busyId, setBusyId] = useState(null);

  const params = useMemo(
    () => ({
      search: filters.search || undefined,
      status: filters.status || undefined,
      category: filters.category || undefined,
      page: page + 1,
      pageSize,
      sortBy: 'createdAt',
      sortOrder: 'desc',
    }),
    [filters, page, pageSize],
  );

  const query = useAdminVendors(params);
  const approveMutation = useApproveAdminVendor();
  const suspendMutation = useSuspendAdminVendor();
  const restoreMutation = useRestoreAdminVendor();
  const items = query.data?.items || [];
  const totalCount = query.data?.totalCount || 0;

  const setFilter = (name) => (event) => {
    setPage(0);
    setFilters((current) => ({ ...current, [name]: event.target.value }));
  };

  const runAction = async (vendorId, action) => {
    setActionError('');
    setBusyId(vendorId);
    try {
      await action({ id: vendorId });
    } catch (err) {
      setActionError(err?.response?.data?.message || err?.message || 'Action failed.');
    } finally {
      setBusyId(null);
    }
  };

  return (
    <Box sx={{ display: 'flex', minHeight: '100vh', backgroundColor: '#F7F3E9', p: { xs: 1.5, md: 2.5 }, gap: { xs: 2, md: 3 } }}>
      <CollapsibleSidebar
        activeTab="vendors"
        showEvents
        showVendors
        showPlanMonitoring
        onSelectTab={(tab) => {
          if (tab === 'dashboard') navigate('/dashboard');
          if (tab === 'events') navigate('/admin/events');
          if (tab === 'vendors') navigate('/admin/vendors');
          if (tab === 'plans') navigate('/admin/plans');
        }}
        onLogout={() => {
          logout();
          navigate('/login');
        }}
      />

      <Box sx={{ flex: 1, minWidth: 0 }}>
        <TopSearchNavbar
          user={user}
          title="Vendors"
          subtitle="Approve, suspend, and restore marketplace vendors"
          onSearch={(value) => {
            setPage(0);
            setFilters((current) => ({ ...current, search: value }));
          }}
        />

        <Stack direction={{ xs: 'column', md: 'row' }} justifyContent="space-between" alignItems={{ md: 'center' }} sx={{ mb: 2.5 }}>
          <Box>
            <Typography variant="h3" sx={{ fontWeight: 800, letterSpacing: '-0.04em', mb: 0.5 }}>
              Vendor directory
            </Typography>
            <Typography color="text.secondary">
              Only APPROVED vendors appear in the Event Planner marketplace.
            </Typography>
          </Box>
        </Stack>

        <SurfaceCard sx={{ p: 2, mb: 2 }}>
          <Stack direction={{ xs: 'column', md: 'row' }} spacing={1.5}>
            <TextField
              size="small"
              label="Search"
              value={filters.search}
              onChange={setFilter('search')}
              placeholder="Business name, email, or id"
              sx={{ minWidth: 220 }}
            />
            <FormControl size="small" sx={{ minWidth: 160 }}>
              <InputLabel>Status</InputLabel>
              <Select label="Status" value={filters.status} onChange={setFilter('status')}>
                <MenuItem value="">All</MenuItem>
                {VENDOR_STATUSES.map((status) => (
                  <MenuItem key={status} value={status}>
                    {formatVendorStatus(status)}
                  </MenuItem>
                ))}
              </Select>
            </FormControl>
            <FormControl size="small" sx={{ minWidth: 180 }}>
              <InputLabel>Category</InputLabel>
              <Select label="Category" value={filters.category} onChange={setFilter('category')}>
                <MenuItem value="">All</MenuItem>
                {VENDOR_CATEGORIES.map((category) => (
                  <MenuItem key={category} value={category}>
                    {category}
                  </MenuItem>
                ))}
              </Select>
            </FormControl>
          </Stack>
        </SurfaceCard>

        {actionError && (
          <Alert severity="error" sx={{ mb: 2 }} onClose={() => setActionError('')}>
            {actionError}
          </Alert>
        )}

        {query.isLoading && (
          <Box sx={{ display: 'flex', justifyContent: 'center', mt: 6 }}>
            <CircularProgress />
          </Box>
        )}

        {query.isError && (
          <Alert severity="error">{query.error?.message || 'Failed to load vendors.'}</Alert>
        )}

        {!query.isLoading && !query.isError && (
          <SurfaceCard>
            <TableContainer>
              <Table size="small">
                <TableHead>
                  <TableRow>
                    <TableCell>Business</TableCell>
                    <TableCell>Category</TableCell>
                    <TableCell>Contact</TableCell>
                    <TableCell>Status</TableCell>
                    <TableCell>Created</TableCell>
                    <TableCell align="right">Actions</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {items.length === 0 && (
                    <TableRow>
                      <TableCell colSpan={6}>
                        <Typography color="text.secondary" sx={{ py: 2 }}>
                          No vendors match the current filters.
                        </Typography>
                      </TableCell>
                    </TableRow>
                  )}
                  {items.map((vendor) => {
                    const busy = busyId === vendor.id;
                    return (
                      <TableRow key={vendor.id} hover>
                        <TableCell>
                          <Button
                            variant="text"
                            onClick={() => navigate(`/admin/vendors/${vendor.id}`)}
                            sx={{ fontWeight: 700, textTransform: 'none', px: 0 }}
                          >
                            {vendor.businessName}
                          </Button>
                          <Typography variant="caption" color="text.secondary" display="block">
                            {vendor.address}
                          </Typography>
                        </TableCell>
                        <TableCell>{vendor.category}</TableCell>
                        <TableCell>
                          <Typography variant="body2">{vendor.contactEmail}</Typography>
                          <Typography variant="caption" color="text.secondary">
                            {vendor.contactPhone}
                          </Typography>
                        </TableCell>
                        <TableCell>
                          <StatusBadge label={formatVendorStatus(vendor.status)} status={vendor.status} />
                        </TableCell>
                        <TableCell>{formatDate(vendor.createdAt)}</TableCell>
                        <TableCell align="right">
                          <Stack direction="row" spacing={1} justifyContent="flex-end">
                            {vendor.status === 'PENDING' && (
                              <Button
                                size="small"
                                variant="contained"
                                disabled={busy}
                                onClick={() => runAction(vendor.id, approveMutation.mutateAsync)}
                              >
                                Approve
                              </Button>
                            )}
                            {vendor.status === 'APPROVED' && (
                              <Button
                                size="small"
                                color="warning"
                                variant="outlined"
                                disabled={busy}
                                onClick={() => runAction(vendor.id, suspendMutation.mutateAsync)}
                              >
                                Suspend
                              </Button>
                            )}
                            {vendor.status === 'SUSPEND' && (
                              <Button
                                size="small"
                                variant="contained"
                                disabled={busy}
                                onClick={() => runAction(vendor.id, restoreMutation.mutateAsync)}
                              >
                                Restore
                              </Button>
                            )}
                            <Button size="small" variant="text" onClick={() => navigate(`/admin/vendors/${vendor.id}`)}>
                              Details
                            </Button>
                          </Stack>
                        </TableCell>
                      </TableRow>
                    );
                  })}
                </TableBody>
              </Table>
            </TableContainer>
            <TablePagination
              component="div"
              count={totalCount}
              page={page}
              onPageChange={(_, next) => setPage(next)}
              rowsPerPage={pageSize}
              onRowsPerPageChange={(event) => {
                setPageSize(Number(event.target.value));
                setPage(0);
              }}
              rowsPerPageOptions={[5, 10, 25]}
            />
          </SurfaceCard>
        )}
      </Box>
    </Box>
  );
}
