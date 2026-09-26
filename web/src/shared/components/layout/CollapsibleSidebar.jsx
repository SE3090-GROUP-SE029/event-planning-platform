import { useState } from 'react';
import { Box, IconButton, Typography } from '@mui/material';
import HomeFilledIcon from '@mui/icons-material/HomeFilled';
import EventAvailableIcon from '@mui/icons-material/EventAvailable';
import FaceIcon from '@mui/icons-material/Face';
import HandymanOutlinedIcon from '@mui/icons-material/HandymanOutlined';
import ScheduleOutlinedIcon from '@mui/icons-material/ScheduleOutlined';
import StorefrontOutlinedIcon from '@mui/icons-material/StorefrontOutlined';
import MeetingRoomIcon from '@mui/icons-material/MeetingRoom';
import AssignmentOutlinedIcon from '@mui/icons-material/AssignmentOutlined';
import ChevronLeftIcon from '@mui/icons-material/ChevronLeft';
import ChevronRightIcon from '@mui/icons-material/ChevronRight';

const Icons = {
  Dashboard: () => <HomeFilledIcon />,
  Events: () => <EventAvailableIcon/>,
  Marketplace: () => <StorefrontOutlinedIcon />,
  Profile: () => <FaceIcon/>,
  Services: () => <HandymanOutlinedIcon />,
  Availability: () => <ScheduleOutlinedIcon />,
  Plans: () => <AssignmentOutlinedIcon />,
  Logout: () => <MeetingRoomIcon/>,
  ChevronLeft: () => <ChevronLeftIcon/>,
  ChevronRight: () => <ChevronRightIcon/>,
};

export default function CollapsibleSidebar({
  activeTab = 'dashboard',
  showEvents = false,
  showMarketplace = false,
  showVendorProfile = false,
  showVendorServices = false,
  showVendorAvailability = false,
  showPlanMonitoring = false,
  onSelectTab,
  onLogout,
}) {
  const [collapsed, setCollapsed] = useState(false);
  const items = [
    { id: 'dashboard', label: 'Dashboard', icon: Icons.Dashboard },
    ...(showEvents ? [{ id: 'events', label: 'Events', icon: Icons.Events }] : []),
    ...(showMarketplace ? [{ id: 'marketplace', label: 'Marketplace', icon: Icons.Marketplace }] : []),
    ...(showVendorProfile ? [{ id: 'vendor-profile', label: 'Vendor profile', icon: Icons.Profile }] : []),
    ...(showVendorServices ? [{ id: 'vendor-services', label: 'Services', icon: Icons.Services }] : []),
    ...(showVendorAvailability ? [{ id: 'vendor-availability', label: 'Availability', icon: Icons.Availability }] : []),
    ...(showPlanMonitoring ? [{ id: 'plans', label: 'Plan monitoring', icon: Icons.Plans }] : []),
  ];

  return (
    <Box
      sx={{
        width: collapsed ? 84 : 220,
        background: 'linear-gradient(180deg, #19191C 0%, #1F1F24 100%)',
        color: '#FFFFFF',
        borderRadius: '28px',
        p: '18px 12px 14px',
        display: 'flex',
        flexDirection: 'column',
        justifyContent: 'space-between',
        transition: 'width 0.25s ease',
        flexShrink: 0,
        boxShadow: '0 18px 40px rgba(17, 16, 20, 0.18)',
        minHeight: 'calc(100vh - 32px)',
      }}
    >
      <Box>
        <Box
          sx={{
            display: 'flex',
            alignItems: 'center',
            justifyContent: collapsed ? 'center' : 'space-between',
            mb: 3.5,
            px: collapsed ? 0 : 1.25,
          }}
        >
          {!collapsed && (
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.2 }}>
              <Box
                sx={{
                  width: 28,
                  height: 28,
                  borderRadius: '50%',
                  background: 'linear-gradient(135deg, #F9BFD8 0%, #FDEEF5 100%)',
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  color: '#19191C',
                  fontWeight: 800,
                  fontSize: 12,
                }}
              >
                P
              </Box>
              <Typography variant="h6" sx={{ fontWeight: 800, color: '#FFFFFF', letterSpacing: '-0.04em' }}>
                Plan It
              </Typography>
            </Box>
          )}
          <IconButton
            onClick={() => setCollapsed((value) => !value)}
            size="small"
            sx={{
              backgroundColor: '#F9BFD8',
              color: '#19191C',
              width: 28,
              height: 28,
              borderRadius: '50%',
              '&:hover': { backgroundColor: '#F5C9DF' },
            }}
          >
            {collapsed ? <Icons.ChevronRight /> : <Icons.ChevronLeft />}
          </IconButton>
        </Box>

        <Typography
          sx={{
            fontSize: 10,
            fontWeight: 700,
            textTransform: 'uppercase',
            letterSpacing: '0.12em',
            color: '#86868D',
            px: 1,
            mb: 1.2,
          }}
        >
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
                  gap: 1.4,
                  px: 1.5,
                  py: 1.2,
                  borderRadius: '14px',
                  cursor: 'pointer',
                  backgroundColor: isActive ? 'rgba(255,255,255,0.08)' : 'transparent',
                  color: isActive ? '#FFFFFF' : '#AEAEB2',
                  justifyContent: collapsed ? 'center' : 'flex-start',
                  border: isActive ? '1px solid rgba(255,255,255,0.08)' : '1px solid transparent',
                  transition: 'all 0.2s ease',
                  '&:hover': { backgroundColor: 'rgba(255,255,255,0.06)', color: '#FFFFFF' },
                }}
              >
                <Box sx={{ fontSize: 18, lineHeight: 1 }}>{<Icon />}</Box>
                {!collapsed && (
                  <Typography sx={{ color: isActive ? '#FFFFFF' : '#AEAEB2',  fontSize: 13.5, fontWeight: isActive ? 700 : 500 }}>{item.label}</Typography>
                )}
              </Box>
            );
          })}
        </Box>
      </Box>

      <Box
        onClick={onLogout}
        sx={{
          display: 'flex',
          alignItems: 'center',
          gap: 1.5,
          px: 1.5,
          py: 1.2,
          borderRadius: '14px',
          cursor: 'pointer',
          color: '#A0A0A8',
          justifyContent: collapsed ? 'center' : 'flex-start',
          transition: 'all 0.2s ease',
          '&:hover': { backgroundColor: 'rgba(255, 255, 255, 0.44)', color: '#FFFFFF' },
        }}
      >
        <Icons.Logout />
        {!collapsed && <Typography sx={{color: '#AEAEB2', fontSize: 13.5, fontWeight: 500 }}>Sign out</Typography>}
      </Box>
    </Box>
  );
}
