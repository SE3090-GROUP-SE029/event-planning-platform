import { Box } from '@mui/material';
import { useNavigate } from 'react-router-dom';
import { useAuthStore } from '../../../shared/store/authStore';
import CollapsibleSidebar from '../../../shared/components/layout/CollapsibleSidebar';
import TopSearchNavbar from '../../../shared/components/layout/TopSearchNavbar';

export default function VendorWorkspaceLayout({ activeTab, title, subtitle, children }) {
  const navigate = useNavigate();
  const user = useAuthStore((state) => state.user);
  const logout = useAuthStore((state) => state.logout);

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  return (
    <Box sx={{ minHeight: '100vh', backgroundColor: '#F7F3E9', p: { xs: 1.5, md: 2.5 } }}>
      <Box sx={{ display: 'flex', gap: { xs: 2, md: 3 }, minHeight: 'calc(100vh - 32px)' }}>
        <CollapsibleSidebar
          activeTab={activeTab}
          showVendorProfile
          showVendorServices
          showVendorAvailability
          showVendorQuotations
          onSelectTab={(tab) => {
            if (tab === 'dashboard') navigate('/dashboard');
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
            title={title}
            subtitle={subtitle || user?.email || 'Vendor workspace'}
          />
          {children}
        </Box>
      </Box>
    </Box>
  );
}
