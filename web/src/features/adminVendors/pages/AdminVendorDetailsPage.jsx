import { useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import {
  Alert,
  Box,
  Button,
  CircularProgress,
  Grid,
  Stack,
  Typography,
} from '@mui/material';
import { useAuthStore } from '../../../shared/store/authStore';
import CollapsibleSidebar from '../../../shared/components/layout/CollapsibleSidebar';
import TopSearchNavbar from '../../../shared/components/layout/TopSearchNavbar';
import SurfaceCard from '../../../shared/components/ui/SurfaceCard';
import StatusBadge from '../../../shared/components/ui/StatusBadge';
import {
  formatVendorStatus,
  useAdminVendor,
  useApproveAdminVendor,
  useRestoreAdminVendor,
  useSuspendAdminVendor,
} from '../api/adminVendorApi';

function Detail({ label, value }) {
  return (
    <Box>
      <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mb: 0.5 }}>
        {label}
      </Typography>
      <Typography sx={{ wordBreak: 'break-word', fontWeight: 500 }}>{value || '—'}</Typography>
    </Box>
  );
}

export default function AdminVendorDetailsPage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const user = useAuthStore((state) => state.user);
  const logout = useAuthStore((state) => state.logout);
  const query = useAdminVendor(id);
  const approveMutation = useApproveAdminVendor();
  const suspendMutation = useSuspendAdminVendor();
  const restoreMutation = useRestoreAdminVendor();
  const [actionError, setActionError] = useState('');
  const [busy, setBusy] = useState(false);

  const vendor = query.data;

  const runAction = async (action) => {
    setActionError('');
    setBusy(true);
    try {
      await action({ id });
    } catch (err) {
      setActionError(err?.response?.data?.message || err?.message || 'Action failed.');
    } finally {
      setBusy(false);
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
        <TopSearchNavbar user={user} title="Vendor details" subtitle="Admin review" />

        <Button variant="text" sx={{ mb: 2 }} onClick={() => navigate('/admin/vendors')}>
          ← Back to vendor directory
        </Button>

        {query.isLoading && (
          <Box sx={{ display: 'grid', placeItems: 'center', minHeight: '40vh' }}>
            <CircularProgress />
          </Box>
        )}

        {query.isError && (
          <Alert severity="error">{query.error?.message || 'Vendor not found.'}</Alert>
        )}

        {actionError && (
          <Alert severity="error" sx={{ mb: 2 }} onClose={() => setActionError('')}>
            {actionError}
          </Alert>
        )}

        {vendor && (
          <Stack spacing={2} sx={{ maxWidth: 900 }}>
            <SurfaceCard sx={{ p: 3 }}>
              <Stack direction="row" justifyContent="space-between" alignItems="flex-start" spacing={2} flexWrap="wrap">
                <Box>
                  <Typography variant="h4" sx={{ fontWeight: 800 }}>
                    {vendor.businessName}
                  </Typography>
                  <Typography color="text.secondary">{vendor.category}</Typography>
                </Box>
                <StatusBadge label={formatVendorStatus(vendor.status)} status={vendor.status} />
              </Stack>

              <Grid container spacing={2} sx={{ mt: 1 }}>
                <Grid item xs={12} md={6}>
                  <Detail label="Contact email" value={vendor.contactEmail} />
                </Grid>
                <Grid item xs={12} md={6}>
                  <Detail label="Contact phone" value={vendor.contactPhone} />
                </Grid>
                <Grid item xs={12}>
                  <Detail label="Address" value={vendor.address} />
                </Grid>
                <Grid item xs={12}>
                  <Detail label="Website" value={vendor.websiteUrl} />
                </Grid>
                <Grid item xs={12}>
                  <Detail label="Description" value={vendor.description} />
                </Grid>
                <Grid item xs={12} md={6}>
                  <Detail label="Vendor id" value={vendor.id} />
                </Grid>
                <Grid item xs={12} md={6}>
                  <Detail label="Owner user id" value={vendor.userId} />
                </Grid>
                <Grid item xs={12} md={6}>
                  <Detail
                    label="Created"
                    value={vendor.createdAt ? new Date(vendor.createdAt).toLocaleString() : null}
                  />
                </Grid>
                <Grid item xs={12} md={6}>
                  <Detail
                    label="Updated"
                    value={vendor.updatedAt ? new Date(vendor.updatedAt).toLocaleString() : null}
                  />
                </Grid>
              </Grid>
            </SurfaceCard>

            <Stack direction="row" spacing={1}>
              {vendor.status === 'PENDING' && (
                <Button
                  variant="contained"
                  disabled={busy}
                  onClick={() => runAction(approveMutation.mutateAsync)}
                >
                  Approve vendor
                </Button>
              )}
              {vendor.status === 'APPROVED' && (
                <Button
                  variant="outlined"
                  color="warning"
                  disabled={busy}
                  onClick={() => runAction(suspendMutation.mutateAsync)}
                >
                  Suspend vendor
                </Button>
              )}
              {vendor.status === 'SUSPEND' && (
                <Button
                  variant="contained"
                  disabled={busy}
                  onClick={() => runAction(restoreMutation.mutateAsync)}
                >
                  Restore to approved
                </Button>
              )}
            </Stack>
          </Stack>
        )}
      </Box>
    </Box>
  );
}
