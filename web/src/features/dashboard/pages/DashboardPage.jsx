import { Box, Button, Card, CardContent, Stack, Typography } from '@mui/material';
import { useNavigate } from 'react-router-dom';
import { useAuthStore } from '../../../shared/store/authStore';
import CollapsibleSidebar from '../../../shared/components/layout/CollapsibleSidebar';
import TopSearchNavbar from '../../../shared/components/layout/TopSearchNavbar';

export default function DashboardPage() {
  const navigate = useNavigate();
  const user = useAuthStore((state) => state.user);
  const logout = useAuthStore((state) => state.logout);
  const isAdmin = user?.roles?.includes('ADMIN');
  const isVendor = user?.roles?.includes('VENDOR');

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  return (
    <Box sx={{ minHeight: '100vh', backgroundColor: '#F7F3E9', p: { xs: 1.5, md: 2.5 } }}>
      <Box sx={{ display: 'flex', gap: { xs: 2, md: 3 }, minHeight: 'calc(100vh - 40px)' }}>
        <CollapsibleSidebar
          activeTab="dashboard"
          showEvents={isAdmin}
          showVendorProfile={isVendor}
          onSelectTab={(tab) => {
            if (tab === 'events') navigate('/admin/events');
            if (tab === 'vendor-profile') navigate('/vendor/profile');
          }}
          onLogout={handleLogout}
        />
        <Box sx={{ flex: 1, minWidth: 0 }}>
          <TopSearchNavbar user={user} />
          <Box sx={{ mb: 3 }}>
            <Typography variant="h4" fontWeight={700} sx={{ color: '#1E1E22' }}>
              Welcome back
            </Typography>
            <Typography color="text.secondary" sx={{ mt: 0.5 }}>
              Use the available sections below to manage data supported by the platform.
            </Typography>
          </Box>
          <Stack direction={{ xs: 'column', md: 'row' }} spacing={2}>
            {isAdmin && (
              <Card sx={{ flex: 1, borderRadius: 3 }}>
                <CardContent>
                  <Typography variant="h6" fontWeight={700}>Event management</Typography>
                  <Typography color="text.secondary" sx={{ mt: 1, mb: 2 }}>
                    Review and inspect events created on the platform.
                  </Typography>
                  <Button variant="contained" onClick={() => navigate('/admin/events')}>
                    Open events
                  </Button>
                </CardContent>
              </Card>
            )}
            {isVendor && (
              <Card sx={{ flex: 1, borderRadius: 3 }}>
                <CardContent>
                  <Typography variant="h6" fontWeight={700}>Vendor profile</Typography>
                  <Typography color="text.secondary" sx={{ mt: 1, mb: 2 }}>
                    View or update the vendor profile associated with your account.
                  </Typography>
                  <Button variant="contained" onClick={() => navigate('/vendor/profile')}>
                    Open profile
                  </Button>
                </CardContent>
              </Card>
            )}
            {!isAdmin && !isVendor && (
              <Card sx={{ borderRadius: 3, width: '100%' }}>
                <CardContent>
                  <Typography variant="h6" fontWeight={700}>No client workspace is available</Typography>
                  <Typography color="text.secondary" sx={{ mt: 1 }}>
                    Your account does not currently have a client-facing module enabled.
                  </Typography>
                </CardContent>
              </Card>
            )}
          </Stack>
        </Box>
      </Box>
    </Box>
  );
}
