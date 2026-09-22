import { Box, Typography } from '@mui/material';
import { useNavigate } from 'react-router-dom';
import { useAuthStore } from '../../../shared/store/authStore';
import CollapsibleSidebar from '../../../shared/components/layout/CollapsibleSidebar';
import TopSearchNavbar from '../../../shared/components/layout/TopSearchNavbar';



export default function DashboardPage() {
  const navigate = useNavigate();
  const user = useAuthStore((state) => state.user);
  const logout = useAuthStore((state) => state.logout);
  const roles = user?.roles || [];
  const isAdmin = roles.includes('ADMIN');
  const isVendor = roles.includes('VENDOR');

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  return (
    <Box sx={{ minHeight: '100vh', backgroundColor: '#F7F3E9', p: { xs: 1.5, md: 2.5 } }}>
      <Box sx={{ display: 'flex', gap: { xs: 2, md: 3 }, minHeight: 'calc(100vh - 32px)' }}>
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
          <TopSearchNavbar user={user} title="Welcome back" subtitle={user?.email || 'Workspace'} />

          <Box sx={{ mb: 3 }}>
            <Typography variant="h3" sx={{ fontWeight: 800, letterSpacing: '-0.04em', mb: 0.5 }}>
              {user?.email ? `Hello, ${user.email.split('@')[0]}` : 'Hello'}
            </Typography>
            <Typography variant="body1" color="text.secondary">
              Use the available workspace tools to manage your active event planning tasks.
            </Typography>
          </Box>

          

          
        </Box>
      </Box>
    </Box>
  );
}
