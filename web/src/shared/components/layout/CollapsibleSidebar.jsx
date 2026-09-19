import { useState } from 'react';
import { Box, IconButton, Typography } from '@mui/material';

const Icons = {
  Dashboard: () => <span aria-hidden="true">▦</span>,
  Events: () => <span aria-hidden="true">◫</span>,
  Profile: () => <span aria-hidden="true">◯</span>,
  Logout: () => <span aria-hidden="true">↪</span>,
  ChevronLeft: () => <span aria-hidden="true">‹</span>,
  ChevronRight: () => <span aria-hidden="true">›</span>,
};

export default function CollapsibleSidebar({
  activeTab = 'dashboard',
  showEvents = false,
  showVendorProfile = false,
  onSelectTab,
  onLogout,
}) {
  const [collapsed, setCollapsed] = useState(false);
  const items = [
    { id: 'dashboard', label: 'Dashboard', icon: Icons.Dashboard },
    ...(showEvents ? [{ id: 'events', label: 'Events', icon: Icons.Events }] : []),
    ...(showVendorProfile ? [{ id: 'vendor-profile', label: 'Vendor profile', icon: Icons.Profile }] : []),
  ];

  return (
    <Box sx={{
      width: collapsed ? 76 : 220,
      backgroundColor: '#19191C',
      color: '#FFFFFF',
      borderRadius: '24px',
      p: '20px 14px',
      display: 'flex',
      flexDirection: 'column',
      justifyContent: 'space-between',
      transition: 'width 0.25s ease',
      flexShrink: 0,
      boxShadow: '0 10px 30px rgba(0,0,0,0.18)',
      minHeight: 'calc(100vh - 40px)',
    }}>
      <Box>
        <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: collapsed ? 'center' : 'space-between', mb: 4, px: collapsed ? 0 : 1 }}>
          {!collapsed && <Typography variant="h5" sx={{ fontWeight: 700, color: '#FFFFFF' }}>Plan It</Typography>}
          <IconButton onClick={() => setCollapsed((value) => !value)} size="small" sx={{ backgroundColor: '#F9BFD8', color: '#19191C', width: 26, height: 26 }}>
            {collapsed ? <Icons.ChevronRight /> : <Icons.ChevronLeft />}
          </IconButton>
        </Box>
        <Typography sx={{ fontSize: 11, fontWeight: 600, textTransform: 'uppercase', letterSpacing: '0.08em', color: '#6E6E78', px: 1.5, mb: 1.2 }}>
          Workspace
        </Typography>
        <Box sx={{ display: 'flex', flexDirection: 'column', gap: 0.5 }}>
          {items.map((item) => {
            const Icon = item.icon;
            const isActive = activeTab === item.id;
            return (
              <Box
                key={item.id}
                onClick={() => onSelectTab?.(item.id)}
                sx={{
                  display: 'flex',
                  alignItems: 'center',
                  gap: 1.5,
                  px: 1.5,
                  py: 1,
                  borderRadius: '12px',
                  cursor: 'pointer',
                  backgroundColor: isActive ? '#28282E' : 'transparent',
                  color: isActive ? '#FFFFFF' : '#8E8E98',
                  justifyContent: collapsed ? 'center' : 'flex-start',
                  '&:hover': { backgroundColor: '#2E2E36', color: '#FFFFFF' },
                }}
              >
                <Icon />
                {!collapsed && <Typography sx={{ fontSize: 13, fontWeight: isActive ? 600 : 500 }}>{item.label}</Typography>}
              </Box>
            );
          })}
        </Box>
      </Box>
      <Box onClick={onLogout} sx={{ display: 'flex', alignItems: 'center', gap: 1.5, px: 1.5, py: 1, borderRadius: '12px', cursor: 'pointer', color: '#8E8E98', justifyContent: collapsed ? 'center' : 'flex-start', '&:hover': { backgroundColor: '#2E2E36', color: '#FFFFFF' } }}>
        <Icons.Logout />
        {!collapsed && <Typography sx={{ fontSize: 13, fontWeight: 500 }}>Sign out</Typography>}
      </Box>
    </Box>
  );
}
