import { useNavigate } from 'react-router-dom';
import {
  Alert,
  Box,
  Button,
  CircularProgress,
  Stack,
  Typography,
} from '@mui/material';
import { useAuthStore } from '../../../shared/store/authStore';
import CollapsibleSidebar from '../../../shared/components/layout/CollapsibleSidebar';
import TopSearchNavbar from '../../../shared/components/layout/TopSearchNavbar';
import SurfaceCard from '../../../shared/components/ui/SurfaceCard';
import StatusBadge from '../../../shared/components/ui/StatusBadge';
import {
  formatDateTime,
  formatQuotedPrice,
  formatQuotationStatus,
  useMyQuotations,
} from '../api/quotationApi';

export default function MyQuotationsPage() {
  const navigate = useNavigate();
  const user = useAuthStore((state) => state.user);
  const logout = useAuthStore((state) => state.logout);
  const isEventPlanner = useAuthStore((state) => state.hasRole('EVENT_PLANNER'));
  const isAdmin = useAuthStore((state) => state.hasRole('ADMIN'));
  const isVendor = useAuthStore((state) => state.hasRole('VENDOR'));
  const { data: quotations = [], isLoading, isError, error } = useMyQuotations();

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  return (
    <Box sx={{ minHeight: '100vh', backgroundColor: '#F7F3E9', p: { xs: 1.5, md: 2.5 } }}>
      <Box sx={{ display: 'flex', gap: { xs: 2, md: 3 }, minHeight: 'calc(100vh - 32px)' }}>
        <CollapsibleSidebar
          activeTab="my-quotations"
          showEvents={isAdmin}
          showMarketplace={isEventPlanner || isAdmin}
          showMyQuotations={isEventPlanner}
          showVendorProfile={isVendor}
          showVendorServices={isVendor}
          showVendorAvailability={isVendor}
          showVendorQuotations={isVendor}
          onSelectTab={(tab) => {
            if (tab === 'dashboard') navigate('/dashboard');
            if (tab === 'events') navigate('/admin/events');
            if (tab === 'marketplace') navigate('/marketplace');
            if (tab === 'my-quotations') navigate('/quotations/mine');
            if (tab === 'vendor-profile') navigate('/vendor/profile');
            if (tab === 'vendor-services') navigate('/vendor/services');
            if (tab === 'vendor-availability') navigate('/vendor/availability');
            if (tab === 'vendor-quotations') navigate('/vendor/quotations');
          }}
          onLogout={handleLogout}
        />

        <Box sx={{ flex: 1, minWidth: 0 }}>
          <TopSearchNavbar
            user={user}
            title="My quotations"
            subtitle="View-only status of your vendor quotation requests"
          />

          <Button variant="text" sx={{ mb: 2 }} onClick={() => navigate('/marketplace')}>
            Browse marketplace
          </Button>

          {isLoading && (
            <Box sx={{ display: 'flex', justifyContent: 'center', mt: 6 }}>
              <CircularProgress />
            </Box>
          )}

          {isError && (
            <Alert severity="error">{error?.message || 'Failed to load quotations.'}</Alert>
          )}

          {!isLoading && !isError && quotations.length === 0 && (
            <SurfaceCard sx={{ p: 3 }}>
              <Typography color="text.secondary">
                You have not requested any quotations yet.
              </Typography>
            </SurfaceCard>
          )}

          <Stack spacing={2} sx={{ maxWidth: 920 }}>
            {quotations.map((q) => (
              <SurfaceCard key={q.id} sx={{ p: 3 }}>
                <Box sx={{ display: 'flex', justifyContent: 'space-between', gap: 2, flexWrap: 'wrap' }}>
                  <Box>
                    <Typography sx={{ fontWeight: 800, fontSize: 18 }}>{q.vendorBusinessName}</Typography>
                    <Typography color="text.secondary">{q.serviceName}</Typography>
                  </Box>
                  <StatusBadge
                    label={formatQuotationStatus(q.status)}
                    status={q.status === 'REQUESTED' ? 'PENDING' : q.status === 'QUOTED' ? 'APPROVED' : q.status}
                  />
                </Box>
                <Typography variant="body2" sx={{ mt: 1.5 }}>
                  Event: {q.eventType || '—'} · Guests: {q.guestCount ?? '—'}
                </Typography>
                <Typography variant="body2">
                  Requested: {formatDateTime(q.requestedStartDateTime)} →{' '}
                  {formatDateTime(q.requestedEndDateTime)}
                </Typography>
                <Typography variant="body2" sx={{ mt: 1 }}>
                  Message: {q.customerMessage || '—'}
                </Typography>
                <Typography variant="body2" sx={{ mt: 1, fontWeight: 700 }}>
                  Quoted price: {formatQuotedPrice(q.quotedPrice)}
                </Typography>
                <Typography variant="body2">Vendor terms: {q.vendorTerms || '—'}</Typography>
              </SurfaceCard>
            ))}
          </Stack>
        </Box>
      </Box>
    </Box>
  );
}
