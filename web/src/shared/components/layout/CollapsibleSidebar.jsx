import { useState } from 'react';
import { Box, Typography, Tooltip, IconButton } from '@mui/material';

// Inline clean SVG icons
const Icons = {
  Dashboard: () => (
    <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <rect x="3" y="3" width="7" height="7" rx="2" />
      <rect x="14" y="3" width="7" height="7" rx="2" />
      <rect x="14" y="14" width="7" height="7" rx="2" />
      <rect x="3" y="14" width="7" height="7" rx="2" />
    </svg>
  ),
  Schedule: () => (
    <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <rect x="3" y="4" width="18" height="18" rx="3" />
      <line x1="16" y1="2" x2="16" y2="6" />
      <line x1="8" y1="2" x2="8" y2="6" />
      <line x1="3" y1="10" x2="21" y2="10" />
    </svg>
  ),
  Events: () => (
    <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2" />
      <circle cx="9" cy="7" r="4" />
      <path d="M23 21v-2a4 4 0 0 0-3-3.87" />
      <path d="M16 3.13a4 4 0 0 1 0 7.75" />
    </svg>
  ),
  Reports: () => (
    <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <circle cx="12" cy="12" r="10" />
      <path d="M12 2a14.5 14.5 0 0 0 0 20 14.5 14.5 0 0 0 0-20" />
      <path d="M2 12h20" />
    </svg>
  ),
  Education: () => (
    <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <path d="M4 19.5A2.5 2.5 0 0 1 6.5 17H20" />
      <path d="M6.5 2H20v20H6.5A2.5 2.5 0 0 1 4 19.5v-15A2.5 2.5 0 0 1 6.5 2z" />
    </svg>
  ),
  Articles: () => (
    <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <path d="M19 21l-7-5-7 5V5a2 2 0 0 1 2-2h10a2 2 0 0 1 2 2z" />
    </svg>
  ),
  Chat: () => (
    <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <path d="M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z" />
    </svg>
  ),
  Billing: () => (
    <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <circle cx="12" cy="12" r="10" />
      <path d="M16 8h-6a2 2 0 1 0 0 4h4a2 2 0 1 1 0 4H8" />
      <path d="M12 18V6" />
    </svg>
  ),
  Documents: () => (
    <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z" />
      <polyline points="14 2 14 8 20 8" />
    </svg>
  ),
  Settings: () => (
    <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <circle cx="12" cy="12" r="3" />
      <path d="M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 0 1 0 2.83 2 2 0 0 1-2.83 0l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 0 1-2 2 2 2 0 0 1-2-2v-.09A1.65 1.65 0 0 0 9 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 0 1-2.83 0 2 2 0 0 1 0-2.83l.06-.06a1.65 1.65 0 0 0 .33-1.82 1.65 1.65 0 0 0-1.51-1H3a2 2 0 0 1-2-2 2 2 0 0 1 2-2h.09A1.65 1.65 0 0 0 4.6 9a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 0 1 0-2.83 2 2 0 0 1 2.83 0l.06.06a1.65 1.65 0 0 0 1.82.33H9a1.65 1.65 0 0 0 1-1.51V3a2 2 0 0 1 2-2 2 2 0 0 1 2 2v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 0 1 2.83 0 2 2 0 0 1 0 2.83l-.06.06a1.65 1.65 0 0 0-.33 1.82V9a1.65 1.65 0 0 0 1.51 1H21a2 2 0 0 1 2 2 2 2 0 0 1-2 2h-.09a1.65 1.65 0 0 0-1.51 1z" />
    </svg>
  ),
  Logout: () => (
    <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4" />
      <polyline points="16 17 21 12 16 7" />
      <line x1="21" y1="12" x2="9" y2="12" />
    </svg>
  ),
  ChevronLeft: () => (
    <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round">
      <polyline points="15 18 9 12 15 6" />
    </svg>
  ),
  ChevronRight: () => (
    <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round">
      <polyline points="9 18 15 12 9 6" />
    </svg>
  ),
};

const NAV_SECTIONS = [
  {
    title: 'General',
    items: [
      { id: 'dashboard', label: 'Dashboard', icon: Icons.Dashboard },
      { id: 'schedule', label: 'Schedule', icon: Icons.Schedule },
      { id: 'events', label: 'Events', icon: Icons.Events },
      { id: 'reports', label: 'Statistics & reports', icon: Icons.Reports },
      { id: 'education', label: 'Education', icon: Icons.Education },
      { id: 'articles', label: 'My articles', icon: Icons.Articles },
    ],
  },
  {
    title: 'Tools',
    items: [
      { id: 'chat', label: 'Chats & calls', icon: Icons.Chat },
      { id: 'billing', label: 'Billing', icon: Icons.Billing },
      { id: 'documents', label: 'Documents base', icon: Icons.Documents },
      { id: 'settings', label: 'Settings', icon: Icons.Settings },
    ],
  },
];

export default function CollapsibleSidebar({ activeTab = 'dashboard', onSelectTab, onLogout }) {
  const [collapsed, setCollapsed] = useState(false);

  return (
    <Box
      sx={{
        width: collapsed ? 76 : 220,
        backgroundColor: '#19191C',
        color: '#FFFFFF',
        borderRadius: '24px',
        p: '20px 14px',
        display: 'flex',
        flexDirection: 'column',
        justifyContent: 'space-between',
        transition: 'width 0.25s cubic-bezier(0.4, 0, 0.2, 1)',
        flexShrink: 0,
        boxShadow: '0 10px 30px rgba(0,0,0,0.18)',
        height: 'calc(100vh - 40px)',
        position: 'sticky',
        top: 20,
      }}
    >
      {/* Top Header & Brand */}
      <Box>
        <Box
          sx={{
            display: 'flex',
            alignItems: 'center',
            justifyContent: collapsed ? 'center' : 'space-between',
            mb: 4,
            px: collapsed ? 0 : 1,
          }}
        >
          {!collapsed && (
            <Typography
              variant="h5"
              sx={{
                fontWeight: 700,
                letterSpacing: '-0.03em',
                color: '#FFFFFF',
                fontFamily: '"Plus Jakarta Sans", sans-serif',
              }}
            >
              Plan It
            </Typography>
          )}

          <IconButton
            onClick={() => setCollapsed(!collapsed)}
            size="small"
            sx={{
              backgroundColor: '#F9BFD8',
              color: '#19191C',
              width: 24,
              height: 24,
              '&:hover': {
                backgroundColor: '#F7ADC9',
              },
            }}
          >
            {collapsed ? <Icons.ChevronRight /> : <Icons.ChevronLeft />}
          </IconButton>
        </Box>

        {/* Navigation Groups */}
        {NAV_SECTIONS.map((section) => (
          <Box key={section.title} sx={{ mb: 3 }}>
            {!collapsed && (
              <Typography
                sx={{
                  fontSize: '11px',
                  fontWeight: 600,
                  textTransform: 'uppercase',
                  letterSpacing: '0.08em',
                  color: '#6E6E78',
                  px: 1.5,
                  mb: 1.2,
                }}
              >
                {section.title}
              </Typography>
            )}

            <Box sx={{ display: 'flex', flexDirection: 'column', gap: 0.5 }}>
              {section.items.map((item) => {
                const IconComponent = item.icon;
                const isActive = activeTab === item.id;

                const content = (
                  <Box
                    key={item.id}
                    onClick={() => onSelectTab && onSelectTab(item.id)}
                    sx={{
                      display: 'flex',
                      alignItems: 'center',
                      gap: 1.5,
                      px: collapsed ? 1.5 : 1.5,
                      py: 1,
                      borderRadius: '12px',
                      cursor: 'pointer',
                      backgroundColor: isActive ? '#28282E' : 'transparent',
                      color: isActive ? '#FFFFFF' : '#8E8E98',
                      transition: 'all 0.15s ease',
                      justifyContent: collapsed ? 'center' : 'flex-start',
                      '&:hover': {
                        backgroundColor: isActive ? '#2E2E36' : '#222227',
                        color: '#FFFFFF',
                      },
                    }}
                  >
                    <Box sx={{ display: 'flex', alignItems: 'center' }}>
                      <IconComponent />
                    </Box>

                    {!collapsed && (
                      <Typography
                        sx={{
                          fontSize: '13px',
                          fontWeight: isActive ? 600 : 500,
                          letterSpacing: '0.01em',
                          whiteSpace: 'nowrap',
                        }}
                      >
                        {item.label}
                      </Typography>
                    )}
                  </Box>
                );

                return collapsed ? (
                  <Tooltip key={item.id} title={item.label} placement="right">
                    {content}
                  </Tooltip>
                ) : (
                  content
                );
              })}
            </Box>
          </Box>
        ))}
      </Box>

      {/* Bottom Log out */}
      <Box sx={{ pt: 2, borderTop: '1px solid rgba(255,255,255,0.06)' }}>
        <Box
          onClick={onLogout}
          sx={{
            display: 'flex',
            alignItems: 'center',
            gap: 1.5,
            px: collapsed ? 1.5 : 1.5,
            py: 1,
            borderRadius: '12px',
            cursor: 'pointer',
            color: '#8E8E98',
            justifyContent: collapsed ? 'center' : 'flex-start',
            transition: 'all 0.15s ease',
            '&:hover': {
              backgroundColor: '#28282E',
              color: '#F9BFD8',
            },
          }}
        >
          <Icons.Logout />
          {!collapsed && (
            <Typography sx={{ fontSize: '13px', fontWeight: 500 }}>
              Log out
            </Typography>
          )}
        </Box>
      </Box>
    </Box>
  );
}
