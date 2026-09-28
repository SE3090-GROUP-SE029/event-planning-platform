import { useState } from 'react';
import {
  Box,
  Button,
  IconButton,
  Tooltip,
  Typography,
} from '@mui/material';
import HomeFilledIcon from '@mui/icons-material/HomeFilled';
import EventAvailableIcon from '@mui/icons-material/EventAvailable';
import MeetingRoomIcon from '@mui/icons-material/MeetingRoom';
import ChevronLeftIcon from '@mui/icons-material/ChevronLeft';
import ChevronRightIcon from '@mui/icons-material/ChevronRight';
import InsightsOutlinedIcon from '@mui/icons-material/InsightsOutlined';
import PeopleAltOutlinedIcon from '@mui/icons-material/PeopleAltOutlined';
import DnsOutlinedIcon from '@mui/icons-material/DnsOutlined';
import AutoAwesomeOutlinedIcon from '@mui/icons-material/AutoAwesomeOutlined';
import StorefrontOutlinedIcon from '@mui/icons-material/StorefrontOutlined';
import { NavLink, useLocation, useNavigate } from 'react-router-dom';
import { useAuthStore } from '../../store/authStore';
import { tokens } from '../../theme/tokens';

const adminItems = [
  { id: 'dashboard', label: 'Dashboard', path: '/admin/dashboard', icon: HomeFilledIcon },
  { id: 'analytics', label: 'Analytics', path: '/admin/analytics', icon: InsightsOutlinedIcon },
  { id: 'users', label: 'Users', path: '/admin/users', icon: PeopleAltOutlinedIcon },
  { id: 'vendors', label: 'Vendors', path: '/admin/vendors', icon: StorefrontOutlinedIcon },
  { id: 'events', label: 'Events', path: '/admin/events', icon: EventAvailableIcon },
  { id: 'plans', label: 'AI Plans', path: '/admin/plans', icon: AutoAwesomeOutlinedIcon },
  { id: 'system', label: 'Monitoring', path: '/admin/system', icon: DnsOutlinedIcon },
];

export default function CollapsibleSidebar({ activeTab, onLogout }) {
  const [collapsed, setCollapsed] = useState(false);
  const location = useLocation();
  const navigate = useNavigate();
  const storeLogout = useAuthStore((state) => state.logout);

  const handleLogout = () => {
    if (onLogout) {
      onLogout();
      return;
    }
    storeLogout();
    navigate('/');
  };

  return (
    <Box
      component="aside"
      sx={{
        width: collapsed ? 76 : 232,
        bgcolor: tokens.colors.surface,
        color: tokens.colors.textPrimary,
        borderRadius: tokens.radius.card,
        border: `1px solid ${tokens.colors.borderLight}`,
        p: 1.5,
        display: 'flex',
        flexDirection: 'column',
        justifyContent: 'space-between',
        transition: 'width 0.2s ease',
        flexShrink: 0,
        boxShadow: tokens.shadows.soft,
        minHeight: 'calc(100vh - 32px)',
        position: { md: 'sticky' },
        top: { md: 16 },
        alignSelf: 'flex-start',
      }}
    >
      <Box>
        <Box
          sx={{
            display: 'flex',
            alignItems: 'center',
            justifyContent: collapsed ? 'center' : 'space-between',
            mb: 3,
            px: 0.5,
          }}
        >
          {!collapsed && (
            <Box
              component={NavLink}
              to="/admin/dashboard"
              sx={{
                display: 'flex',
                alignItems: 'center',
                gap: 1,
                color: tokens.colors.textPrimary,
                textDecoration: 'none',
              }}
            >
              <Box
                sx={{
                  width: 34,
                  height: 34,
                  borderRadius: tokens.radius.sm,
                  bgcolor: tokens.colors.pastelBlue,
                  color: tokens.colors.pastelBlueText,
                  display: 'grid',
                  placeItems: 'center',
                  fontWeight: 800,
                }}
              >
                P
              </Box>
              <Typography variant="h6" sx={{ fontWeight: 800 }}>
                Plan It
              </Typography>
            </Box>
          )}
          <IconButton
            aria-label={collapsed ? 'Expand navigation' : 'Collapse navigation'}
            onClick={() => setCollapsed((value) => !value)}
            size="small"
            sx={{
              bgcolor: tokens.colors.pastelBlueLight,
              color: tokens.colors.pastelBlueText,
              '&:hover': { bgcolor: tokens.colors.pastelBlue },
            }}
          >
            {collapsed ? <ChevronRightIcon fontSize="small" /> : <ChevronLeftIcon fontSize="small" />}
          </IconButton>
        </Box>

        {!collapsed && (
          <Typography
            sx={{
              fontSize: 11,
              fontWeight: 800,
              textTransform: 'uppercase',
              letterSpacing: '0.08em',
              color: tokens.colors.textMuted,
              px: 1,
              mb: 1,
            }}
          >
            Administration
          </Typography>
        )}

        <Box component="nav" aria-label="Admin navigation" sx={{ display: 'flex', flexDirection: 'column', gap: 0.5 }}>
          {adminItems.map((item) => {
            const Icon = item.icon;
            const isActive = activeTab
              ? activeTab === item.id || (item.id === 'vendors' && activeTab === 'admin-vendors')
              : location.pathname === item.path || location.pathname.startsWith(`${item.path}/`);
            const link = (
              <Box
                component={NavLink}
                key={item.id}
                to={item.path}
                aria-current={isActive ? 'page' : undefined}
                sx={{
                  display: 'flex',
                  alignItems: 'center',
                  gap: 1.25,
                  px: 1.25,
                  py: 1.1,
                  minHeight: 44,
                  borderRadius: tokens.radius.sm,
                  textDecoration: 'none',
                  bgcolor: isActive ? tokens.colors.pastelBlueLight : 'transparent',
                  color: isActive ? tokens.colors.pastelBlueText : tokens.colors.textSecondary,
                  justifyContent: collapsed ? 'center' : 'flex-start',
                  '&:hover': { bgcolor: tokens.colors.pastelBlueLight },
                }}
              >
                <Icon fontSize="small" />
                {!collapsed && (
                  <Typography sx={{ color: 'inherit', fontSize: 14, fontWeight: isActive ? 700 : 500 }}>
                    {item.label}
                  </Typography>
                )}
              </Box>
            );
            return collapsed ? (
              <Tooltip key={item.id} title={item.label} placement="right">
                {link}
              </Tooltip>
            ) : link;
          })}
        </Box>
      </Box>

      <Button
        onClick={handleLogout}
        startIcon={!collapsed && <MeetingRoomIcon fontSize="small" />}
        aria-label={collapsed ? 'Sign out' : undefined}
        sx={{
          justifyContent: collapsed ? 'center' : 'flex-start',
          minHeight: 44,
          color: tokens.colors.textSecondary,
          '&:hover': { bgcolor: tokens.colors.pastelPinkLight, color: tokens.colors.pastelPinkText },
        }}
      >
        {collapsed ? <MeetingRoomIcon fontSize="small" /> : 'Sign out'}
      </Button>
    </Box>
  );
}
